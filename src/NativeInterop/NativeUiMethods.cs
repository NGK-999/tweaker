using System;
using System.Runtime.InteropServices;

namespace ApexTweaker.NativeInterop;

internal static class NativeUiMethods
{
    internal const int WM_NCHITTEST = 0x0084;
    internal const int HTCLIENT = 1;
    internal const int HTCAPTION = 2;
    internal const int HTLEFT = 10;
    internal const int HTRIGHT = 11;
    internal const int HTTOP = 12;
    internal const int HTTOPLEFT = 13;
    internal const int HTTOPRIGHT = 14;
    internal const int HTBOTTOM = 15;
    internal const int HTBOTTOMLEFT = 16;
    internal const int HTBOTTOMRIGHT = 17;

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        nint hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    [DllImport("user32.dll")]
    internal static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetWindowRect(nint hWnd, out RECT rect);

    internal static bool TryApplyModernWindowFrame(nint handle)
    {
        if (handle == nint.Zero || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return false;
        }

        var updated = false;
        updated |= TrySetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, 1);
        updated |= TrySetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, (int)DwmWindowCornerPreference.Round);

        // Windows 11 maps TabbedWindow most closely to Mica Alt. If unavailable,
        // fall back to TransientWindow/Acrylic-like behavior and then to MainWindow.
        updated |= TrySetWindowAttribute(handle, DWMWA_SYSTEMBACKDROP_TYPE, (int)DwmSystemBackdropType.TabbedWindow)
                   || TrySetWindowAttribute(handle, DWMWA_SYSTEMBACKDROP_TYPE, (int)DwmSystemBackdropType.TransientWindow)
                   || TrySetWindowAttribute(handle, DWMWA_SYSTEMBACKDROP_TYPE, (int)DwmSystemBackdropType.MainWindow);

        return updated;
    }

    internal static bool ApplyWindowCorners(nint handle)
    {
        if (handle == nint.Zero || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return false;
        }

        return TrySetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, (int)DwmWindowCornerPreference.Round);
    }

    private static bool TrySetWindowAttribute(nint handle, int attribute, int value)
    {
        try
        {
            return DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int)) >= 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }
}

internal enum DwmSystemBackdropType
{
    Auto = 0,
    None = 1,
    MainWindow = 2,
    TransientWindow = 3,
    TabbedWindow = 4
}

internal enum DwmWindowCornerPreference
{
    Default = 0,
    DoNotRound = 1,
    Round = 2,
    RoundSmall = 3
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct RECT
{
    public readonly int Left;
    public readonly int Top;
    public readonly int Right;
    public readonly int Bottom;
}
