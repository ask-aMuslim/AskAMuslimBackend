using AskAMuslimBackend.Application.Common.Interfaces;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace AskAMuslimBackend.Web.Services;

/// <summary>
/// Implements <see cref="ILocalizationService"/> by loading translations from
/// embedded JSON files (<c>Localization/{culture}.json</c>). The active culture is
/// resolved from the <c>Accept-Language</c> HTTP header by
/// <c>RequestLocalizationMiddleware</c> and stored in
/// <see cref="CultureInfo.CurrentUICulture"/>.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> _cache =
        new(LoadAllTranslations, LazyThreadSafetyMode.ExecutionAndPublication);

    public string this[string key] => GetString(key);

    public string GetString(string key, params object[] args)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var translations = _cache.Value;

        if (!translations.TryGetValue(culture, out var dict) || !dict.TryGetValue(key, out var value))
        {
            // Fall back to English
            if (!translations.TryGetValue("en", out dict) || !dict.TryGetValue(key, out value))
                return key; // Return the key itself as last resort
        }

        return args.Length > 0 ? string.Format(value, args) : value;
    }

    private static Dictionary<string, Dictionary<string, string>> LoadAllTranslations()
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            // Only process resources under the Localization namespace, e.g.
            // "AskAMuslimBackend.Web.Localization.en.json"
            if (!resourceName.Contains(".Localization.") ||
                !resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = resourceName.Split('.');
            // Need at least: <Namespace>.Localization.<lang>.json → 5 segments
            if (parts.Length < 5)
                continue;

            // The language/culture code is the second-to-last segment (before ".json")
            var langCode = parts[^2];

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
                continue;

            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
            if (dict is not null)
                result[langCode] = dict;
        }

        return result;
    }
}

