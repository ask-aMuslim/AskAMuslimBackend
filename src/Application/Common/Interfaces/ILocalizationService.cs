namespace AskAMuslimBackend.Application.Common.Interfaces;

/// <summary>
/// Abstraction over <c>IStringLocalizer</c> that command/query handlers use to
/// retrieve locale-appropriate messages without depending directly on
/// <c>Microsoft.Extensions.Localization</c>.
/// The language is resolved from the HTTP <c>Accept-Language</c> header by
/// <see cref="Microsoft.AspNetCore.Builder.ApplicationBuilderExtensions.UseRequestLocalization"/>.
/// </summary>
public interface ILocalizationService
{
    /// <summary>Returns the localized string for <paramref name="key"/>, falling back to the key itself when not found.</summary>
    string this[string key] { get; }

    /// <summary>Returns the localized string for <paramref name="key"/>, formatted with <paramref name="args"/>.</summary>
    string GetString(string key, params object[] args);
}
