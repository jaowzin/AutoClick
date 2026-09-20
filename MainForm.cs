using System.Drawing;
using System.Windows.Forms;

namespace AutoClick;

internal sealed class MainForm : Form
{
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
        Text = "Bloquear a ação original do botão lateral (voltar/avançar)",
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
        Text = "Pausar autoclick"
    };

    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly MouseHook _hook;
    private bool _enabled = true;
    private bool _held;

    public MainForm()
    {
        Text = "AutoClick - Botão lateral → direito";
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
        _timer.Tick += (_, _) => SendRightClick();
        _clicksPerSecond.ValueChanged += (_, _) =>
            _timer.Interval = Math.Max(1, (int)Math.Round(1000m / _clicksPerSecond.Value));
        _sideButton.SelectedIndexChanged += (_, _) => StopClicking();

        _toggle.Click += (_, _) =>
        {
            _enabled = !_enabled;
            if (!_enabled) StopClicking();
            _toggle.Text = _enabled ? "Pausar autoclick" : "Ativar autoclick";
            UpdateStatus();
        };

        _hook = new MouseHook(OnSideButton);
        _hook.Start();
        UpdateStatus();
    }

    // O hook global é instalado na thread da interface: os eventos chegam nesta mesma thread.
    private bool OnSideButton(int button, bool down)
    {
        if (!_enabled || button != _sideButton.SelectedIndex + 1)
            return false;

        if (down)
        {
            if (!_held)
            {
                _held = true;
                _timer.Start();
                UpdateStatus();
                SendRightClick(); // Primeiro clique imediato; os demais seguem a velocidade escolhida.
            }
        }
        else
        {
            StopClicking();
        }

        // Não transforma nem bloqueia o botão direito físico; apenas o lateral escolhido.
        return _suppressOriginal.Checked;
    }

    private void SendRightClick()
    {
        if (!_enabled || !_held)
            return;

        if (!MouseSender.RightClick())
        {
            StopClicking();
            _status.Text = "Windows bloqueou o clique. Verifique as permissões da janela.";
        }
    }

    private void StopClicking()
    {
        _timer.Stop();
        _held = false;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        _status.Text = !_enabled
            ? "Pausado. Os botões do mouse funcionam normalmente."
            : _held
                ? "Clicando... Solte o botão lateral para parar."
                : "Ativo. Segure o botão lateral escolhido para clicar.";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _hook.Dispose();
        _timer.Dispose();
        base.OnFormClosed(e);
    }
}
