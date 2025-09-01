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
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Clowd.Clipboard;
using Anu.Core.Views;
using CommunityToolkit.Mvvm.Input;
using Anu.Core.Models;
using Anu.Core.Services;
using Anu.Core.ViewModels;
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
    private Window? _shortcutHintWindow;
    private SparkleUpdater? _sparkle;

    public ICommand SettingsCommand { get; }

    public ICommand CheckUpdatesCommand { get; }
    public ICommand MainCommand { get; }
    public ICommand QuitCommand { get; }

    public App()
    {
        QuitCommand = new RelayCommand(QuitApplication);
        SettingsCommand = new RelayCommand(ShowSettingsWindow);
        MainCommand = new RelayCommand(OpenChatWindowForInput);
        CheckUpdatesCommand = new AsyncRelayCommand(CheckUpdates);
        _chatWindowViewModel = ServiceProviderBuilder.ServiceProvider?.GetRequiredService<ChatWindowViewModel>();
        _settingsWindowViewModel = ServiceProviderBuilder.ServiceProvider?.GetRequiredService<SettingsWindowViewModel>();
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
        // predefined global hotkeys
        GlobalHotkeyManager.BindHotkey("hide_chat_window", ModifierMask.None, KeyCode.VcEscape, HideChatAndShortcutHintWindow);

        foreach (var (functionName, function) in FunctionRegistry._functionBindings)
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
                // fix text ghosting in click-through mode by remove decorations
                _chatWindow.SystemDecorations = _chatWindowViewModel.IgnoreMouseEvents ? SystemDecorations.None : SystemDecorations.Full;
                _chatWindow.TransparencyLevelHint = _chatWindowViewModel.IgnoreMouseEvents ? [WindowTransparencyLevel.Transparent] : [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur];
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

    public void OpenChatWindowForInput()
    {
        if (_chatWindow == null)
        {
            ShowChatWindow(true);
        }
        ForceActivateChatWindow();
    }

    public void ShowChatWindow(bool forceActivated = false)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            InitChatWindow(forceActivated);
            _chatWindow?.Show();
        });
    }

    public void ShowShortcutHintWindow()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            InitShortcutHintWindow();
            _shortcutHintWindow?.Show();
        });
    }

    private void InitShortcutHintWindow()
    {
        if (_shortcutHintWindow == null)
        {
            _shortcutHintWindow = new ShortcutHintWindow
            {
                Topmost = true,
                CanResize = false,
                ShowInTaskbar = false,
                ShowActivated = false,
                Background = Brushes.Transparent,
                TransparencyLevelHint = [WindowTransparencyLevel.Transparent],
                SystemDecorations = SystemDecorations.None,
            };
            ConfigureWindowBehaviors(_shortcutHintWindow, new WindowBehaviorOptions
            {
                ContentProtection = true,
                OverlayWindow = true,
                IgnoreMouseEvents = true
            });
        }
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
                    SystemDecorations = forceActivated ? SystemDecorations.Full : SystemDecorations.None,                    TransparencyLevelHint = forceActivated ? [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur] : [WindowTransparencyLevel.Transparent],
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
            }
        }
    }

    public void HideChatAndShortcutHintWindow()
    {
        HideChatWindow();
        HideShortcutHintWindow();
    }

    public void ShowChatAndShortcutHintWindow()
    {
        ShowChatWindow();
        ShowShortcutHintWindow();
    }

    public void HideChatWindow()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _chatWindow?.Close();
            _chatWindow = null;
        });
    }

    public void HideShortcutHintWindow()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _shortcutHintWindow?.Close();
            _shortcutHintWindow = null;
        });
    }

    public void ToggleShowChatWindow()
    {
        if (_chatWindow != null)
        {
            HideChatAndShortcutHintWindow();
        }
        else
        {
            ShowChatAndShortcutHintWindow();
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
                _chatWindowViewModel.MdText = String.Empty;
                _chatWindowViewModel.ImageSource = null;
                _chatWindowViewModel.UserMessage = string.Empty;
                _chatWindowViewModel.StopAIResponse();
            }
        });
        AIChat.ResetConversation();
    }
    public void ForceActivateChatWindow()
    {
        if (_chatWindow != null)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _chatWindow.Activate();
            });
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

    public void QuitApplication()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _lifetime?.Shutdown();
        });
    }
}