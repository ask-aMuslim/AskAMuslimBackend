using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Web.Infrastructure;

/// <summary>
/// Resolves a <see cref="Language"/> from the HTTP <c>Accept-Language</c> header.
/// Falls back to <see cref="Language.English"/> when the header is absent or unrecognised.
/// </summary>
public static class LanguageResolver
{
    public static Language Resolve(IHeaderDictionary headers)
    {
        var headerValue = headers["Accept-Language"].FirstOrDefault();
        // Accept-Language can be e.g. "ar,en-US;q=0.9,en;q=0.8"
        // Take the first segment (highest-priority language), strip quality factor
        var primary = headerValue?.Split(',')[0].Split(';')[0].Trim().ToLowerInvariant();
        return primary switch
        {
            "ar" => Language.Arabic,
            "es" => Language.Spanish,
            "pt" => Language.Portuguese,
            "fr" => Language.French,
            "de" => Language.German,
            _ => Language.English
        };
    }
}
