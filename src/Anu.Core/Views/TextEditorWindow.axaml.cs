using System;
using Anu.Core.ViewModels;
using Avalonia.Controls;
using AvaloniaEdit.Document;

namespace Anu.Core.Views;

public partial class TextEditorWindow : Window
{
    public TextEditorWindow()
    {
        InitializeComponent();
        Opened += OnTextEditorWindowOpend;
        Closed += OnTextEditorWindowClose;
    }

    private void OnTextEditorWindowOpend(object? sender, EventArgs e)
    {
        if (DataContext is TextEditorWindowViewModel vm)
        {
            EmbeddedTextEditor.Document = new TextDocument(vm.Text);
        }
    }

    private void OnTextEditorWindowClose(object? sender, EventArgs e)
    {
        if (DataContext is TextEditorWindowViewModel vm)
        {
            _ = vm.SaveFile(EmbeddedTextEditor.Text);
        }
    }

    private void EmbeddedTextEditor_OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        if (DataContext is TextEditorWindowViewModel vm)
        {
            vm.Text = e.NewDocument.Text;
        }
    }
}