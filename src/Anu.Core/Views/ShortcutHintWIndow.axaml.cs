using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Anu.Core.Views;

public partial class ShortcutHintWindow : Window
{
    public ShortcutHintWindow()
    {
        InitializeComponent();
    }

    private void PositionWindowAtLeftBottom()
    {
        var screen = Screens.Primary;
        if (screen != null)
        {
            Position = new PixelPoint(10, screen.Bounds.Y + screen.Bounds.Bottom - (int)Height - 10);
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        PositionWindowAtLeftBottom();
        base.OnOpened(e);
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        Opacity = 1;
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        Opacity = 0;
    }
}