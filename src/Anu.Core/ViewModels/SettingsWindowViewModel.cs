using System;
using System.Reflection;
using Avalonia.Controls.Converters;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Anu.Core.Models;
using Anu.Core.Services;
using Anu.Core.Utilities;
using Microsoft.Extensions.DependencyInjection;
using SharpHook.Native;

namespace Anu.Core.ViewModels;

public partial class SettingsWindowViewModel: ViewModelBase
{
    private readonly ConfigService? _configService;
    
    [ObservableProperty]
    private string? _systemPrompt;
    [ObservableProperty]
    private string? _userPrompt;
    [ObservableProperty]
    private string? _apiKey;
    [ObservableProperty]
    private string? _endpoint;
    [ObservableProperty]
    private string? _model;
    [ObservableProperty]
    private string? _chatWindowKey;
    [ObservableProperty]
    private string? _screenshotKey;
    [ObservableProperty]
    private string? _startOverKey;
    [ObservableProperty]
    private string? _askAIKey;
    [ObservableProperty]
    private string? _followPointerKey;
    [ObservableProperty]
    private string? _clickThroughKey;
    [ObservableProperty]
    private string? _quitAppKey;
    [ObservableProperty]
    private bool _settingsWindowShown;
    [ObservableProperty]
    private bool _autoCheckForUpdates;
    [ObservableProperty]
    private bool _startOnBoot;
    [ObservableProperty]
    private string? _language;
    
    // global app settings
    [ObservableProperty]
    private int _screenMaxWidth;
    [ObservableProperty]
    private int _screenMaxHeight;
    [ObservableProperty]
    private bool _contentProtection = true;
    [ObservableProperty]
    private bool _overlayWindow = true;
    [ObservableProperty]
    private bool _ignoreMouseEvents = true;
    [ObservableProperty]
    private string _alias = "https://github.com/arctan95/Anu";
    [ObservableProperty]
    private string _url = "https://github.com/arctan95/Anu";
    [ObservableProperty]
    private string _version = $"Version {Assembly.GetEntryAssembly()?.GetName().Version}";
    [ObservableProperty]
    private string _copyright = $"Copyright © 2025-{DateTime.Now.Year} arctan95";
    
    partial void OnAutoCheckForUpdatesChanged(bool value) => _configService?.Set("general.auto_check_for_updates", value);

    partial void OnStartOnBootChanged(bool value) => _configService?.Set("general.start_on_boot", value);
    
    partial void OnSystemPromptChanged(string? value) => _configService?.Set("ai.default_system_prompt", value);
    
    partial void OnUserPromptChanged(string? value) =>  _configService?.Set("ai.default_user_prompt", value);

    partial void OnApiKeyChanged(string? value) => _configService?.Set("ai.api_key", value);
    partial void OnEndpointChanged(string? value) => _configService?.Set("ai.endpoint", value);

    partial void OnModelChanged(string? value) => _configService?.Set("ai.model", value);
    
    public SettingsWindowViewModel()
    {
        _configService = ServiceProviderBuilder.ServiceProvider?.GetRequiredService<ConfigService>();

        StartOnBoot = Convert.ToBoolean(_configService?.Get<bool>("general.start_on_boot"));
        AutoCheckForUpdates = Convert.ToBoolean(_configService?.Get<bool>("general.auto_check_for_updates"));
        SystemPrompt = _configService?.Get<string>("ai.default_system_prompt") ?? string.Empty;
        UserPrompt = _configService?.Get<string>("ai.default_user_prompt") ?? string.Empty;
        ApiKey = _configService?.Get<string>("ai.api_key") ?? string.Empty;
        Endpoint = _configService?.Get<string>("ai.endpoint") ?? string.Empty;
        Model = _configService?.Get<string>("ai.model") ?? string.Empty;

        var chatWindowHotkey = _configService?.Get<ushort[]>("control.open_chat_window");
        var screenshotHotKey = _configService?.Get<ushort[]>("control.take_screenshot");
        var askAIHotKey = _configService?.Get<ushort[]>("control.ask_ai");
        var startOverHotkey = _configService?.Get<ushort[]>("control.start_over");
        var quitAppHotkey = _configService?.Get<ushort[]>("control.quit_app");
        var clickThroughHotkey = _configService?.Get<ushort[]>("control.click_through");
        if (chatWindowHotkey is { Length: > 1 })
        {
            ChatWindowKey = PlatformKeyGestureConverter.ToPlatformString(new KeyGesture(KeyConvertor.ToKey((KeyCode)chatWindowHotkey[1]), KeyConvertor.ToKeyModifiers((ModifierMask)chatWindowHotkey[0])));
        }
        if (screenshotHotKey is { Length: > 1 })
        {
            ScreenshotKey = PlatformKeyGestureConverter.ToPlatformString(new KeyGesture(KeyConvertor.ToKey((KeyCode)screenshotHotKey[1]), KeyConvertor.ToKeyModifiers((ModifierMask)screenshotHotKey[0])));
        }
        if (askAIHotKey is { Length: > 1 })
        {
            AskAIKey = PlatformKeyGestureConverter.ToPlatformString(new KeyGesture(KeyConvertor.ToKey((KeyCode)askAIHotKey[1]), KeyConvertor.ToKeyModifiers((ModifierMask)askAIHotKey[0])));
        }
        if (startOverHotkey is { Length: > 1 })
        {
            StartOverKey = PlatformKeyGestureConverter.ToPlatformString(new KeyGesture(KeyConvertor.ToKey((KeyCode)startOverHotkey[1]), KeyConvertor.ToKeyModifiers((ModifierMask)startOverHotkey[0])));
        }
        if (clickThroughHotkey is { Length: > 1 })
        {
            ClickThroughKey = PlatformKeyGestureConverter.ToPlatformString(new KeyGesture(KeyConvertor.ToKey((KeyCode)clickThroughHotkey[1]), KeyConvertor.ToKeyModifiers((ModifierMask)clickThroughHotkey[0])));
        }
        if (quitAppHotkey is { Length: > 1 })
        {
            QuitAppKey = PlatformKeyGestureConverter.ToPlatformString(new KeyGesture(KeyConvertor.ToKey((KeyCode)quitAppHotkey[1]), KeyConvertor.ToKeyModifiers((ModifierMask)quitAppHotkey[0])));
        }
    }

    public void RecordHotKey(string functionName, GlobalHotkey hotkey)
    {
        hotkey.SetFunctionBinding(FunctionRegistry.GetFunction(functionName));
        GlobalHotkeyManager.BindHotkey(functionName, hotkey);
        _configService?.Set("control." + functionName, new[] {(ushort)hotkey.Modifier, (ushort)hotkey.Key });
    }
}