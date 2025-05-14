using System;
using System.Diagnostics;
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

    private readonly Key[] _modifierKeys =  [
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
                    var modifiers = KeyConvertor.ToKeyModifiers(hotkey.Modifier);
                    if (key != Key.None && modifiers != KeyModifiers.None)
                    {
                        viewModel.RecordHotKey(functionName, hotkey);
                        Dispatcher.UIThread.InvokeAsync(() => textBox.Text = PlatformKeyGestureConverter.ToPlatformString(new KeyGesture(key, modifiers)));
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

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is SettingsWindowViewModel viewModel)
        {
            viewModel.SettingsWindowShown = false;
        }
        base.OnClosing(e);
    }

    private void OpenUr(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsWindowViewModel viewModel)
        {
            if (OperatingSystem.IsWindows())
            {
                using var proc = new Process();
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.FileName = viewModel.Url;
                proc.Start();

                return;
            }

            if (OperatingSystem.IsLinux())
            {
                Process.Start("x-www-browser", viewModel.Url);
                return;
            }

            if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", viewModel.Url);
            }
        }
    }
}