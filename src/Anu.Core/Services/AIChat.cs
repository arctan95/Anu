using System;
using System.Buffers;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Anu.Core.Tools;
using Avalonia.Media.Imaging;
using Anu.Core.ViewModels;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;
using SharpHook.Native;

namespace Anu.Core.Services;

public class AIChat
{
    private static ChatWindowViewModel? _chatWindowViewModel;
    private static SettingsWindowViewModel? _settingsViewModel;
    private static ChatClient? _chatClient;
    private static readonly Dictionary<string, CancellationTokenSource> Cancellations = new();
    private static readonly List<ChatMessage> _messages = new();
    private static bool _initialized;

    public class StreamingChatToolCallsBuilder
    {
        private readonly Dictionary<int, string> _indexToToolCallId = [];
        private readonly Dictionary<int, string> _indexToFunctionName = [];
        private readonly Dictionary<int, SequenceBuilder<byte>> _indexToFunctionArguments = [];
        private int _nextIndex = -1;

        private int EnsureIndex(int index)
        {
            if (index > 0)
            {
                return index;
            }

            return Interlocked.Increment(ref _nextIndex);
        }

        private string EnsureToolCallId(string? toolCallId)
        {
            if (!string.IsNullOrWhiteSpace(toolCallId))
            {
                return toolCallId;
            }
            // Fix empty toolCallId such as Gemini.
            return $"call_{Guid.NewGuid()}";
        }

        public void Append(StreamingChatToolCallUpdate toolCallUpdate)
        {
            // Fix index is always 0 such as Gemini.
            int index = EnsureIndex(toolCallUpdate.Index);

            // Keep track of which tool call ID belongs to this update index.
            if (toolCallUpdate.ToolCallId != null)
            {
                _indexToToolCallId[index] = EnsureToolCallId(toolCallUpdate.ToolCallId);
            }

            // Keep track of which function name belongs to this update index.
            if (toolCallUpdate.FunctionName != null)
            {
                _indexToFunctionName[index] = toolCallUpdate.FunctionName;
            }

            // Keep track of which function arguments belong to this update index,
            // and accumulate the arguments as new updates arrive.
            if (toolCallUpdate.FunctionArgumentsUpdate != null && !toolCallUpdate.FunctionArgumentsUpdate.ToMemory().IsEmpty)
            {
                if (!_indexToFunctionArguments.TryGetValue(index, out SequenceBuilder<byte> argumentsBuilder))
                {
                    argumentsBuilder = new SequenceBuilder<byte>();
                    _indexToFunctionArguments[index] = argumentsBuilder;
                }

                argumentsBuilder.Append(toolCallUpdate.FunctionArgumentsUpdate);
            }
        }

        public IReadOnlyList<ChatToolCall> Build()
        {
            List<ChatToolCall> toolCalls = [];

            foreach ((int index, string toolCallId) in _indexToToolCallId)
            {
                ReadOnlySequence<byte> sequence = _indexToFunctionArguments[index].Build();

                ChatToolCall toolCall = ChatToolCall.CreateFunctionToolCall(
                    id: toolCallId,
                    functionName: _indexToFunctionName[index],
                    functionArguments: BinaryData.FromBytes(sequence.ToArray()));

                toolCalls.Add(toolCall);
            }

            return toolCalls;
        }
    }


    public class SequenceBuilder<T>
    {
        Segment _first;
        Segment _last;

        public void Append(ReadOnlyMemory<T> data)
        {
            if (_first == null)
            {
                Debug.Assert(_last == null);
                _first = new Segment(data);
                _last = _first;
            }
            else
            {
                _last = _last!.Append(data);
            }
        }

        public ReadOnlySequence<T> Build()
        {
            if (_first == null)
            {
                Debug.Assert(_last == null);
                return ReadOnlySequence<T>.Empty;
            }

            if (_first == _last)
            {
                Debug.Assert(_first.Next == null);
                return new ReadOnlySequence<T>(_first.Memory);
            }

            return new ReadOnlySequence<T>(_first, 0, _last!, _last!.Memory.Length);
        }

        private sealed class Segment : ReadOnlySequenceSegment<T>
        {
            public Segment(ReadOnlyMemory<T> items) : this(items, 0)
            {
            }

