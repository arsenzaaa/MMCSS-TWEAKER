using System.Runtime.InteropServices;

namespace MMCSSTweaker.Win32;

internal static class NativeMethods
{
    internal const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;
    internal const int DWMWA_CAPTION_COLOR = 35;
    internal const int DWMWA_TEXT_COLOR = 36;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int SetCurrentProcessExplicitAppUserModelID(string appID);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool AllocConsole();

    [DllImport("dwmapi.dll")]
    internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
}
