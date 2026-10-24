using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Infrastructure.Services;

/// <summary>
/// Creates the default <see cref="FirebaseApp"/> from configuration.
/// Firebase:CredentialJson (raw or base64-encoded service-account JSON, e.g. from the
/// Firebase__CredentialJson environment variable) wins over Firebase:CredentialPath (a file path).
/// </summary>
public static class FirebaseAppInitializer
{
    public static FirebaseInitializationResult Initialize(IConfiguration configuration, params string[] searchDirectories)
    {
        if (FirebaseApp.DefaultInstance != null)
        {
            return new FirebaseInitializationResult(true, "an existing FirebaseApp instance", null);
        }

        try
        {
            var credentialJson = configuration["Firebase:CredentialJson"];
            if (!string.IsNullOrWhiteSpace(credentialJson))
            {
                Create(GoogleCredential.FromJson(DecodeIfBase64(credentialJson.Trim())));
                return new FirebaseInitializationResult(true, "Firebase:CredentialJson", null);
            }

            var credentialPath = configuration["Firebase:CredentialPath"];
            if (!string.IsNullOrWhiteSpace(credentialPath))
            {
                var fullPath = searchDirectories
                    .Select(dir => Path.Combine(dir, credentialPath))
                    .Prepend(credentialPath)
                    .Where(Path.IsPathRooted)
                    .FirstOrDefault(File.Exists);

                if (fullPath != null)
                {
                    Create(GoogleCredential.FromFile(fullPath));
                    return new FirebaseInitializationResult(true, $"Firebase:CredentialPath ({fullPath})", null);
                }

                return new FirebaseInitializationResult(false, null,
                    $"Firebase:CredentialJson is not set and the Firebase:CredentialPath file '{credentialPath}' was not found in: {string.Join(", ", searchDirectories)}.");
            }

            return new FirebaseInitializationResult(false, null, "Neither Firebase:CredentialJson nor Firebase:CredentialPath is configured.");
        }
        catch (Exception ex)
        {
            return new FirebaseInitializationResult(false, null, $"The Firebase credential could not be loaded: {ex.Message}");
        }
    }

    private static void Create(GoogleCredential credential)
    {
        FirebaseApp.Create(new AppOptions { Credential = credential });
    }

    private static string DecodeIfBase64(string value)
    {
        if (value.StartsWith('{'))
        {
            return value;
        }

        var buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out var written)
            ? Encoding.UTF8.GetString(buffer, 0, written)
            : value;
    }
}

public record FirebaseInitializationResult(bool IsInitialized, string? Source, string? Problem);

/// <summary>
/// Logs the Firebase initialization outcome once the host starts, so a missing credential is visible.
/// </summary>
public class FirebaseStartupLogger : IHostedService
{
    private readonly FirebaseInitializationResult _result;
    private readonly ILogger<FirebaseStartupLogger> _logger;

    public FirebaseStartupLogger(FirebaseInitializationResult result, ILogger<FirebaseStartupLogger> logger)
    {
        _result = result;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_result.IsInitialized)
        {
            _logger.LogInformation("Firebase initialized from {Source}.", _result.Source);
        }
        else
        {
            _logger.LogWarning(
                "Firebase is not initialized, so push notifications will not be sent. {Problem} Set Firebase:CredentialJson (env var Firebase__CredentialJson) or Firebase:CredentialPath.",
                _result.Problem);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
