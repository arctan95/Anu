using System;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Anu.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;

namespace Anu.Core.Services;

public class AIChat
{
    private static ChatWindowViewModel? _chatWindowViewModel;
    private static SettingsWindowViewModel? _settingsViewModel;
    private static ChatClient? chatClient;
    private static readonly Dictionary<string, CancellationTokenSource> Cancellations = new();
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

            if (chatClient == null)
            {
                var options = new OpenAIClientOptions
                {
                    Endpoint = new Uri(_settingsViewModel.Endpoint)
                };

                var openAiClient = new OpenAIClient(new ApiKeyCredential(_settingsViewModel.ApiKey), options);
                chatClient = openAiClient.GetChatClient(_settingsViewModel.Model);
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
            var  cancellationTokenSource = Cancellations[requestId];
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested && _chatWindowViewModel != null)
            {
                cancellationTokenSource.Cancel();
                _chatWindowViewModel.MessageRequested = false;
                Cancellations.Remove(requestId);
            }
        }
    }

    private static async Task Ask(string requestId, params ChatMessage[] messages)
    {
        var cts = new CancellationTokenSource();
        var cancelToken = cts.Token;
        cancelToken.ThrowIfCancellationRequested();
        Cancellations.Add(requestId, cts);
        AsyncCollectionResult<StreamingChatCompletionUpdate>? completionUpdates = chatClient?.CompleteChatStreamingAsync(messages, null, cancelToken);

        if (_chatWindowViewModel != null)
        {
            _chatWindowViewModel.LastRequestId = requestId;
            _chatWindowViewModel.MessageRequested = true;
            _chatWindowViewModel.UserMessage = string.Empty;
            _chatWindowViewModel.MdText = "[AI]: ";
            if (completionUpdates != null)
            {
                await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                {
                    if (completionUpdate.ContentUpdate.Count > 0)
                    {
                        string text = completionUpdate.ContentUpdate[0].Text;
                        _chatWindowViewModel.UpdateText(text);
                    }

                    if (completionUpdate.FinishReason == ChatFinishReason.Stop)
                    {
                        _chatWindowViewModel.MessageRequested = false;
                        Cancellations.Remove(requestId);
                    }
                }
            }
        }
    }
    
    public static async Task Ask(Bitmap? bitmap = null, string? question = null)
    {
        if (!_initialized && !TryInitialize())
        {
            _chatWindowViewModel?.ShowMissingApiKeyHint();
            return;
        }
        
        var userPrompt = string.IsNullOrWhiteSpace(question) ? _settingsViewModel?.UserPrompt : question;
        var systemPrompt = _settingsViewModel?.SystemPrompt;

        if (string.IsNullOrWhiteSpace(userPrompt) && bitmap == null)
        {
            Console.WriteLine("Both question and image are null. Nothing to send.");
            return;
        }

        var userParts = new List<ChatMessageContentPart>();

        if (!string.IsNullOrWhiteSpace(userPrompt))
        {
            userParts.Add(ChatMessageContentPart.CreateTextPart(userPrompt));
        }

        if (bitmap != null)
        {
            var imageData = ToBinaryData(bitmap);
            userParts.Add(ChatMessageContentPart.CreateImagePart(imageData, "image/png"));
        }

        var userMessage = new UserChatMessage(userParts.ToArray());
        var sysPart = ChatMessageContentPart.CreateTextPart(systemPrompt);
        var systemMessage = new SystemChatMessage(sysPart);
        var requestId = Guid.NewGuid().ToString();
        await Ask(requestId, systemMessage, userMessage);
    }

    private static BinaryData ToBinaryData(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        return BinaryData.FromBytes(stream.ToArray());
    }
}