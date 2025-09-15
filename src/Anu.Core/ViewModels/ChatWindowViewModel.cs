using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Anu.Core.Models;
using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Anu.Core.Services;

namespace Anu.Core.ViewModels;

public partial class ChatWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private Bitmap? _imageSource;
    [ObservableProperty]
    private string _userPrompt = "";
    [ObservableProperty]
    private string _lastRequestId = "";
    [ObservableProperty]
    private bool _messageRequested;
    [ObservableProperty]
    private bool _messageStreaming;
    [ObservableProperty]
    private int _cursorPositionX;
    [ObservableProperty]
    private int _cursorPositionY;
    [ObservableProperty]
    private Vector _scrollValue = Vector.Zero;
    [ObservableProperty]
    private Size _extentSize;
    [ObservableProperty]
    private Size _viewPortSize;
    [ObservableProperty]
    private string _chatWindowOpacity = "0.5";
    [ObservableProperty]
    private bool _followPointer;
    [ObservableProperty]
    private double _chatWindowWidth;
    [ObservableProperty]
    private double _chatWindowHeight;
    [ObservableProperty]
    private double _screenWidth;
    [ObservableProperty]
    private double _screenHeight;
    [ObservableProperty]
    private bool _reasoning;
    [ObservableProperty]
    private bool _computerUse;
    [ObservableProperty]
    private bool _contentProtection = true;
    [ObservableProperty]
    private bool _overlayWindow = true;
    [ObservableProperty]
    private bool _ignoreMouseEvents = true;
    [ObservableProperty]
    private bool _isMenubarVisible;
    [ObservableProperty]
    private bool _arcylicEnabled;

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = new();

    private ChatMessageViewModel? _assistantMessageInProgress;

    partial void OnIgnoreMouseEventsChanged(bool value)
    {
        ArcylicEnabled = !value;
    }

    public void AddUserMessage(string text)
    {
        var message = ChatMessageViewModel.CreateUserMessage(text);
        Messages.Add(message);
    }

    public void AddUserMessage(Bitmap? image)
    {
        var imagePart = MessageContentPart.CreateImagePart(image);
        var message = ChatMessageViewModel.CreateUserMessage([imagePart]);
        Messages.Add(message);
    }

    public void AddErrorMessage(string text)
    {
        AddAssistantMessage(text);
    }

    public void AddAssistantMessage(string text)
    {
        var message = ChatMessageViewModel.CreateAssistantMessage(text);
        Messages.Add(message);
    }
    
    public void UpdateLastRequestId(string requestId)
    {
        LastRequestId = requestId;
    }

    public void StartAssistantResponse()
    {
        MessageStreaming = true;
        _assistantMessageInProgress = ChatMessageViewModel.CreateAssistantMessage(string.Empty);
        Messages.Add(_assistantMessageInProgress);
    }

    public void AppendAssistantText(string chunk)
    {
        if (_assistantMessageInProgress == null)
        {
            StartAssistantResponse();
        }
        _assistantMessageInProgress!.MessageContent.Add(MessageContentPart.CreateTextPart(chunk));
    }

    public void EndAssistantResponse()
    {
        MessageStreaming = false;
        _assistantMessageInProgress = null;
    }

    public async Task ReadClipboardImage()
    {
        if (Application.Current is App app)
        {
            ImageSource = await app.LoadClipboardImageAsync();
        }
    }

    public async Task TakeScreenshot()
    {
        if (Application.Current is App app)
        {
            ImageSource = await app.TakeScreenshotAsync();
        }
    }

    public async Task AskQuestion(bool enableConversationMemory = false)
    {
        await ChatService.Ask(enableConversationMemory);
    }

    public async Task SendOrStop()
    {
        if (MessageRequested)
        {
            EndConversation();
        }
        else
        {
            await SendMessage();
        }
    }

    public void StartConversation()
    {
        MessageRequested = true;
    }
    
    public void EndConversation()
    {
        MessageRequested = false;
        if (!string.IsNullOrEmpty(LastRequestId))
        {
            ChatService.StopAIResponseStream(LastRequestId);
        }
    }

    public async Task SendMessage()
    {
        await AskQuestion(true);
    }

    public void OnMouseMoved(int mouseX, int mouseY)
    {
        CursorPositionX = mouseX;
        CursorPositionY = mouseY;

        if (!FollowPointer)
        {
            return;
        }

        int deltaX = -(int)(ChatWindowWidth / 2);
        int deltaY = 20;

        if (Application.Current is App app)
        {
            app.UpdateChatWindowPosition(new PixelPoint(mouseX + deltaX, mouseY + deltaY));
        }
    }

    public void ClearScreen()
    {
        Messages.Clear();
        _assistantMessageInProgress = null;
    }

    public void ClearInput()
    {
        UserPrompt = string.Empty;
        DeleteImage();
    }

    public void DeleteImage()
    {
        ImageSource = null;
    }

    public void CloseWindow()
    {
        if (Application.Current is App app)
        {
            app.HideChatWindow();
        }
    }

    public void ToggleFollowPointer()
    {
        FollowPointer = !FollowPointer;
    }

    public void ScrollContent(Vector offset)
    {
        var newX = ScrollValue.X + offset.X;
        var newY = ScrollValue.Y + offset.Y;

        var maxX = Math.Max(0, ExtentSize.Width  - ViewPortSize.Width);
        var maxY = Math.Max(0, ExtentSize.Height - ViewPortSize.Height);

        newX = Math.Clamp(newX, 0, maxX);
        newY = Math.Clamp(newY, 0, maxY);

        ScrollValue = new Vector(newX, newY);
    }

    public void ShowMissingApiKeyHint()
    {
        Messages.Add(ChatMessageViewModel.CreateAssistantMessage("Please configure your AI provider's API key in the settings."));
    }

}