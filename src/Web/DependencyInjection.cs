using Azure.Identity;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Infrastructure.Data;
using AskAMuslimBackend.Web.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

using NSwag;
using NSwag.Generation.Processors.Security;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddScoped<IUser, CurrentUser>();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>();

        builder.Services.AddExceptionHandler<CustomExceptionHandler>();

        // Localization – reads from embedded JSON files in Localization/*.json
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

        // Add Hangfire services
        builder.Services.AddHangfire(configuration => { });
        builder.Services.AddHangfireServer();

        // Add CORS
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                      {
                          // Allow web and admin panel
                          if (origin == "https://aam-web.ask-a-muslim.com" ||
                              origin == "https://adminpanel.ask-a-muslim.com" ||
                              origin == "https://islam-port.vercel.app" ||
                              origin == "https://noor-frontend-xi.vercel.app")
                              return true;
                          
                          // Allow localhost for development (Angular, Android emulator, etc.)
                          if (origin.StartsWith("http://localhost") || 
                              origin.StartsWith("http://127.0.0.1"))
                              return true;
                          
                          // Allow Android emulator (10.0.2.2 is the special alias for host machine)
                          if (origin.StartsWith("http://10.0.2.2"))
                              return true;
                          
                          // Allow Android app (no origin or custom schemes)
                          if (string.IsNullOrEmpty(origin) || 
                              origin.StartsWith("android-app://") ||
                              origin.StartsWith("capacitor://") ||
                              origin.StartsWith("ionic://"))
                              return true;
                          
                          return false;
                      })
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        // Customise default API behaviour
        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddOpenApiDocument((configure, sp) =>
        {
            configure.Title = "AskAMuslimBackend API";

            // Add JWT
            configure.AddSecurity("JWT", Enumerable.Empty<string>(), new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.ApiKey,
                Name = "Authorization",
                In = OpenApiSecurityApiKeyLocation.Header,
                Description = "Type into the textbox: Bearer {your JWT token}."
            });

            configure.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("JWT"));
        });
    }

    public static void AddKeyVaultIfConfigured(this IHostApplicationBuilder builder)
    {
        var keyVaultUri = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
        }
    }
}
