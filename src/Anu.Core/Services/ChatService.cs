using System;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Anu.Core.Tools;
using Avalonia.Media.Imaging;
using Anu.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Responses;
using SharpHook.Native;

namespace Anu.Core.Services;

public static class ChatService
{
    private static ChatWindowViewModel? _chatWindowViewModel;
    private static SettingsWindowViewModel? _settingsViewModel;
    private static OpenAIResponseClient? _openAIResponseClient;
    private static readonly Dictionary<string, CancellationTokenSource> Cancellations = new();
    private static readonly List<ResponseItem> Messages = new();
    private static bool _initialized;

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

            if (_openAIResponseClient == null)
            {
                var options = new OpenAIClientOptions
                {
                    Endpoint = new Uri(_settingsViewModel.Endpoint)
                };

                var openAiClient = new OpenAIClient(new ApiKeyCredential(_settingsViewModel.ApiKey), options);
                _openAIResponseClient = openAiClient.GetOpenAIResponseClient(_settingsViewModel.Model);
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
            if (cancellationTokenSource is { IsCancellationRequested: false })
            {
                cancellationTokenSource.Cancel();
                Cancellations.Remove(requestId);
            }
        }
    }

    private static async Task DoAsk()
    {
        ResponseCreationOptions options = new()
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

        try
        {
            bool requiresAction;
            do
            {
                requiresAction = false;

                var requestId = Guid.NewGuid().ToString();
                var cts = new CancellationTokenSource();
                var cancelToken = cts.Token;
                cancelToken.ThrowIfCancellationRequested();
                Cancellations.Add(requestId, cts);

                _chatWindowViewModel?.UpdateLastRequestId(requestId);
                AsyncCollectionResult<StreamingResponseUpdate>? completionUpdates =
                    _openAIResponseClient?.CreateResponseStreamingAsync(Messages, options, cancelToken);

                if (completionUpdates != null)
                {
                    await foreach (StreamingResponseUpdate completionUpdate in completionUpdates)
                    {
                        if (completionUpdate is StreamingResponseOutputItemAddedUpdate outputItemAddedUpdated)
                        {
                            if (outputItemAddedUpdated.Item is MessageResponseItem message &&
                                message.Role == MessageRole.Assistant)
                            {
                                _chatWindowViewModel?.StartAssistantResponse();
                            }
                        }

                        if (completionUpdate is StreamingResponseOutputTextDeltaUpdate outputTextUpdate)
                        {
                            // Accumulate the text content as new updates arrive.
                            _chatWindowViewModel?.AppendAssistantText(outputTextUpdate.Delta);
                        }

                        if (completionUpdate is StreamingResponseOutputItemDoneUpdate outputItemDoneUpdate)
                        {
                            // Add the assistant message to the conversation history.
                            Messages.Add(outputItemDoneUpdate.Item);
                            _chatWindowViewModel?.EndAssistantResponse();

                            if (outputItemDoneUpdate.Item is FunctionCallResponseItem functionCall)
                            {
                                if (_chatWindowViewModel is { ComputerUse: true })
                                {
                                    switch (functionCall.FunctionName)
                                    {
                                        case nameof(InputTools.GetScreenSize):
                                        {
                                            string size = InputTools.GetScreenSize();
                                            Messages.Add(new FunctionCallOutputResponseItem(functionCall.Id, size));
                                            break;
                                        }

                                        case nameof(InputTools.GetCurrentCursorPosition):
                                        {
                                            string position = InputTools.GetCurrentCursorPosition();
                                            Messages.Add(new FunctionCallOutputResponseItem(functionCall.Id, position));
                                            break;
                                        }

                                        case nameof(InputTools.GetAllKeyNames):
                                        {
                                            string allKeys = InputTools.GetAllKeyNames();
                                            Messages.Add(new FunctionCallOutputResponseItem(functionCall.Id, allKeys));
                                            break;
                                        }

                                        case nameof(InputTools.InputText):
                                        {
                                            using JsonDocument argumentsJson =
                                                JsonDocument.Parse(functionCall.FunctionArguments);
                                            if (argumentsJson.RootElement.TryGetProperty("text",
                                                    out JsonElement text))
                                            {
                                                string result = InputTools.InputText(text.GetString());
                                                Messages.Add(
                                                    new FunctionCallOutputResponseItem(functionCall.Id, result));
                                            }

                                            break;
                                        }

                                        case nameof(InputTools.PressKey):
                                        {
                                            using JsonDocument argumentsJson =
                                                JsonDocument.Parse(functionCall.FunctionArguments);
                                            if (argumentsJson.RootElement.TryGetProperty("key",
                                                    out JsonElement key) &&
                                                Enum.TryParse<KeyCode>(key.GetString(), out var keyEnum))
                                            {
                                                string result = InputTools.PressKey(keyEnum);
                                                Messages.Add(
                                                    new FunctionCallOutputResponseItem(functionCall.Id, result));
                                            }

                                            break;
                                        }

                                        case nameof(InputTools.LongPressKey):
                                        {
                                            using JsonDocument argumentsJson =
                                                JsonDocument.Parse(functionCall.FunctionArguments);
                                            if (argumentsJson.RootElement.TryGetProperty("key",
                                                    out JsonElement key) &&
                                                argumentsJson.RootElement.TryGetProperty("duration_ms",
                                                    out JsonElement durationMs) &&
                                                Enum.TryParse<KeyCode>(key.GetString(), out var keyEnum))
                                            {
                                                string result = InputTools.LongPressKey(keyEnum,
                                                    durationMs.GetInt32());
                                                Messages.Add(
                                                    new FunctionCallOutputResponseItem(functionCall.Id, result));
                                            }

                                            break;
                                        }

                                        case nameof(InputTools.PressKeyCombination):
                                        {
                                            using JsonDocument argumentsJson =
                                                JsonDocument.Parse(functionCall.FunctionArguments);

                                            if (argumentsJson.RootElement.TryGetProperty("modifier",
                                                    out JsonElement modifierElement) &&
                                                argumentsJson.RootElement.TryGetProperty("key",
                                                    out JsonElement keyElement) &&
                                                Enum.TryParse<KeyCode>(modifierElement.GetString(),
                                                    out var modifierEnum) &&
                                                Enum.TryParse<KeyCode>(keyElement.GetString(), out var keyEnum))
                                            {
                                                string result =
                                                    InputTools.PressKeyCombination(modifierEnum, keyEnum);
                                                Messages.Add(
                                                    new FunctionCallOutputResponseItem(functionCall.Id, result));
                                            }
                                            else
                                            {
                                                Messages.Add(new FunctionCallOutputResponseItem(functionCall.Id,
                                                    "Invalid key combination"));
                                            }

                                            break;
                                        }

                                        case nameof(InputTools.LeftClick):
                                        {
                                            string result = InputTools.LeftClick();
                                            Messages.Add(new FunctionCallOutputResponseItem(functionCall.Id, result));
                                            break;
                                        }

                                        case nameof(InputTools.RightClick):
                                        {
                                            string result = InputTools.RightClick();
                                            Messages.Add(new FunctionCallOutputResponseItem(functionCall.Id, result));
                                            break;
                                        }

                                        case nameof(InputTools.MiddleClick):
                                        {
                                            string result = InputTools.MiddleClick();
                                            Messages.Add(new FunctionCallOutputResponseItem(functionCall.Id, result));
                                            break;
                                        }

                                        case nameof(InputTools.DoubleClick):
                                        {
                                            string result = InputTools.DoubleClick();
                                            Messages.Add(new FunctionCallOutputResponseItem(functionCall.Id, result));
                                            break;
                                        }

                                        case nameof(InputTools.TripleClick):
                                        {
                                            string result = InputTools.TripleClick();
                                            Messages.Add(new FunctionCallOutputResponseItem(functionCall.Id, result));
                                            break;
                                        }

                                        case nameof(InputTools.ClickAt):
                                        {
                                            using JsonDocument argumentsJson =
                                                JsonDocument.Parse(functionCall.FunctionArguments);
                                            if (argumentsJson.RootElement.TryGetProperty("x",
                                                    out JsonElement x) &&
                                                argumentsJson.RootElement.TryGetProperty("y",
                                                    out JsonElement y))
                                            {
                                                string result = InputTools.ClickAt(x.GetInt16(), y.GetInt16());
                                                Messages.Add(
                                                    new FunctionCallOutputResponseItem(functionCall.Id, result));
                                            }

                                            break;
                                        }

                                        case nameof(InputTools.ClickAndDrag):
                                        {
                                            using JsonDocument argumentsJson =
                                                JsonDocument.Parse(functionCall.FunctionArguments);
                                            if (argumentsJson.RootElement.TryGetProperty("startX",
                                                    out JsonElement startX) &&
                                                argumentsJson.RootElement.TryGetProperty("startY",
                                                    out JsonElement startY) &&
                                                argumentsJson.RootElement.TryGetProperty("endX",
                                                    out JsonElement endX) &&
                                                argumentsJson.RootElement.TryGetProperty("endY",
                                                    out JsonElement endY))
                                            {
                                                string result = InputTools.ClickAndDrag(
                                                    startX.GetInt16(), startY.GetInt16(),
                                                    endX.GetInt16(), endY.GetInt16()
                                                );
                                                Messages.Add(
                                                    new FunctionCallOutputResponseItem(functionCall.Id, result));
                                            }

                                            break;
                                        }

                                        case nameof(InputTools.MoveMouse):
                                        {
                                            using JsonDocument argumentsJson =
                                                JsonDocument.Parse(functionCall.FunctionArguments);
                                            if (argumentsJson.RootElement.TryGetProperty("x",
                                                    out JsonElement x) &&
                                                argumentsJson.RootElement.TryGetProperty("y",
                                                    out JsonElement y))
                                            {
                                                string result =
                                                    InputTools.MoveMouse(x.GetInt16(), y.GetInt16());
                                                Messages.Add(
                                                    new FunctionCallOutputResponseItem(functionCall.Id, result));
                                            }

                                            break;
                                        }

                                        case nameof(InputTools.MoveMouseRelative):
                                        {
                                            using JsonDocument argumentsJson =
                                                JsonDocument.Parse(functionCall.FunctionArguments);
                                            if (argumentsJson.RootElement.TryGetProperty("dx",
                                                    out JsonElement dx) &&
                                                argumentsJson.RootElement.TryGetProperty("dy",
                                                    out JsonElement dy))
                                            {
                                                string result =
                                                    InputTools.MoveMouseRelative(dx.GetInt16(), dy.GetInt16());
                                                Messages.Add(
                                                    new FunctionCallOutputResponseItem(functionCall.Id, result));
                                            }

                                            break;
                                        }

                                        case nameof(InputTools.WheelMouse):
                                        {
                                            using JsonDocument argumentsJson =
                                                JsonDocument.Parse(functionCall.FunctionArguments);
                                            if (argumentsJson.RootElement.TryGetProperty("rotation",
                                                    out JsonElement rotation) &&
                                                argumentsJson.RootElement.TryGetProperty("direction",
                                                    out JsonElement direction) &&
                                                Enum.TryParse<MouseWheelScrollDirection>(direction.GetString(),
                                                    out var directionEnum))
                                            {
                                                string result = InputTools.WheelMouse(rotation.GetInt16(),
                                                    directionEnum);
                                                Messages.Add(
                                                    new FunctionCallOutputResponseItem(functionCall.Id, result));
                                            }

                                            break;
                                        }

                                        case nameof(InputTools.TakeScreenshot):
                                        {
                                            string result = await InputTools.TakeScreenshot();
                                            Messages.Add(new FunctionCallOutputResponseItem(functionCall.Id, result));
                                            if (_chatWindowViewModel.ImageSource != null)
                                            {
                                                _chatWindowViewModel.AddUserMessage(
                                                    _chatWindowViewModel.ImageSource);
                                                Messages.Add(ResponseItem.CreateUserMessageItem(
                                                    [CreateImagePart(ToBinaryData(_chatWindowViewModel.ImageSource))]));
                                                _chatWindowViewModel.ImageSource = null;
                                            }

                                            break;
                                        }

                                        default:
                                        {
                                            throw new NotImplementedException(
                                                $"Tool {functionCall.FunctionName} is not implemented.");
                                        }
                                    }
                                }

                                requiresAction = true;
                                break;
                            }

                            Cancellations.Remove(requestId);
                        }
                    }
                }
            } while (requiresAction);
        }
        catch (Exception)
        {
            _chatWindowViewModel?.AddErrorMessage("Something went wrong!");
        }
        finally
        {
            _chatWindowViewModel?.EndConversation();
        }
    }

    public static async Task Ask(bool enableConversationMemory = false)
    {
        if (!_initialized && !TryInitialize())
        {
            _chatWindowViewModel?.ShowMissingApiKeyHint();
            _chatWindowViewModel?.EndConversation();
            return;
        }

        var systemPrompt = _settingsViewModel?.SystemPrompt;
        var sysPart = ResponseContentPart.CreateInputTextPart(systemPrompt);
        var systemMessage = ResponseItem.CreateSystemMessageItem([sysPart]);
        var userPrompt = string.IsNullOrWhiteSpace(_chatWindowViewModel?.UserPrompt)
            ? _settingsViewModel?.UserPrompt
            : _chatWindowViewModel.UserPrompt;

        if (string.IsNullOrWhiteSpace(userPrompt) && _chatWindowViewModel?.ImageSource == null)
        {
            Console.WriteLine("Both question and image are null. Nothing to send.");
            _chatWindowViewModel?.EndConversation();
            return;
        }

        _chatWindowViewModel?.AddUserMessage(_chatWindowViewModel.ImageSource);
        _chatWindowViewModel?.AddUserMessage(userPrompt!);

        var userParts = new List<ResponseContentPart>();

        if (!string.IsNullOrWhiteSpace(userPrompt))
        {
            userParts.Add(ResponseContentPart.CreateInputTextPart(userPrompt));
        }

        if (_chatWindowViewModel?.ImageSource != null)
        {
            userParts.Add(CreateImagePart(ToBinaryData(_chatWindowViewModel.ImageSource)));
        }

        var userMessage = ResponseItem.CreateUserMessageItem(userParts);

        if (!enableConversationMemory)
        {
            ResetConversationContext();
        }

        _chatWindowViewModel?.ClearInput();
        _chatWindowViewModel?.StartConversation();

        // Only add system message in first time
        if (Messages.Count == 0)
        {
            Messages.Add(systemMessage);
        }

        Messages.Add(userMessage);
        await DoAsk();
    }

    public static void ResetConversationContext()
    {
        Messages.Clear();
    }

    private static ResponseContentPart CreateImagePart(BinaryData imageBytes)
    {
        return ResponseContentPart.CreateInputImagePart(imageBytes, "image/png");
    }

    public static Bitmap BytesToBitmap(byte[] imageBytes)
    {
        using var memoryStream = new MemoryStream(imageBytes);
        Bitmap bitmap = new Bitmap(memoryStream);
        return bitmap;
    }

    private static BinaryData ToBinaryData(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        return BinaryData.FromBytes(stream.ToArray());
    }
}