namespace AskAMuslimBackend.Application.Common.Interfaces;

/// <summary>
/// Abstraction for retrieving locale-appropriate messages in command/query handlers.
/// The active culture is resolved from the HTTP <c>Accept-Language</c> header by
/// <c>RequestLocalizationMiddleware</c> and stored in
/// <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>.
/// Translations are loaded from embedded JSON files (<c>Localization/{culture}.json</c>)
/// and fall back to English when a key or culture is not found.
/// </summary>
public interface ILocalizationService
{
    /// <summary>Returns the localized string for <paramref name="key"/>, falling back to the key itself when not found.</summary>
    string this[string key] { get; }

    /// <summary>Returns the localized string for <paramref name="key"/>, formatted with <paramref name="args"/>.</summary>
    string GetString(string key, params object[] args);
}
