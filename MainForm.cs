using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoClick;

internal sealed class MainForm : Form
{
    private const int VkXButton1 = 0x05;
    private const int VkXButton2 = 0x06;
    private const int VkLeftButton = 0x01;
    private const int WmHotkey = 0x0312;
    private const int PanicHotkeyId = 0xAC01;
    private const uint ModControlAlt = 0x0003;
    private const uint VkF12 = 0x7B;

    private readonly ComboBox _sideButton = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Location = new Point(20, 40),
        Size = new Size(225, 28)
    };

    private readonly NumericUpDown _clicksPerSecond = new()
    {
        Location = new Point(272, 40),
        Size = new Size(95, 28),
        Minimum = 1,
        Maximum = 1000,
        Value = 12,
        TextAlign = HorizontalAlignment.Center,
        ThousandsSeparator = true
    };

    private readonly Label _status = new()
    {
        Location = new Point(20, 83),
        Size = new Size(350, 65),
        AutoSize = false
    };

    private readonly Button _toggle = new()
    {
        Location = new Point(20, 154),
        Size = new Size(347, 38),
        Text = "Ativar autoclick",
        Enabled = false
    };

    private readonly HoldState _hold = new();
    private readonly NotifyIcon _trayIcon = new();
    private readonly ContextMenuStrip _trayMenu = new();
    private readonly ToolStripMenuItem _trayToggle = new("Ativar autoclick");
    private readonly ToolStripMenuItem _trayOpen = new("Abrir AutoClick");
    private readonly ToolStripMenuItem _trayExit = new("Sair completamente");
    private CancellationTokenSource? _clickCancellation;
    private int _clickSession;
    private int _requestedCps = 12;
    private bool _ready;
    private bool _reallyExit;
    private bool _hotkeyRegistered;

    public MainForm()
    {
        Text = "AutoClick - Lateral → clique esquerdo";
        ClientSize = new Size(387, 210);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(new Label { Location = new Point(20, 17), AutoSize = true, Text = "Botão que ativa" });
        Controls.Add(new Label { Location = new Point(272, 17), AutoSize = true, Text = "Cliques/seg." });
        Controls.AddRange(new Control[] { _sideButton, _clicksPerSecond, _status, _toggle });

        _sideButton.Items.AddRange(new object[] { "Lateral 1 (Voltar)", "Lateral 2 (Avançar)" });
        _sideButton.SelectedIndex = 0;
        _sideButton.SelectedIndexChanged += (_, _) =>
        {
            StopClicking();
            _hold.SelectButton(_sideButton.SelectedIndex + 1);
            UpdateStatus();
        };
        _clicksPerSecond.ValueChanged += (_, _) =>
            Volatile.Write(ref _requestedCps, (int)_clicksPerSecond.Value);
        _toggle.Click += (_, _) => ToggleEnabled();

        _trayOpen.Click += (_, _) => RestoreWindow();
        _trayToggle.Click += (_, _) => ToggleEnabled();
        _trayExit.Click += (_, _) =>
        {
            _reallyExit = true;
            Close();
        };
        _trayMenu.Items.AddRange(new ToolStripItem[]
        {
            _trayOpen, _trayToggle, new ToolStripSeparator(), _trayExit
        });
        _trayIcon.Icon = SystemIcons.Application;
        _trayIcon.Text = "AutoClick (pausado)";
        _trayIcon.ContextMenuStrip = _trayMenu;
        _trayIcon.DoubleClick += (_, _) => RestoreWindow();
        _trayIcon.Visible = true;
        UpdateStatus();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        try
        {
            RawMouseInput.Register(Handle);
            _hotkeyRegistered = RegisterHotKey(Handle, PanicHotkeyId, ModControlAlt, VkF12);
            _ready = true;
            _toggle.Enabled = true;
        }
        catch (Win32Exception ex)
        {
            _ready = false;
            StopClicking();
            _hold.SetEnabled(false);
            _toggle.Enabled = false;
            MessageBox.Show(this, ex.Message, "AutoClick: erro de inicialização",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        UpdateStatus();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam == (IntPtr)PanicHotkeyId)
        {
            Pause(); // Funciona também com a janela escondida.
            return;
        }
        if (m.Msg == RawMouseInput.WmInput && _ready &&
            RawMouseInput.TryGetButtonFlags(m.LParam, out ushort flags))
        {
            switch (_hold.OnRawMouseButtons(flags))
            {
                case HoldState.Transition.Started:
                    StartClicking();
                    break;
                case HoldState.Transition.Stopped:
                    StopClicking();
                    break;
                default:
                    if (_hold.Held && (flags & (0x0001 | 0x0002)) != 0)
                        UpdateStatus();
                    break;
            }
        }
        base.WndProc(ref m);
    }

    private void ToggleEnabled()
    {
        if (!_ready) return;
        StopClicking();
        _hold.SetEnabled(!_hold.Enabled);
        UpdateStatus();
    }

    private void Pause()
    {
        StopClicking();
        _hold.SetEnabled(false);
        UpdateStatus();
    }

    private void StartClicking()
    {
        CancelWorker();
        if (!_ready || !_hold.CanClick)
        {
            UpdateStatus();
            return;
        }
        var cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token; // Captura ANTES de eventual Dispose.
        _clickCancellation = cancellation;
        int session = Interlocked.Increment(ref _clickSession);
        int side = _hold.SelectedButton == 1 ? VkXButton1 : VkXButton2;
        _ = Task.Factory.StartNew(() => ClickLoop(side, session, token),
            CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        UpdateStatus();
    }

    private void ClickLoop(int side, int session, CancellationToken token)
    {
        try
        {
            long nextClick = Stopwatch.GetTimestamp();
            long startedAt = nextClick;
            while (!token.IsCancellationRequested && session == Volatile.Read(ref _clickSession))
            {
                // O estado físico é a autoridade, mesmo com perda de WM_INPUT.
                if ((GetAsyncKeyState(side) & 0x8000) == 0)
                {
                    // Pequena janela inicial para o Windows atualizar XBUTTON.
                    if (Stopwatch.GetTimestamp() - startedAt < Stopwatch.Frequency / 50)
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                    NotifyWorkerStopped(session, null);
                    return;
                }
                startedAt = long.MinValue / 2; // Após confirmar, qualquer soltura para.

                if ((GetAsyncKeyState(VkLeftButton) & 0x8000) != 0)
                {
                    nextClick = Stopwatch.GetTimestamp();
                    Thread.Sleep(1);
                    continue;
                }

                long until = nextClick - Stopwatch.GetTimestamp();
                if (until > 0)
                {
                    if (until > Stopwatch.Frequency / 500)
                        Thread.Sleep(1);
                    else if (until > Stopwatch.Frequency / 2000)
                        Thread.Sleep(0);
                    else
                        Thread.SpinWait(64);
                    continue;
                }

                if (token.IsCancellationRequested || session != Volatile.Read(ref _clickSession))
                    return;
                if ((GetAsyncKeyState(side) & 0x8000) == 0)
                {
                    NotifyWorkerStopped(session, null);
                    return;
                }
                if ((GetAsyncKeyState(VkLeftButton) & 0x8000) != 0)
                    continue;

                if (!MouseSender.LeftClick())
                {
                    NotifyWorkerStopped(session, "Windows não confirmou o clique. Autoclick pausado.");
                    return;
                }
                int cps = Math.Clamp(Volatile.Read(ref _requestedCps), 1, 1000);
                nextClick = Stopwatch.GetTimestamp() + Math.Max(1L, Stopwatch.Frequency / cps);
                // Nunca acumula cliques atrasados para dispará-los em rajada.
            }
        }
        catch (Exception)
        {
            NotifyWorkerStopped(session, "Erro no motor de cliques. Autoclick pausado.");
        }
    }

    private void NotifyWorkerStopped(int session, string? error)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try
        {
            BeginInvoke((Action)(() =>
            {
                if (session != Volatile.Read(ref _clickSession)) return;
                if (error is null)
                    StopClicking();
                else
                {
                    Pause();
                    _status.Text = error;
                }
            }));
        }
        catch (InvalidOperationException) { /* A janela foi encerrada. */ }
    }

    private void CancelWorker()
    {
        Interlocked.Increment(ref _clickSession);
        var old = _clickCancellation;
        _clickCancellation = null;
        if (old is not null)
        {
            old.Cancel();
            old.Dispose(); // Thread utiliza apenas CancellationToken, não o CTS.
        }
    }

    private void StopClicking()
    {
        CancelWorker();
        _hold.Stop();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        bool enabled = _ready && _hold.Enabled;
        _toggle.Text = enabled ? "Pausar autoclick" : "Ativar autoclick";
        _trayToggle.Text = _toggle.Text;
        _trayToggle.Enabled = _ready;
        _trayIcon.Text = enabled ? "AutoClick (ativo)" : "AutoClick (pausado)";
        _status.Text = !_ready
            ? "Inicializando... autoclick desativado."
            : !enabled
                ? "Pausado. Ative para usar. Ctrl+Alt+F12: pausa de emergência."
                : _hold.Held && _hold.PhysicalLeftHeld
                    ? "Esquerdo físico pressionado: autoclick suspenso."
                    : _hold.Held
                        ? "Segurando lateral: clique esquerdo automático."
                        : "Segure o lateral para clicar. X: manter na bandeja.\nCtrl+Alt+F12: pausa de emergência.";
    }

    private void RestoreWindow()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        Activate();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_reallyExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            ShowInTaskbar = false;
            return;
        }
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        Pause();
        if (_hotkeyRegistered) UnregisterHotKey(Handle, PanicHotkeyId);
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _trayMenu.Dispose();
        base.OnFormClosed(e);
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr window, int id);
}