            private Segment(ReadOnlyMemory<T> items, long runningIndex)
            {
                Debug.Assert(runningIndex >= 0);
                Memory = items;
                RunningIndex = runningIndex;
            }

            public Segment Append(ReadOnlyMemory<T> items)
            {
                long runningIndex;
                checked { runningIndex = RunningIndex + Memory.Length; }
                Segment segment = new(items, runningIndex);
                Next = segment;
                return segment;
            }
        }
    }


    private static bool TryInitialize()
    {
        try
        {
            _chatWindowViewModel ??= ServiceProviderBuilder.ServiceProvider?.GetRequiredService<ChatWindowViewModel>();
            _settingsViewModel ??= ServiceProviderBuilder.ServiceProvider?.GetRequiredService<SettingsWindowViewModel>();

            if (string.IsNullOrWhiteSpace(_settingsViewModel?.Endpoint) ||
                string.IsNullOrWhiteSpace(_settingsViewModel?.ApiKey))
            {
                return false;
            }

            if (_chatClient == null)
            {
                var options = new OpenAIClientOptions
                {
                    Endpoint = new Uri(_settingsViewModel.Endpoint)
                };

                var openAiClient = new OpenAIClient(new ApiKeyCredential(_settingsViewModel.ApiKey), options);
                _chatClient = openAiClient.GetChatClient(_settingsViewModel.Model);
            }

            _initialized = true;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize AI chat service：{ex.Message}");
            return false;
        }
    }

