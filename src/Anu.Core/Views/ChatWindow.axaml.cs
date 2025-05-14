using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Anu.Core.ViewModels;

namespace Anu.Core.Views;

public partial class ChatWindow : Window
{
    public ChatWindow()
    {
        InitializeComponent();
        Activated += OnActivated;
        Deactivated += OnDeActivated;
        Resized += OnResized;
        Closing += OnClosing;
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        if (Application.Current is App app)
        {
            app.HideShortcutHintWindow();
        }
        if (DataContext is ChatWindowViewModel vm)
        {
            vm.ChatBoxOpacity = "0.5";
            ForceFocusUserPromptInput();
        }
    }

    private void OnResized(object? sender, WindowResizedEventArgs e)
    {
        if (DataContext is ChatWindowViewModel vm)
        {
            vm.ChatBoxHeight = e.ClientSize.Height;
            vm.ChatBoxWidth = e.ClientSize.Width;
        }
    }

    private void OnDeActivated(object? sender, EventArgs e)
    {
        if (DataContext is ChatWindowViewModel vm)
        {
            vm.ChatBoxOpacity = "0.4";
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        DetectScreenSize();
        base.OnOpened(e);
    }

    private void ForceFocusUserPromptInput()
    {
        UserPrompt.Focus();
    }

    private void DetectScreenSize()
    {
        var screen = Screens.Primary;
        if (screen != null)
        {
            if (DataContext is ChatWindowViewModel viewModel)
            {
                viewModel.WindowPositionX = Position.X;
                viewModel.WindowPositionY = Position.Y;
                viewModel.ScreenWidth = screen.Bounds.Width;
                viewModel.ScreenHeight = screen.Bounds.Height;
            }
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.HideChatAndShortcutHintWindow();
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else
            {
                BeginMoveDrag(e);
            }
        }
    }
}