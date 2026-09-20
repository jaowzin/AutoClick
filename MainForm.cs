using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoClick;

internal sealed class MainForm : Form
{
    private const int VkXButton1 = 0x05;
    private const int VkXButton2 = 0x06;
    private const int VkLeftButton = 0x01;

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
        Maximum = 40,
        Value = 12,
        TextAlign = HorizontalAlignment.Center
    };

    private readonly Label _status = new()
    {
        Location = new Point(20, 83),
        Size = new Size(350, 56),
        AutoSize = false
    };

    private readonly Button _toggle = new()
    {
        Location = new Point(20, 146),
        Size = new Size(347, 38),
        Text = "Ativar autoclick",
        Enabled = false
    };

    // O timer verifica a pressão física a cada tick; um relógio separado
    // controla a cadência dos cliques sem perder a verificação de soltura.
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 10 };
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly HoldState _hold = new();
    private long _lastClickMs = -1;
    private bool _ready;

    public MainForm()
    {
        Text = "AutoClick - Lateral → clique esquerdo";
        ClientSize = new Size(387, 201);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(new Label { Location = new Point(20, 17), AutoSize = true, Text = "Botão que ativa" });
        Controls.Add(new Label { Location = new Point(272, 17), AutoSize = true, Text = "Cliques/seg." });
        Controls.AddRange(new Control[] { _sideButton, _clicksPerSecond, _status, _toggle });

        _sideButton.Items.AddRange(new object[] { "Lateral 1 (Voltar)", "Lateral 2 (Avançar)" });
        _sideButton.SelectedIndex = 0;
        _timer.Tick += (_, _) => CheckAndClick();
        _sideButton.SelectedIndexChanged += (_, _) =>
        {
            _hold.SelectButton(_sideButton.SelectedIndex + 1);
            StopClicking();
        };
        _toggle.Click += (_, _) =>
        {
            _timer.Stop();
            _hold.SetEnabled(!_hold.Enabled);
            _lastClickMs = -1;
            _toggle.Text = _hold.Enabled ? "Pausar autoclick" : "Ativar autoclick";
            UpdateStatus();
        };
        UpdateStatus();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        try
        {
            RawMouseInput.Register(Handle);
            _ready = true;
            _toggle.Enabled = true;
        }
        catch (Win32Exception ex)
        {
            _ready = false;
            _timer.Stop();
            _hold.SetEnabled(false);
            _toggle.Enabled = false;
            MessageBox.Show(this, ex.Message, "AutoClick: erro de inicialização",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        UpdateStatus();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == RawMouseInput.WmInput && _ready &&
            RawMouseInput.TryGetButtonFlags(m.LParam, out ushort flags))
        {
            switch (_hold.OnRawMouseButtons(flags))
            {
                case HoldState.Transition.Started:
                    // Aguarda o estado de XBUTTON atualizar e verifica no timer.
                    // Nunca injeta clique só porque chegou um evento DOWN.
                    _lastClickMs = -1;
                    _timer.Start();
                    UpdateStatus();
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

    private void CheckAndClick()
    {
        if (!_ready || !_hold.Enabled || !_hold.Held)
        {
            StopClicking();
            return;
        }

        // Confirma o botão lateral real ANTES DE CADA CLIQUE. Sem hook de
        // bloqueio, o estado assíncrono do Windows continua confiável.
        int side = _hold.SelectedButton == 1 ? VkXButton1 : VkXButton2;
        bool sideDown = (GetAsyncKeyState(side) & 0x8000) != 0;
        if (!sideDown)
        {
            StopClicking();
            return;
        }

        // Não solta virtualmente o esquerdo físico que a pessoa está segurando.
        // Botão direito não é interceptado, reconfigurado nem enviado.
        bool leftDown = (GetAsyncKeyState(VkLeftButton) & 0x8000) != 0;
        if (!_hold.CanClickWithPhysicalState(sideDown, leftDown))
            return;

        long nowMs = _clock.ElapsedMilliseconds;
        double minIntervalMs = 1000.0 / (double)_clicksPerSecond.Value;
        if (_lastClickMs >= 0 && nowMs - _lastClickMs < minIntervalMs)
            return;

        if (!MouseSender.LeftClick())
        {
            _hold.SetEnabled(false); // Falha fechado se SendInput não confirmar.
            StopClicking();
            _toggle.Text = "Ativar autoclick";
            _status.Text = "Falha ao enviar clique; autoclick pausado por segurança.";
            return;
        }
        _lastClickMs = _clock.ElapsedMilliseconds;
    }

    private void StopClicking()
    {
        _timer.Stop();
        _hold.Stop();
        _lastClickMs = -1;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        _status.Text = !_ready
            ? "Inicializando... autoclick desativado."
            : !_hold.Enabled
                ? "Pausado. Clique em Ativar para habilitar."
                : _hold.Held && _hold.PhysicalLeftHeld
                    ? "Esquerdo físico pressionado: autoclick suspenso."
                    : _hold.Held
                        ? "Segurando lateral: clique esquerdo automático."
                        : "Pronto. Segure o lateral; direito livre para mirar.\nO lateral mantém sua ação original (Voltar/Avançar).";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _hold.SetEnabled(false);
        _timer.Dispose();
        base.OnFormClosed(e);
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);
}
