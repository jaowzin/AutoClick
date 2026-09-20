using System.Runtime.InteropServices;

namespace AutoClick;

internal static class MouseSender
{
    private const uint MouseInput = 0;
    private const uint RightDown = 0x0008;
    private const uint RightUp = 0x0010;

    public static bool RightClick()
    {
        // Dois eventos independentes: pressionar e soltar o botão direito virtual.
        // O botão direito físico não é remapeado e continua disponível normalmente.
        var inputs = new[]
        {
            new Input { Type = MouseInput, Data = new InputUnion { Mouse = new MouseInputData { Flags = RightDown } } },
            new Input { Type = MouseInput, Data = new InputUnion { Mouse = new MouseInputData { Flags = RightUp } } }
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
