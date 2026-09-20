namespace AutoClick;

// Uma transição real de pressionar inicia; soltar sempre desarma.
// Eventos duplicados de DOWN não criam novas sessões de clique.
internal sealed class HoldState
{
    internal enum Transition { None, Started, Stopped }

    public bool Enabled { get; private set; }
    public bool Held { get; private set; }
    public int SelectedButton { get; private set; } = 1;
    public bool CanClick => Enabled && Held;

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        Held = false; // Reativar não aproveita um pressionamento antigo.
    }

    public void SelectButton(int button)
    {
        if (button is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(button));
        SelectedButton = button;
        Held = false;
    }

    public void Stop() => Held = false;

    // RI_MOUSE_BUTTON_4 = lateral 1; RI_MOUSE_BUTTON_5 = lateral 2.
    // O estado vem de WM_INPUT físico, não dos cliques injetados via SendInput.
    public Transition OnRawMouseButtons(ushort flags)
    {
        ushort down = SelectedButton == 1 ? (ushort)0x0040 : (ushort)0x0100;
        ushort up = SelectedButton == 1 ? (ushort)0x0080 : (ushort)0x0200;

        // Soltar tem prioridade, mesmo se o evento contiver outras flags.
        if ((flags & up) != 0)
        {
            if (!Held) return Transition.None;
            Held = false;
            return Transition.Stopped;
        }

        if (Enabled && (flags & down) != 0 && !Held)
        {
            Held = true;
            return Transition.Started;
        }

        return Transition.None;
    }
}
