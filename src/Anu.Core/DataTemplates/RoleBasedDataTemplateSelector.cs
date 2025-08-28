using System.Collections.Generic;
using Anu.Core.ViewModels;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Anu.Core.DataTemplates;

public class RoleBasedDataTemplateSelector : IDataTemplate
{
    [Content] public Dictionary<string, IDataTemplate> Templates { get; } = new();

    public Control? Build(object? data)
    {
        return Templates[((ChatMessageViewModel)data!).Role.ToString()].Build(data);
    }

    public bool Match(object? data)
    {
        return data is ChatMessageViewModel;
    }
}