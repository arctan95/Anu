using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Anu.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Anu.Core.ViewModels;

public partial class ChatMessageViewModel : ViewModelBase
{
    public ObservableCollection<MessageContentPart> MessageContent { get; }

    [ObservableProperty]
    private string _message = string.Empty;

    public MessageRole Role { get; set; }

    private ChatMessageViewModel(MessageRole role, string content)
    {
        Role = role;
        MessageContent = new ObservableCollection<MessageContentPart>();
        MessageContent.CollectionChanged += OnMessageContentChanged;
        MessageContent.Add(MessageContentPart.CreateTextPart(content));
    }

    private ChatMessageViewModel(MessageRole role, IEnumerable<MessageContentPart> contentParts)
    {
        Role = role;
        MessageContent = new ObservableCollection<MessageContentPart>(contentParts);
        MessageContent.CollectionChanged += OnMessageContentChanged;
    }

    private void OnMessageContentChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Message = string.Concat(
            MessageContent
                .Where(p => p.Kind == MessageContentPartKind.Text)
                .Select(p => p.Text));
    }

    public static ChatMessageViewModel CreateUserMessage(string content)
    {
        return new ChatMessageViewModel(MessageRole.User, content);
    }

    public static ChatMessageViewModel CreateUserMessage(IEnumerable<MessageContentPart> contentParts) =>
        new(MessageRole.User, contentParts);


    public static ChatMessageViewModel CreateAssistantMessage(string content)
    {
        return new ChatMessageViewModel(MessageRole.Assistant, content);
    }

    public static ChatMessageViewModel CreateAssistantMessage(IEnumerable<MessageContentPart> contentParts) =>
        new(MessageRole.Assistant, contentParts);
}