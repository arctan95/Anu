using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Anu.Core.ViewModels;
using Avalonia.Reactive;

namespace Anu.Core.Views;

public partial class ChatWindow : Window
{
    public ChatWindow()
    {
        InitializeComponent();
        Activated += OnActivated;
        Deactivated += OnDeActivated;
        Closing += OnClosing;
        Resized += OnResized;

        ChatScrollViewer
            .GetObservable(ScrollViewer.ExtentProperty)
            .Subscribe(new AnonymousObserver<Size>(_ =>
            {
                if (DataContext is ChatWindowViewModel { MessageStreaming: true })
                {
                    ChatScrollViewer.ScrollToEnd();
                }
            }));
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (DataContext is ChatWindowViewModel vm)
        {
            if (!vm.IgnoreMouseEvents)
            {
                vm.IsMenubarVisible = true;
            }
        }
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is ChatWindowViewModel vm)
        {
            vm.IsMenubarVisible = false;
        }
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        if (DataContext is ChatWindowViewModel vm)
        {
            vm.ChatWindowOpacity = "0.5";
            vm.IsMenubarVisible = true;
            UserPrompt.Focus();
        }
    }

    private void OnDeActivated(object? sender, EventArgs e)
    {
        if (DataContext is ChatWindowViewModel vm)
        {
            vm.ChatWindowOpacity = "0.4";
            vm.IsMenubarVisible = false;
        }
    }
    
    private void OnResized(object? sender, WindowResizedEventArgs e)
    {
        if (DataContext is ChatWindowViewModel vm)
        {
            vm.ChatWindowWidth = e.ClientSize.Width;
            vm.ChatWindowHeight = e.ClientSize.Height;
        }
    }
    
    protected override void OnOpened(EventArgs e)
    {
        DetectScreenSize();
        LoadMcpServers();
        base.OnOpened(e);
    }

    private void LoadMcpServers()
    {
        if (DataContext is ChatWindowViewModel vm)
        {
            vm.LoadMcpServers();
        }
    }

    private void DetectScreenSize()
    {
        var screen = Screens.Primary;
        if (screen != null)
        {
            if (DataContext is ChatWindowViewModel viewModel)
            {
                viewModel.ScreenWidth = screen.Bounds.Width;
                viewModel.ScreenHeight = screen.Bounds.Height;
                Position = new PixelPoint((int)(screen.Bounds.X + (screen.Bounds.Width - Width) / 2),
                    (int)(screen.Bounds.Y + (screen.Bounds.Height - Height) / 2));
            }
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.CloseChatWindow();
        }
    }

    private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
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