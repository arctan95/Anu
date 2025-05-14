using Avalonia.Input;
using Markdown.Avalonia;

namespace Anu.Core.Views;

public class CustomMarkdownScrollViewer : MarkdownScrollViewer
{
    public CustomMarkdownScrollViewer()
    {
        Plugins = new MdAvPlugins();
        Plugins.Plugins.Add(new ChatAISetup());
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        // Optionally, handle pointer pressed event to prevent focus
        e.Handled = true;
    }
}