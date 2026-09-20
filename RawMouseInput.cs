using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AutoClick;

internal static class RawMouseInput
{
    public const int WmInput = 0x00FF;
    private const uint RidInput = 0x10000003;
    private const uint RidevInputSink = 0x00000100;
    private const uint RimTypeMouse = 0;
    private const uint Error = uint.MaxValue;

    public static void Register(IntPtr windowHandle)
    {
        var devices = new[]
        {
            new RawInputDevice
            {
                UsagePage = 0x01, // Generic Desktop
                Usage = 0x02,     // Mouse
                Flags = RidevInputSink,
                Target = windowHandle
            }
        };

        if (!RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RawInputDevice>()))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Não foi possível registrar a entrada física do mouse.");
    }

    // Ignora eventos injetados: somente WM_INPUT de um mouse físico pode ativar o clique.
    public static bool TryGetButtonFlags(IntPtr rawHandle, out ushort buttonFlags)
    {
        buttonFlags = 0;
        uint bytes = 0;
        uint headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
        if (GetRawInputData(rawHandle, RidInput, IntPtr.Zero, ref bytes, headerSize) == Error ||
            bytes < headerSize + 6)
            return false;

        IntPtr buffer = Marshal.AllocHGlobal(checked((int)bytes));
        try
        {
            if (GetRawInputData(rawHandle, RidInput, buffer, ref bytes, headerSize) == Error ||
                bytes < headerSize + 6 ||
                Marshal.ReadInt32(buffer) != RimTypeMouse)
                return false;

            // RAWINPUTHEADER vem antes de RAWMOUSE. Dentro de RAWMOUSE,
            // usButtonFlags ocupa 2 bytes na posição 4 (após usFlags + alinhamento).
            buttonFlags = unchecked((ushort)Marshal.ReadInt16(buffer, checked((int)headerSize + 4)));
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDevice
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputHeader
    {
        public uint Type;
        public uint Size;
        public IntPtr Device;
        public IntPtr WParam;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterRawInputDevices(
        [In] RawInputDevice[] devices, uint deviceCount, uint deviceSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(
        IntPtr input, uint command, IntPtr data, ref uint size, uint headerSize);
}
