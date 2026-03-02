using AskAMuslimBackend.Application.Common.Interfaces;
using Microsoft.Extensions.Localization;

namespace AskAMuslimBackend.Web.Services;

/// <summary>
/// Implements <see cref="ILocalizationService"/> by wrapping
/// <see cref="IStringLocalizer{SharedResource}"/>. The current culture is resolved
/// automatically from the HTTP <c>Accept-Language</c> header via
/// <c>RequestLocalizationMiddleware</c>.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LocalizationService(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public string this[string key] => _localizer[key].Value;

    public string GetString(string key, params object[] args)
        => _localizer[key, args].Value;
}