    public static void StopAIResponseStream(string requestId)
    {
        if (!string.IsNullOrEmpty(requestId) && Cancellations.ContainsKey(requestId))
        {
            var cancellationTokenSource = Cancellations[requestId];
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested && _chatWindowViewModel != null)
            {
                cancellationTokenSource.Cancel();
                _chatWindowViewModel.MessageRequested = false;
                Cancellations.Remove(requestId);
            }
        }
    }

    private static async Task Ask(SystemChatMessage systemMessage, UserChatMessage userMessage)
    {
        ChatCompletionOptions options = new()
        {
            Tools =
            {
                InputTools.GetScreenSizeTool,
                InputTools.GetCurrentCursorPositionTool,
                InputTools.GetAllKeyNamesTool,
                InputTools.InputTextTool,
                InputTools.PressKeyTool,
                InputTools.LongPressKeyTool,
                InputTools.PressKeyCombinationTool,
                InputTools.LeftClickTool,
                InputTools.RightClickTool,
                InputTools.MiddleClickTool,
                InputTools.DoubleClickTool,
                InputTools.TripleClickTool,
                InputTools.ClickAtTool,
                InputTools.ClickAndDragTool,
                InputTools.MoveMouseTool,
                InputTools.MoveMouseRelativeTool,
                InputTools.WheelMouseTool,
                InputTools.TakeScreenshotTool
            },
        };

        if (_chatWindowViewModel != null)
        {
            // Empty input message
            _chatWindowViewModel.UserMessage = string.Empty;
            _chatWindowViewModel.ClearScreen();
            _chatWindowViewModel.UpdateText($"[USER]:");
            _chatWindowViewModel.UpdateText($"{userMessage.Content[0].Text}");
            _chatWindowViewModel.UpdateText(Environment.NewLine + Environment.NewLine);
            _chatWindowViewModel.UpdateText($"[AI]:");
            
            // Only add system message in first time
            if (_messages.Count == 0)
            {
                _messages.Add(systemMessage);
            }
            
            _messages.Add(userMessage);

            try
            {
                bool requiresAction;
                do
                {
                    requiresAction = false;
                    StringBuilder contentBuilder = new();
                    StreamingChatToolCallsBuilder toolCallsBuilder = new();

                    // Prepares
                    var requestId = Guid.NewGuid().ToString();
                    var cts = new CancellationTokenSource();
                    var cancelToken = cts.Token;
                    cancelToken.ThrowIfCancellationRequested();
                    Cancellations.Add(requestId, cts);
                    _chatWindowViewModel.LastRequestId = requestId;
                    _chatWindowViewModel.MessageRequested = true;

                    AsyncCollectionResult<StreamingChatCompletionUpdate>? completionUpdates =
                        _chatClient?.CompleteChatStreamingAsync(_messages, options, cancelToken);

                    if (completionUpdates != null)
                    {
                        await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                        {
                            // Accumulate the text content as new updates arrive.
                            foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
                            {
                                _chatWindowViewModel.UpdateText(contentPart.Text);
                                contentBuilder.Append(contentPart.Text);
                            }

                            // Build the tool calls as new updates arrive.
                            foreach (StreamingChatToolCallUpdate toolCallUpdate in completionUpdate.ToolCallUpdates)
                            {
                                _chatWindowViewModel.UpdateText(Environment.NewLine + Environment.NewLine);
                                _chatWindowViewModel.UpdateText($"[TOOL CALL]: {toolCallUpdate.FunctionName}: {toolCallUpdate.FunctionArgumentsUpdate}");
                                _chatWindowViewModel.UpdateText(Environment.NewLine + Environment.NewLine);
                                toolCallsBuilder.Append(toolCallUpdate);
                            }

                            switch (completionUpdate.FinishReason)
                            {
                                case ChatFinishReason.Stop:
                                    {
                                        // Add the assistant message to the conversation history.
                                        _messages.Add(new AssistantChatMessage(contentBuilder.ToString()));
                                        _chatWindowViewModel.MessageRequested = false;
                                        Cancellations.Remove(requestId);
                                        break;
                                    }
                                case ChatFinishReason.ToolCalls:
                                    {
                                        // First, collect the accumulated function arguments into complete tool calls to be processed
                                        IReadOnlyList<ChatToolCall> toolCalls = toolCallsBuilder.Build();

                                        // Next, add the assistant message with tool calls to the conversation history.
                                        AssistantChatMessage assistantMessage = new(toolCalls);

                                        if (contentBuilder.Length > 0)
                                        {
                                            assistantMessage.Content.Add(ChatMessageContentPart.CreateTextPart(contentBuilder.ToString()));
                                        }
                                        _messages.Add(assistantMessage);
                                        
                                        // Force close allow operations on screen
                                        if (Application.Current is App application && !_chatWindowViewModel.IgnoreMouseEvents)
                                        {
                                            application.ToggleClickThrough();
                                        }

                                        // Then, add a new tool message for each tool call to be resolved.
                                        foreach (ChatToolCall toolCall in toolCalls)
                                        {

                                            switch (toolCall.FunctionName)
                                            {
                                                case nameof(InputTools.GetScreenSize):
                                                    {
                                                        string size = InputTools.GetScreenSize();
                                                        _messages.Add(new ToolChatMessage(toolCall.Id, size));
                                                        break;
                                                    }

                                                case nameof(InputTools.GetCurrentCursorPosition):
                                                    {
                                                        string position = InputTools.GetCurrentCursorPosition();
                                                        _messages.Add(new ToolChatMessage(toolCall.Id, position));
                                                        break;
                                                    }

                                                case nameof(InputTools.GetAllKeyNames):
                                                    {
                                                        string allKeys = InputTools.GetAllKeyNames();
                                                        _messages.Add(new ToolChatMessage(toolCall.Id, allKeys));
                                                        break;
                                                    }

                                                case nameof(InputTools.InputText):
                                                    {
                                                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                                        if (argumentsJson.RootElement.TryGetProperty("text", out JsonElement text))
                                                        {
                                                            string result = InputTools.InputText(text.GetString());
                                                            _messages.Add(new ToolChatMessage(toolCall.Id, result));
                                                        }
                                                        break;
                                                    }


                                                case nameof(InputTools.PressKey):
                                                    {
                                                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                                        if (argumentsJson.RootElement.TryGetProperty("key", out JsonElement key) &&
                                                            Enum.TryParse<KeyCode>(key.GetString(), out var keyEnum))
                                                        {
                                                            string result = InputTools.PressKey(keyEnum);
                                                            _messages.Add(new ToolChatMessage(toolCall.Id, result));
                                                        }
                                                        break;
                                                    }
                                                
                                                case nameof(InputTools.LongPressKey):
                                                    {
                                                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                                        if (argumentsJson.RootElement.TryGetProperty("key", out JsonElement key) &&
                                                            argumentsJson.RootElement.TryGetProperty("duration_ms", out JsonElement durationMs) &&
                                                            Enum.TryParse<KeyCode>(key.GetString(), out var keyEnum))
                                                        {
                                                            string result = InputTools.LongPressKey(keyEnum, durationMs.GetInt32());
                                                            _messages.Add(new ToolChatMessage(toolCall.Id, result));
                                                        }
                                                        break;
                                                    }

                                                case nameof(InputTools.PressKeyCombination):
                                                    {
                                                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);

                                                        if (argumentsJson.RootElement.TryGetProperty("modifier", out JsonElement modifierElement) &&
                                                            argumentsJson.RootElement.TryGetProperty("key", out JsonElement keyElement) &&
                                                            Enum.TryParse<KeyCode>(modifierElement.GetString(), out var modifierEnum) &&
                                                            Enum.TryParse<KeyCode>(keyElement.GetString(), out var keyEnum))
                                                        {
                                                            string result = InputTools.PressKeyCombination(modifierEnum, keyEnum);
                                                            _messages.Add(new ToolChatMessage(toolCall.Id, result));
                                                        }
                                                        else
                                                        {
                                                            _messages.Add(new ToolChatMessage(toolCall.Id, "Invalid key combination"));
                                                        }
                                                        break;
                                                    }


                                                case nameof(InputTools.LeftClick):
                                                    {
                                                        string result = InputTools.LeftClick();
                                                        _messages.Add(new ToolChatMessage(toolCall.Id, result));
                                                        break;
                                                    }

                                                case nameof(InputTools.RightClick):
                                                    {
                                                        string result = InputTools.RightClick();
                                                        _messages.Add(new ToolChatMessage(toolCall.Id, result));
                                                        break;
                                                    }

                                                case nameof(InputTools.MiddleClick):
                                                    {
                                                        string result = InputTools.MiddleClick();
                                                        _messages.Add(new ToolChatMessage(toolCall.Id, result));
                                                        break;
                                                    }

                                                case nameof(InputTools.DoubleClick):
                                                    {
                                                        string result = InputTools.DoubleClick();
                                                        _messages.Add(new ToolChatMessage(toolCall.Id, result));
                                                        break;
                                                    }

                                                case nameof(InputTools.TripleClick):
                                                    {
                                                        string result = InputTools.TripleClick();
                                                        _messages.Add(new ToolChatMessage(toolCall.Id, result));
                                                        break;
                                                    }

                                                case nameof(InputTools.ClickAt):
                                                    {
                                                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                                        if (argumentsJson.RootElement.TryGetProperty("x",
                                                                out JsonElement x) &&
                                                            argumentsJson.RootElement.TryGetProperty("y",
                                                                out JsonElement y))
                                                        {
                                                            string result = InputTools.ClickAt(x.GetInt16(), y.GetInt16());
                                                            _messages.Add(new ToolChatMessage(toolCall.Id, result));
                                                        }
                                                        break;
                                                    }

                                                case nameof(InputTools.ClickAndDrag):
                                                    {
                                                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                                        if (argumentsJson.RootElement.TryGetProperty("startX", out JsonElement startX) &&
                                                            argumentsJson.RootElement.TryGetProperty("startY", out JsonElement startY) &&
                                                            argumentsJson.RootElement.TryGetProperty("endX", out JsonElement endX) &&
                                                            argumentsJson.RootElement.TryGetProperty("endY", out JsonElement endY))
                                                        {
                                                            string result = InputTools.ClickAndDrag(
                                                                startX.GetInt16(), startY.GetInt16(),
                                                                endX.GetInt16(), endY.GetInt16()
                                                            );
                                                            _messages.Add(new ToolChatMessage(toolCall.Id, result));
                                                        }
                                                        break;
                                                    }

                                                case nameof(InputTools.MoveMouse):
                                                    {
                                                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                                        if (argumentsJson.RootElement.TryGetProperty("x", out JsonElement x) &&
                                                            argumentsJson.RootElement.TryGetProperty("y", out JsonElement y))
                                                        {
                                                            string result = InputTools.MoveMouse(x.GetInt16(), y.GetInt16());
                                                            _messages.Add(new ToolChatMessage(toolCall.Id, result));
                                                        }
                                                        break;
                                                    }

                                                case nameof(InputTools.MoveMouseRelative):
                                                    {
                                                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                                        if (argumentsJson.RootElement.TryGetProperty("dx", out JsonElement dx) &&
                                                            argumentsJson.RootElement.TryGetProperty("dy", out JsonElement dy))
                                                        {
                                                            string result = InputTools.MoveMouseRelative(dx.GetInt16(), dy.GetInt16());
                                                            _messages.Add(new ToolChatMessage(toolCall.Id, result));
                                                        }
                                                        break;
                                                    }

                                                case nameof(InputTools.WheelMouse):
                                                    {
                                                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                                        if (argumentsJson.RootElement.TryGetProperty("rotation", out JsonElement rotation) &&
                                                            argumentsJson.RootElement.TryGetProperty("direction", out JsonElement direction) &&
                                                            Enum.TryParse<MouseWheelScrollDirection>(direction.GetString(), out var directionEnum))
                                                        {
                                                            string result = InputTools.WheelMouse(rotation.GetInt16(), directionEnum);
                                                            _messages.Add(new ToolChatMessage(toolCall.Id, result));
                                                        }
                                                        break;
                                                    }

                                                case nameof(InputTools.TakeScreenshot):
                                                    {
                                                        string result = await InputTools.TakeScreenshot();
                                                        _messages.Add(new ToolChatMessage(toolCall.Id, result));
                                                        if (_chatWindowViewModel.ImageSource != null)
                                                        {
                                                            _messages.Add(new UserChatMessage(CreateImagePart(_chatWindowViewModel.ImageSource)));
                                                        }
                                                        break;
                                                    }

                                                default:
                                                    {
                                                        throw new NotImplementedException($"Tool {toolCall.FunctionName} is not implemented.");
                                                    }
                                            }

                                        }

                                        requiresAction = true;
                                        break;
                                    }
                                default:
                                    break;
                            }
                        }
                    }
                } while (requiresAction);
            }
            catch (Exception)
            {
                _chatWindowViewModel.UpdateText("Something went wrong.");
                StopAIResponseStream(_chatWindowViewModel.LastRequestId);
            }
        }
        
        if (Application.Current is App app && _chatWindowViewModel is { IgnoreMouseEvents: true } and { FirstShowActivated: true })
        {
            app.ToggleClickThrough();
            app.OpenChatWindowForInput();
        }
        
        if (_chatWindowViewModel != null) 
            _chatWindowViewModel.ImageSource = null;
    }

    public static async Task Ask(bool enableConversationMemory = false)
    {
        if (!_initialized && !TryInitialize())
        {
            _chatWindowViewModel?.ShowMissingApiKeyHint();
            return;
        }

        var systemPrompt = _settingsViewModel?.SystemPrompt;
        var sysPart = ChatMessageContentPart.CreateTextPart(systemPrompt);
        var systemMessage = new SystemChatMessage(sysPart);
        var userPrompt = string.IsNullOrWhiteSpace(_chatWindowViewModel?.UserMessage) ? _settingsViewModel?.UserPrompt : _chatWindowViewModel.UserMessage;

        if (string.IsNullOrWhiteSpace(userPrompt) && _chatWindowViewModel?.ImageSource == null)
        {
            Console.WriteLine("Both question and image are null. Nothing to send.");
            return;
        }
        
        var userParts = new List<ChatMessageContentPart>();

        if (!string.IsNullOrWhiteSpace(userPrompt))
        {
            userParts.Add(ChatMessageContentPart.CreateTextPart(userPrompt));
        }

        if (_chatWindowViewModel?.ImageSource != null)
        {
            userParts.Add(CreateImagePart(_chatWindowViewModel.ImageSource));
        }
        var userMessage = new UserChatMessage(userParts.ToArray());

        if (!enableConversationMemory)
        {
            ResetConversation();
        }
        await Ask(systemMessage, userMessage);
    }
    
    public static void ResetConversation()
    {
        _messages.Clear();
    }

    private static ChatMessageContentPart CreateImagePart(Bitmap bitmap)
    {
        return ChatMessageContentPart.CreateImagePart(ToBinaryData(bitmap), "image/png");
    }
    
    private static BinaryData ToBinaryData(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        return BinaryData.FromBytes(stream.ToArray());
    }
}