namespace AutoClick;

// Apenas o botão lateral FÍSICO selecionado inicia; soltar sempre interrompe.
internal sealed class HoldState
{
    internal enum Transition { None, Started, Stopped }

    public bool Enabled { get; private set; }
    public bool Held { get; private set; }
    public bool PhysicalLeftHeld { get; private set; }
    public int SelectedButton { get; private set; } = 1;

    // LEFTUP sintético não pode interromper um LEFTDOWN físico do usuário.
    // O direito físico é independente: pode mirar enquanto o lateral atira.
    public bool CanClick => Enabled && Held && !PhysicalLeftHeld;

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        Held = false; // Exige novo pressionamento real depois de reativar.
    }

    public void SelectButton(int button)
    {
        if (button is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(button));
        SelectedButton = button;
        Held = false;
    }

    public void Stop() => Held = false;

    // Flags do WM_INPUT físico: eventos sintéticos de SendInput não ativam o lateral.
    public Transition OnRawMouseButtons(ushort flags)
    {
        const ushort leftDown = 0x0001; // RI_MOUSE_LEFT_BUTTON_DOWN
        const ushort leftUp = 0x0002;   // RI_MOUSE_LEFT_BUTTON_UP
        if ((flags & leftDown) != 0) PhysicalLeftHeld = true;
        if ((flags & leftUp) != 0) PhysicalLeftHeld = false;

        ushort down = SelectedButton == 1 ? (ushort)0x0040 : (ushort)0x0100;
        ushort up = SelectedButton == 1 ? (ushort)0x0080 : (ushort)0x0200;

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
