using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Anu.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using SharpHook;
using SharpHook.Native;

namespace Anu.Core.Services;

public class InputListener
{
    private static ChatWindowViewModel? _chatWindowViewModel;
    private static IGlobalHook? _globalHook;
    private static DateTime _lastAltTime = DateTime.MinValue;
    private static DateTime _lastShiftTime = DateTime.MinValue;
    private static DateTime _lastCtrlTime = DateTime.MinValue;
    private const int DoubleTapThresholdMs = 300;
    
    static InputListener()
    {
        _globalHook = new TaskPoolGlobalHook(1, GlobalHookType.All, null, true);
        _globalHook.KeyPressed += GlobalHotkeyManager.OnKeyPressed;
        _globalHook.KeyPressed += GlobalHotkeyRecorder.OnKeyPressed;
        _globalHook.KeyPressed += CheckDefaultKeyBindings;
        _globalHook.MouseMoved += OnMouseMoved;
        _globalHook.MouseDragged += OnMouseMoved;
        _chatWindowViewModel = ServiceProviderBuilder.ServiceProvider?.GetRequiredService<ChatWindowViewModel>();
    }
    public static void SetupSystemHook()
    {
        if (_globalHook != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _globalHook.RunAsync();
                    Debug.WriteLine("Hook started.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error starting hook: {ex.Message}");
                }
            });
        }
    }
    
    private static void OnMouseMoved(object? sender, MouseHookEventArgs e)
    {
        _chatWindowViewModel?.OnMouseMoved(e.Data.X, e.Data.Y);
    }

    private static void CheckDefaultKeyBindings(object? sender, KeyboardHookEventArgs e)
    {
        if (e.Data.KeyCode is KeyCode.VcLeftAlt or KeyCode.VcRightAlt)
        {
            var now = DateTime.Now;
            var timeDelta = now - _lastAltTime;
            
            if (timeDelta.TotalMilliseconds < DoubleTapThresholdMs)
            {
                _chatWindowViewModel?.ToggleFollowPointer();
            }
            _lastAltTime = now;
        }
        
        if (e.RawEvent.Mask.HasFlag(ModifierMask.LeftCtrl) && e.RawEvent.Mask.HasFlag(ModifierMask.LeftAlt))
        {
            double delta = 25;
            Vector offset = e.Data.KeyCode switch
            {
                KeyCode.VcUp => new Vector(0, -delta),
                KeyCode.VcDown => new Vector(0, delta),
                KeyCode.VcLeft => new Vector(-delta, 0),
                KeyCode.VcRight => new Vector(delta, 0),
                _ => Vector.Zero
            };

            ScrollMarkdown(offset);
        }
        
        if (e.Data.KeyCode is KeyCode.VcLeftControl or KeyCode.VcRightControl)
        {
            var now = DateTime.Now;
            var timeDelta = now - _lastCtrlTime;

            if (timeDelta.TotalMilliseconds < DoubleTapThresholdMs)
            {
                if (Application.Current is App app)
                {
                    app.ToggleClickThrough();
                }
            }
            _lastCtrlTime = now;
        }

        if (e.Data.KeyCode is KeyCode.VcLeftShift or KeyCode.VcRightShift)
        {
            var now = DateTime.Now;
            var timeDelta = now - _lastShiftTime;
            
            if (timeDelta.TotalMilliseconds < DoubleTapThresholdMs)
            {
                if (Application.Current is App app)
                {
                    app.OpenChatWindowForInput();
                }
            }
            _lastShiftTime = now;
        }
    }
    
    
    static void ScrollMarkdown(Vector offset)
    {
        _chatWindowViewModel?.ScrollMarkdown(offset);
    }

    
}