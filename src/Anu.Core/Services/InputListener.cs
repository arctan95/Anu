using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Anu.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using SharpHook;

namespace Anu.Core.Services;

public static class InputListener
{
    private static ChatWindowViewModel? _chatWindowViewModel;
    private static IGlobalHook? _globalHook;

    static InputListener()
    {
        _chatWindowViewModel = ServiceProviderBuilder.ServiceProvider?.GetRequiredService<ChatWindowViewModel>();
        _globalHook = new TaskPoolGlobalHook(1, GlobalHookType.All, null, true);
        _globalHook.KeyPressed += GlobalHotkeyManager.OnKeyPressed;
        _globalHook.KeyPressed += GlobalHotkeyRecorder.OnKeyPressed;
        _globalHook.MouseMoved += OnMouseMoved;
        _globalHook.MouseDragged += OnMouseMoved;
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
}