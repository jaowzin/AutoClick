using System.Runtime.InteropServices;

namespace AutoClick;

internal static class MouseSender
{
    private const uint MouseInput = 0;
    private const uint LeftDown = 0x0002;
    private const uint LeftUp = 0x0004;

    public static bool LeftClick()
    {
        // Envia pressionar e soltar no MESMO lote. Nunca altera o botão direito.
        var release = new Input
        {
            Type = MouseInput,
            Data = new InputUnion { Mouse = new MouseInputData { Flags = LeftUp } }
        };
        var inputs = new[]
        {
            new Input { Type = MouseInput, Data = new InputUnion { Mouse = new MouseInputData { Flags = LeftDown } } },
            release
        };

        uint sent = SendInput(2, inputs, Marshal.SizeOf<Input>());
        if (sent == 2)
            return true;

        // Falha parcial rara: se LEFTDOWN foi aceito mas LEFTUP não, tenta
        // liberar imediatamente antes de a interface desativar o autoclick.
        // Não injeta UP se DOWN não foi enviado.
        if (sent == 1)
            SendInput(1, new[] { release }, Marshal.SizeOf<Input>());
        return false;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MouseInputData Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInputData
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, [In] Input[] inputs, int inputSize);
}
