using System.ComponentModel;
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

    private readonly CheckBox _suppressOriginal = new()
    {
        Location = new Point(20, 83),
        Size = new Size(348, 34),
        Text = "Bloquear a ação original do lateral (voltar/avançar)",
        Checked = true
    };

    private readonly Label _status = new()
    {
        Location = new Point(20, 126),
        Size = new Size(350, 34),
        AutoSize = false
    };

    private readonly Button _toggle = new()
    {
        Location = new Point(20, 170),
        Size = new Size(347, 38),
        Text = "Ativar autoclick",
        Enabled = false
    };

    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly HoldState _hold = new();
    private readonly MouseHook _hook;
    private bool _ready;

    public MainForm()
    {
        Text = "AutoClick - Lateral → clique esquerdo";
        ClientSize = new Size(387, 225);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(new Label { Location = new Point(20, 17), AutoSize = true, Text = "Botão que ativa" });
        Controls.Add(new Label { Location = new Point(272, 17), AutoSize = true, Text = "Cliques/seg." });
        Controls.AddRange(new Control[] { _sideButton, _clicksPerSecond, _suppressOriginal, _status, _toggle });

        _sideButton.Items.AddRange(new object[] { "Lateral 1 (Voltar)", "Lateral 2 (Avançar)" });
        _sideButton.SelectedIndex = 0;
        _timer.Interval = 1000 / (int)_clicksPerSecond.Value;
        _timer.Tick += (_, _) => SendLeftClick(checkPhysicalState: true);
        _clicksPerSecond.ValueChanged += (_, _) =>
            _timer.Interval = Math.Max(1, (int)Math.Round(1000m / _clicksPerSecond.Value));
        _sideButton.SelectedIndexChanged += (_, _) =>
        {
            _hold.SelectButton(_sideButton.SelectedIndex + 1);
            StopClicking();
        };

        _toggle.Click += (_, _) =>
        {
            _timer.Stop();
            _hold.SetEnabled(!_hold.Enabled);
            _toggle.Text = _hold.Enabled ? "Pausar autoclick" : "Ativar autoclick";
            UpdateStatus();
        };

        // Hook só observa a soltura do lateral e opcionalmente bloqueia Voltar/Avançar.
        // Jamais intercepta o clique direito ou esquerdo físico.
        _hook = new MouseHook(OnSideButton);
        UpdateStatus();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        try
        {
            RawMouseInput.Register(Handle);
            _hook.Start();
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
            // WM_INPUT relata o lateral físico e o estado do esquerdo REAL,
            // sem confundir cliques que nós próprios injetamos.
            var transition = _hold.OnRawMouseButtons(flags);
            switch (transition)
            {
                case HoldState.Transition.Started:
                    _timer.Start();
                    UpdateStatus();
                    SendLeftClick(checkPhysicalState: false);
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

    private bool OnSideButton(int button, bool down)
    {
        if (button == _hold.SelectedButton && !down)
            StopClicking(); // Redundância caso a soltura não chegue pelo Raw Input.

        return _hold.Enabled && button == _hold.SelectedButton && _suppressOriginal.Checked;
    }

    private void SendLeftClick(bool checkPhysicalState)
    {
        if (!_ready || !_hold.CanClick)
            return;

        // Nunca injeta LEFTUP enquanto o esquerdo real está segurado: evita
        // cancelar disparo contínuo/arrastar selecionado pelo usuário.
        if ((GetAsyncKeyState(VkLeftButton) & 0x8000) != 0)
            return;

        // Sem bloqueio do lateral, o estado do Windows serve como verificação extra.
        // Com bloqueio, GetAsyncKeyState pode não atualizar: WM_INPUT e hook cuidam do UP.
        if (checkPhysicalState && !_suppressOriginal.Checked)
        {
            int key = _hold.SelectedButton == 1 ? VkXButton1 : VkXButton2;
            if ((GetAsyncKeyState(key) & 0x8000) == 0)
            {
                StopClicking();
                return;
            }
        }

        if (!MouseSender.LeftClick())
        {
            StopClicking();
            _status.Text = "Windows bloqueou o clique. Verifique as permissões da janela.";
        }
    }

    private void StopClicking()
    {
        _timer.Stop();
        _hold.Stop();
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
                        ? "Clicando esquerdo... Solte o lateral para parar."
                        : "Pronto. Segure o lateral; direito segue para mirar.";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _hold.SetEnabled(false);
        _hook.Dispose();
        _timer.Dispose();
        base.OnFormClosed(e);
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);
}
