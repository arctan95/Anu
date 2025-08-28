using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Clowd.Clipboard;
using Anu.Core.Views;
using CommunityToolkit.Mvvm.Input;
using Anu.Core.Models;
using Anu.Core.Services;
using Anu.Core.ViewModels;
using Avalonia.Media;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using NetSparkleUpdater;
using NetSparkleUpdater.Downloaders;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;
using SharpHook.Native;

namespace Anu.Core;

public partial class App : Application
{
    private IClassicDesktopStyleApplicationLifetime? _lifetime;
    private ChatWindowViewModel? _chatWindowViewModel;
    private SettingsWindowViewModel? _settingsWindowViewModel;
    private ConfigService? _configService;
    private Window? _chatWindow;
    private SparkleUpdater? _sparkle;
    private double _delta = 25;

    public ICommand SettingsCommand { get; }

    public ICommand CheckUpdatesCommand { get; }
    public ICommand MainCommand { get; }
    public ICommand QuitCommand { get; }

    public App()
    {
        QuitCommand = new RelayCommand(QuitApplication);
        SettingsCommand = new RelayCommand(ShowSettingsWindow);
        MainCommand = new RelayCommand(OpenChatWindowActivated);
        CheckUpdatesCommand = new AsyncRelayCommand(CheckUpdates);
        _chatWindowViewModel = ServiceProviderBuilder.ServiceProvider?.GetRequiredService<ChatWindowViewModel>();
        _settingsWindowViewModel =
            ServiceProviderBuilder.ServiceProvider?.GetRequiredService<SettingsWindowViewModel>();
        _configService = ServiceProviderBuilder.ServiceProvider?.GetRequiredService<ConfigService>();
        DataContext = this;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        InputListener.SetupSystemHook();
        RegisterFunctions();
        RegisterGlobalHotkeys();
        SetUpUpdater();
    }

    private void RegisterGlobalHotkeys()
    {
        // Predefined global hotkeys
        GlobalHotkeyManager.BindHotkey("show_chat_window_activated", ModifierMask.None, KeyCode.VcLeftShift,
            OpenChatWindowActivated, true);
        GlobalHotkeyManager.BindHotkey("show_chat_window", ModifierMask.None, KeyCode.VcLeftControl, OpenChatWindow,
            true);
        GlobalHotkeyManager.BindHotkey("toggle_follow_pointer", ModifierMask.None, KeyCode.VcLeftAlt,
            ToggleFollowPointer, true);
        GlobalHotkeyManager.BindHotkey("hide_chat_window", ModifierMask.None, KeyCode.VcEscape, HideChatWindow);
        GlobalHotkeyManager.BindHotkey("scroll_up", ModifierMask.LeftCtrl | ModifierMask.LeftAlt, KeyCode.VcUp,
            () => ManualScrollContent(new Vector(0, -_delta)));
        GlobalHotkeyManager.BindHotkey("scroll_down", ModifierMask.LeftCtrl | ModifierMask.LeftAlt, KeyCode.VcDown,
            () => ManualScrollContent(new Vector(0, _delta)));
        GlobalHotkeyManager.BindHotkey("scroll_left", ModifierMask.LeftCtrl | ModifierMask.LeftAlt, KeyCode.VcLeft,
            () => ManualScrollContent(new Vector(-_delta, 0)));
        GlobalHotkeyManager.BindHotkey("scroll_right", ModifierMask.LeftCtrl | ModifierMask.LeftAlt, KeyCode.VcRight,
            () => ManualScrollContent(new Vector(_delta, 0)));

        foreach (var (functionName, function) in FunctionRegistry.FunctionBindings)
        {
            var hotkey = _configService?.Get<ushort[]>("control." + functionName);
            if (hotkey is { Length: > 1 })
            {
                GlobalHotkeyManager.BindHotkey(
                    functionName,
                    (ModifierMask)hotkey[0],
                    (KeyCode)hotkey[1],
                    function);
            }
        }
    }

    private void RegisterFunctions()
    {
        FunctionRegistry.RegisterFunction("open_chat_window", ToggleShowChatWindow);
        FunctionRegistry.RegisterFunction("take_screenshot", TakeScreenshot);
        FunctionRegistry.RegisterFunction("start_over", StartOver);
        FunctionRegistry.RegisterFunction("ask_ai", AskAI);
        FunctionRegistry.RegisterFunction("quit_app", QuitApplication);
        FunctionRegistry.RegisterFunction("click_through", ToggleClickThrough);
    }

