using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Avalonia.Media;
using Anu.Core.Services;

namespace Anu.Desktop.Windows.Services;

public class WindowConfigurator: IWindowConfigurator
{
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int HWND_TOPMOST = -1;
    
    private static COLORREF ToColorRef(Color color)
    {
        return (COLORREF)(uint)(color.R | (color.G << 8) | (color.B << 16));
    }
    public void SetIgnoresMouseEvents(IntPtr handle, bool ignoreMouseEvents)
    {
        int style = PInvoke.GetWindowLong((HWND)handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        if (ignoreMouseEvents)
        {
            style |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            PInvoke.SetWindowLong((HWND)handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, style);
            PInvoke.SetLayeredWindowAttributes((HWND)handle, ToColorRef(Brushes.Transparent.Color), 0, LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_COLORKEY);
        }
        else
        {
            style &= ~WS_EX_TRANSPARENT;
            PInvoke.SetWindowLong((HWND)handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, style);
        }
        
    }

    public void SetOverlayWindow(IntPtr handle, bool overlay)
    {
        if (overlay)
        {
            PInvoke.GetWindowRect((HWND)handle, out var winRect);
            PInvoke.SetWindowPos((HWND)handle, (HWND)HWND_TOPMOST, winRect.X, winRect.Y, winRect.Width,
                winRect.Height, SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
        }
    }

    public void SetContentProtection(IntPtr handle, bool contentProtection)
    {
        if (contentProtection)
        {
            PInvoke.SetWindowDisplayAffinity((HWND)handle, WINDOW_DISPLAY_AFFINITY.WDA_EXCLUDEFROMCAPTURE);
        }
    }
}