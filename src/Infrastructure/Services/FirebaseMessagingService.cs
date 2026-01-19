using AskAMuslimBackend.Application.Notifications.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Models;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Infrastructure.Services;

public class FirebaseMessagingService : IFirebaseMessagingService
{
    private readonly ILogger<FirebaseMessagingService> _logger;
    private const int MaxBatchSize = 500;

    public FirebaseMessagingService(ILogger<FirebaseMessagingService> logger)
    {
        _logger = logger;
    }

    public async Task<PushNotificationResult> SendToTokenAsync(
        string fcmToken, 
        PushNotificationRequest notification, 
        CancellationToken cancellationToken = default)
    {
        return await SendToTokensAsync(new[] { fcmToken }, notification, cancellationToken);
    }

    public async Task<PushNotificationResult> SendToTokensAsync(
        IEnumerable<string> fcmTokens, 
        PushNotificationRequest notification, 
        CancellationToken cancellationToken = default)
    {
        var tokens = fcmTokens.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
        
        if (!tokens.Any())
        {
            return new PushNotificationResult
            {
                SuccessCount = 0,
                FailureCount = 0
            };
        }

        var result = new PushNotificationResult();
        var invalidTokens = new List<string>();
        var errors = new List<string>();
        var successCount = 0;
        var failureCount = 0;

        try
        {
            // Process tokens in batches of MaxBatchSize (500)
            var batches = tokens.Chunk(MaxBatchSize);

            foreach (var batch in batches)
            {
                var messages = batch.Select(token => BuildMessage(token, notification)).ToList();

                try
                {
                    var batchResponse = await FirebaseMessaging.DefaultInstance.SendEachAsync(messages, cancellationToken);

                    // Count successes and failures
                    foreach (var response in batchResponse.Responses)
                    {
                        if (response.IsSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                        }
                    }

                    // Track invalid tokens for cleanup
                    for (int i = 0; i < batchResponse.Responses.Count; i++)
                    {
                        var response = batchResponse.Responses[i];
                        if (!response.IsSuccess)
                        {
                            var error = response.Exception;
                            if (error != null)
                            {
                                var errorCode = GetErrorCode(error);
                                
                                // Invalid registration tokens should be removed
                                if (errorCode is "invalid-registration-token" or "registration-token-not-registered")
                                {
                                    invalidTokens.Add(batch.ElementAt(i));
                                    _logger.LogWarning("Invalid FCM token detected: {Token}", batch.ElementAt(i));
                                }
                                else
                                {
                                    errors.Add($"Token {i}: {error.Message}");
                                    _logger.LogError(error, "Failed to send notification to token {Index}", i);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    failureCount += batch.Count();
                    errors.Add($"Batch error: {ex.Message}");
                    _logger.LogError(ex, "Failed to send notification batch");
                }
            }

            return new PushNotificationResult
            {
                SuccessCount = successCount,
                FailureCount = failureCount,
                InvalidTokens = invalidTokens,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending push notifications");
            return new PushNotificationResult
            {
                SuccessCount = 0,
                FailureCount = tokens.Count,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private static Message BuildMessage(string token, PushNotificationRequest notification)
    {
        var dataPayload = new Dictionary<string, string>();
        
        // Add custom data payload
        if (notification.CustomData != null && notification.CustomData.Any())
        {
            foreach (var kvp in notification.CustomData)
            {
                dataPayload[kvp.Key] = kvp.Value;
            }
        }
        
        // Add action URL to data payload if provided
        if (!string.IsNullOrWhiteSpace(notification.ActionUrl))
        {
            dataPayload["actionUrl"] = notification.ActionUrl;
        }

        var message = new Message
        {
            Token = token,
            Notification = new FirebaseAdmin.Messaging.Notification
            {
                Title = notification.Title,
                Body = notification.Body,
                ImageUrl = notification.ImageUrl
            },
            Data = dataPayload.Any() ? dataPayload : null,
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    Sound = "default",
                    ClickAction = notification.ActionUrl
                }
            },
            Apns = new ApnsConfig
            {
                Aps = new Aps
                {
                    Sound = "default"
                }
            }
        };

        return message;
    }

    private static string? GetErrorCode(Exception exception)
    {
        if (exception is FirebaseMessagingException fcmException)
        {
            return fcmException.MessagingErrorCode?.ToString().ToLowerInvariant()
                .Replace("_", "-");
        }
        return null;
    }
}