    public void ToggleClickThrough()
    {
        if (_chatWindow != null && _chatWindowViewModel != null)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _chatWindowViewModel.IgnoreMouseEvents = !_chatWindowViewModel.IgnoreMouseEvents;

                ConfigureWindowBehaviors(_chatWindow, new WindowBehaviorOptions
                {
                    ContentProtection = _chatWindowViewModel.ContentProtection,
                    OverlayWindow = _chatWindowViewModel.OverlayWindow,
                    IgnoreMouseEvents = _chatWindowViewModel.IgnoreMouseEvents,
                });
                _chatWindow.SystemDecorations = _chatWindowViewModel.IgnoreMouseEvents
                    ? SystemDecorations.None
                    : SystemDecorations.Full;
                _chatWindow.TransparencyLevelHint = _chatWindowViewModel.IgnoreMouseEvents
                    ? [WindowTransparencyLevel.Transparent]
                    : [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur];
                _chatWindow.RequestedThemeVariant = _chatWindowViewModel.IgnoreMouseEvents
                    ? ThemeVariant.Dark
                    : ThemeVariant.Default;
            });
        }
    }

    public void UpdateChatWindowPosition(PixelPoint newPosition)
    {
        if (_chatWindow != null && _chatWindowViewModel != null)
        {
            Dispatcher.UIThread.InvokeAsync(() => { _chatWindow.Position = newPosition; });
        }
    }

    private void AskAI()
    {
        _chatWindowViewModel?.AskQuestion();
    }

    public async Task<Bitmap?> TakeScreenshotAsync()
    {
        var screenCapturer = ServiceProviderBuilder.ServiceProvider?.GetService<IScreenCapturer>();
        if (screenCapturer != null)
        {
            return await screenCapturer.CaptureScreen(1920, 1080);
        }

        return null;
    }

    public async void TakeScreenshot()
    {
        try
        {
            if (_chatWindowViewModel != null)
            {
                var bitmap = await TakeScreenshotAsync();
                _chatWindowViewModel.ImageSource = bitmap;
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Failed to take screenshot: {e.Message}");
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _lifetime = desktop;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task CheckUpdates()
    {
        if (_sparkle != null)
            await _sparkle.CheckForUpdatesAtUserRequest();
    }


    private void SetUpUpdater()
    {
        var osName = OperatingSystem.IsMacOS() ? "macos" :
            OperatingSystem.IsWindows() ? "windows" :
            "unknown";
        var url = $"https://arctan95.github.io/Anu/publish/{osName}/appcast.xml";
        using var iconStream = AssetLoader.Open(new Uri("avares://Anu.Core/Assets/AppIcon.ico"));
        using var pubStream = AssetLoader.Open(new Uri("avares://Anu.Core/Assets/app_update.pub"));
        using var reader = new StreamReader(pubStream, Encoding.UTF8);

        _sparkle = new CustomSparkleUpdater(url, new Ed25519Checker(SecurityMode.Strict, reader.ReadToEnd()))
        {
            UIFactory = new NetSparkleUpdater.UI.Avalonia.UIFactory(new WindowIcon(iconStream)),
            LogWriter = new LogWriter(LogWriterOutputMode.Console),
            CheckServerFileName = false
        };
        var dler = new WebRequestAppCastDataDownloader(_sparkle.LogWriter) { TrustEverySSLConnection = true };
        _sparkle.AppCastDataDownloader = dler;
        StartSparkle();
    }

    private async void StartSparkle()
    {
        try
        {
            if (_sparkle != null)
            {
                await _sparkle.StartLoop(_settingsWindowViewModel?.AutoCheckForUpdates ?? true);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Failed to start sparkle: {e.Message}");
        }
    }

    public void OpenChatWindowActivated()
    {
        if (_chatWindow == null)
        {
            InitAndShowChatWindow(true);
        }

        ForceActivateChatWindow();
    }

    public void OpenChatWindow()
    {
        if (_chatWindow == null)
        {
            InitAndShowChatWindow();
        }
    }

    public void InitAndShowChatWindow(bool forceActivated = false)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            InitChatWindow(forceActivated);
            _chatWindow?.Show();
        });
    }


    private void InitChatWindow(bool forceActivated)
    {
        if (_chatWindow == null)
        {
            if (_chatWindowViewModel != null)
            {
                _chatWindow = new ChatWindow
                {
                    Topmost = true,
                    CanResize = true,
                    ShowInTaskbar = false,
                    Width = 450,
                    Height = 800,
                    ShowActivated = forceActivated,
                    Background = Brushes.Transparent,
                    ExtendClientAreaToDecorationsHint = true,
                    ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome,
                    SystemDecorations = forceActivated ? SystemDecorations.Full : SystemDecorations.None,
                    TransparencyLevelHint = forceActivated
                        ? [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur]
                        : [WindowTransparencyLevel.Transparent],
                    DataContext = _chatWindowViewModel
                };

                if (_settingsWindowViewModel != null)
                {
                    _chatWindowViewModel.ContentProtection = _settingsWindowViewModel.ContentProtection;
                    _chatWindowViewModel.OverlayWindow = false;
                    _chatWindowViewModel.IgnoreMouseEvents = !forceActivated;
                }

                ConfigureWindowBehaviors(_chatWindow, new WindowBehaviorOptions
                {
                    ContentProtection = _chatWindowViewModel.ContentProtection,
                    OverlayWindow = _chatWindowViewModel.OverlayWindow,
                    IgnoreMouseEvents = _chatWindowViewModel.IgnoreMouseEvents
                });
                
                _chatWindow.RequestedThemeVariant = _chatWindowViewModel.IgnoreMouseEvents
                    ? ThemeVariant.Dark
                    : ThemeVariant.Default;
            }
        }
    }

    public void HideChatWindow()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _chatWindow?.Close();
            _chatWindow = null;
        });
    }

    public void ToggleShowChatWindow()
    {
        if (_chatWindow != null)
        {
            HideChatWindow();
        }
        else
        {
            InitAndShowChatWindow(true);
        }
    }

    public void ShowSettingsWindow()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_settingsWindowViewModel is { SettingsWindowShown: false })
            {
                var window = new SettingsWindow
                {
                    Topmost = true,
                    Background = Brushes.Transparent,
                    TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur],
                    DataContext = _settingsWindowViewModel
                };
                window.Show();
                _settingsWindowViewModel.SettingsWindowShown = true;
            }
        });
    }

    public void StartOver()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_chatWindowViewModel != null)
            {
                _chatWindowViewModel.ClearInput();
                _chatWindowViewModel.ClearScreen();
                _chatWindowViewModel.EndConversation();
            }
        });
        ChatService.ResetConversationContext();
    }

    public void ForceActivateChatWindow()
    {
        if (_chatWindow != null)
        {
            Dispatcher.UIThread.InvokeAsync(() => { _chatWindow.Activate(); });
        }
    }

    public async Task<Bitmap?> LoadClipboardImageAsync()
    {
        if (OperatingSystem.IsWindows())
            return await ClipboardAvalonia.GetImageAsync();

        var clipboard = _chatWindow?.Clipboard;
        if (clipboard == null)
            return null;

        var formats = await clipboard.GetFormatsAsync();
        if (formats == null || formats.Length == 0)
            return null;

        string[] preferredFormats = ["png", "image/png"];
        foreach (var format in formats.Where(f => !string.IsNullOrWhiteSpace(f)))
        {
            if (preferredFormats.Any(candidate =>
                    format.Contains(candidate, StringComparison.OrdinalIgnoreCase)))
            {
                var data = await clipboard.GetDataAsync(format);
                if (data is byte[] bytes)
                {
                    try
                    {
                        using var stream = new MemoryStream(bytes);
                        return new Bitmap(stream);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to load image from clipboard: {e.Message}");
                    }
                }
            }
        }

        return null;
    }

    public void ConfigureWindowBehaviors(Window window, WindowBehaviorOptions options)
    {
        var windowConfigurator = ServiceProviderBuilder.ServiceProvider?.GetService<IWindowConfigurator>();
        windowConfigurator?.ConfigureWindow(window, options);
    }

    private void TrayIcon_OnClicked(object? sender, EventArgs e)
    {
        ToggleWindowState();
    }

    public void ManualScrollContent(Vector offset)
    {
        if (_chatWindowViewModel != null)
        {
            _chatWindowViewModel.ScrollContent(offset);
        }
    }

    public void ToggleFollowPointer()
    {
        if (_chatWindowViewModel != null)
        {
            _chatWindowViewModel.ToggleFollowPointer();
        }
    }

    public void ToggleWindowState()
    {
        if (_chatWindow == null)
        {
            InitAndShowChatWindow();
        }
        else
        {
            HideChatWindow();
        }
    }

    public void QuitApplication()
    {
        Dispatcher.UIThread.InvokeAsync(() => { _lifetime?.Shutdown(); });
    }
}