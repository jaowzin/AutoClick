using System.Runtime.InteropServices;

namespace AutoClick;

internal static class MouseSender
{
    private const uint MouseInput = 0;
    private const uint LeftDown = 0x0002;
    private const uint LeftUp = 0x0004;

    public static bool LeftClick()
    {
        // O programa injeta SOMENTE clique esquerdo; o botão direito nunca é alterado.
        var inputs = new[]
        {
            new Input { Type = MouseInput, Data = new InputUnion { Mouse = new MouseInputData { Flags = LeftDown } } },
            new Input { Type = MouseInput, Data = new InputUnion { Mouse = new MouseInputData { Flags = LeftUp } } }
        };

        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == (uint)inputs.Length;
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
