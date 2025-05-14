using System;
using Avalonia.Controls;
using Anu.Core.Models;

namespace Anu.Core.Services;

public interface IWindowConfigurator
{
    public void ConfigureWindow(Window window, WindowBehaviorOptions windowBehaviorOptions)
    {
        if (window.TryGetPlatformHandle()?.Handle is { } handle)
        {
            SetOverlayWindow(handle, windowBehaviorOptions.OverlayWindow);
            SetIgnoresMouseEvents(handle, windowBehaviorOptions.IgnoreMouseEvents);
            SetContentProtection(handle, windowBehaviorOptions.ContentProtection);
        }
    }

    public void SetIgnoresMouseEvents(IntPtr window, bool ignoreMouseEvents);
    public void SetOverlayWindow(IntPtr window, bool overlay);
    public void SetContentProtection(IntPtr window, bool contentProtection);
}