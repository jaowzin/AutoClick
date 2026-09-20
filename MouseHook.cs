using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AutoClick;

internal sealed class MouseHook : IDisposable
{
    private const int WhMouseLl = 14;
    private const int WmXButtonDown = 0x020B;
    private const int WmXButtonUp = 0x020C;

    private readonly HookProc _callback;
    private readonly Func<int, bool, bool> _onSideButton;
    private IntPtr _handle;

    public MouseHook(Func<int, bool, bool> onSideButton)
    {
        _onSideButton = onSideButton;
        _callback = HandleMouseEvent; // Mantém o delegate vivo enquanto o hook estiver instalado.
    }

    public void Start()
    {
        if (_handle != IntPtr.Zero)
            return;

        _handle = SetWindowsHookEx(WhMouseLl, _callback, GetModuleHandle(null), 0);
        if (_handle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Falha ao instalar o hook global do mouse.");
    }

    private IntPtr HandleMouseEvent(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0 && (message == (IntPtr)WmXButtonDown || message == (IntPtr)WmXButtonUp))
        {
            var mouse = Marshal.PtrToStructure<LowLevelMouseData>(data);
            int button = (int)((mouse.MouseData >> 16) & 0xFFFF);
            if ((button == 1 || button == 2) && _onSideButton(button, message == (IntPtr)WmXButtonDown))
                return (IntPtr)1; // Impede voltar/avançar somente quando solicitado.
        }

        return CallNextHookEx(_handle, code, message, data);
    }

    public void Dispose()
    {
        if (_handle == IntPtr.Zero)
            return;

        UnhookWindowsHookEx(_handle);
        _handle = IntPtr.Zero;
    }

    private delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LowLevelMouseData
    {
        public Point Position;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int hookId, HookProc callback, IntPtr module, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
}
