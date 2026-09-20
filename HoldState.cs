namespace AutoClick;

// Apenas um DOWN físico do lateral inicia o autoclick. Um UP sempre o interrompe.
internal sealed class HoldState
{
    internal enum Transition { None, Started, Stopped }

    public bool Enabled { get; private set; }
    public bool Held { get; private set; }
    public bool PhysicalRightHeld { get; private set; }
    public int SelectedButton { get; private set; } = 1;

    // Não envie um UP sintético enquanto o botão direito REAL está segurado:
    // isso faria aplicativos perderem o arraste/clique direito do usuário.
    public bool CanClick => Enabled && Held && !PhysicalRightHeld;

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        Held = false; // Ao reativar, exige um novo pressionamento físico.
    }

    public void SelectButton(int button)
    {
        if (button is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(button));
        SelectedButton = button;
        Held = false;
    }

    public void Stop() => Held = false;

    // Flags são eventos físicos de WM_INPUT, não eventos de SendInput.
    public Transition OnRawMouseButtons(ushort flags)
    {
        const ushort rightDown = 0x0004; // RI_MOUSE_RIGHT_BUTTON_DOWN
        const ushort rightUp = 0x0008;   // RI_MOUSE_RIGHT_BUTTON_UP
        if ((flags & rightDown) != 0) PhysicalRightHeld = true;
        if ((flags & rightUp) != 0) PhysicalRightHeld = false;

        ushort down = SelectedButton == 1 ? (ushort)0x0040 : (ushort)0x0100;
        ushort up = SelectedButton == 1 ? (ushort)0x0080 : (ushort)0x0200;

        // A soltura vence inclusive quando chegam flags de DOWN e UP juntas.
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
