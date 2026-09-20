namespace AutoClick;

// Apenas o lateral FÍSICO selecionado inicia; soltar sempre interrompe.
internal sealed class HoldState
{
    internal enum Transition { None, Started, Stopped }

    public bool Enabled { get; private set; }
    public bool Held { get; private set; }
    public bool PhysicalLeftHeld { get; private set; }
    public int SelectedButton { get; private set; } = 1;

    // O direito físico é independente: pode mirar enquanto o lateral atira.
    public bool CanClick => Enabled && Held && !PhysicalLeftHeld;

    // Porta final: estado baseado em evento NÃO basta; sem confirmação
    // independente do botão lateral, nenhum clique pode ser emitido.
    public bool CanClickWithPhysicalState(bool sideActuallyDown, bool leftActuallyDown)
        => CanClick && sideActuallyDown && !leftActuallyDown;

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        Held = false; // Reativar exige novo pressionamento real.
    }

    public void SelectButton(int button)
    {
        if (button is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(button));
        SelectedButton = button;
        Held = false;
    }

    public void Stop() => Held = false;

    // Flags de WM_INPUT vêm do mouse físico, não de SendInput.
    public Transition OnRawMouseButtons(ushort flags)
    {
        const ushort leftDown = 0x0001;
        const ushort leftUp = 0x0002;
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
