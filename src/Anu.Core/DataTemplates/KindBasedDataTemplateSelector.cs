using System.Collections.Generic;
using Anu.Core.Models;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Anu.Core.DataTemplates;

public class KindBasedDataTemplateSelector : IDataTemplate
{
    [Content] public Dictionary<string, IDataTemplate> Templates { get; } = new();

    public Control? Build(object? data)
    {
        return Templates[((MessageContentPart)data!).Kind.ToString()].Build(data);
    }

    public bool Match(object? data)
    {
        return data is MessageContentPart;
    }
}