using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Anu.Core.Services;
using Anu.Core.Utilities;
using Anu.Core.ViewModels;

namespace Anu.Core.Views;

public partial class SettingsWindow : Window
{
    private readonly Key[] _modifierKeys =
    [
        Key.LeftShift,
        Key.RightShift,
        Key.LeftCtrl,
        Key.RightCtrl,
        Key.LeftAlt,
        Key.RightAlt,
        Key.LWin,
        Key.RWin,
    ];

    public SettingsWindow()
    {
        InitializeComponent();
    }

    public void OnTextBoxFocused(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Watermark = string.Empty;
            textBox.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
            if (DataContext is SettingsWindowViewModel viewModel)
            {
                string functionName = StringExtensions.ToSnakeCase(textBox.Name!);
                GlobalHotkeyRecorder.StartRecording(hotkey =>
                {
                    var key = KeyConvertor.ToKey(hotkey.Key);
                    var modifier = KeyConvertor.ToKeyModifier(hotkey.Modifier);
                    if (key != Key.None && modifier != KeyModifiers.None)
                    {
                        hotkey.SetFunctionBinding(FunctionRegistry.GetFunction(functionName));
                        viewModel.RecordHotKey(functionName, hotkey);
                        Dispatcher.UIThread.InvokeAsync(() =>
                            textBox.Text = PlatformKeyGestureConverter.ToPlatformString(new KeyGesture(key, modifier)));
                    }
                });
            }
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            if (!_modifierKeys.Contains(e.Key))
            {
                textBox.Text = PlatformKeyGestureConverter.ToPlatformString(new KeyGesture(e.Key, e.KeyModifiers));
            }
        }

        e.Handled = true;
    }

    public void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Watermark = "Press shortcut";
            GlobalHotkeyRecorder.StopRecording();
        }
    }

    private async void OnEditMcpServersClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is SettingsWindowViewModel viewModel)
            {
                await viewModel.EditMcpServers(this);
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }
}