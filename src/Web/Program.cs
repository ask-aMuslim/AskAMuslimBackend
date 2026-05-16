using AskAMuslimBackend.Infrastructure.Data;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Initialize database in all environments.
app.Logger.LogInformation(
    "Starting automatic database initialization for environment {EnvironmentName}.",
    app.Environment.EnvironmentName);

try
{
    await app.InitialiseDatabaseAsync();
}
catch (Exception ex)
{
    app.Logger.LogCritical(
        ex,
        "Automatic database initialization failed during startup in environment {EnvironmentName}. Verify the connection string 'AskAMuslimBackendDb', network access, and database migration permissions.",
        app.Environment.EnvironmentName);
    throw;
}

// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
app.UseHsts();

app.UseHealthChecks("/health");
app.UseHttpsRedirection();
app.UseStaticFiles();

// Serve uploaded files from configured upload path (maps to /uploads)
app.UseUploadStaticFiles();

app.UseCors();

// Request localization – resolves culture from Accept-Language header
var supportedCultures = new[] { "en", "ar", "fr", "es", "de", "pt" };
app.UseRequestLocalization(opts =>
{
    opts.SetDefaultCulture("en")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
    opts.ApplyCurrentCultureToResponseHeaders = true;
});

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

app.UseAuthentication();
app.UseJwtBlacklist(); // Must be after UseAuthentication() and before UseAuthorization()
app.UseAuthorization();

// Add Hangfire Dashboard (admin-only access)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.UseExceptionHandler(options => { });

app.Map("/", () => Results.Redirect("/api"));

app.MapEndpoints();

app.Run();

/// <summary>
/// Defines the <see cref="Program" />
/// </summary>
public partial class Program
{
    /// <summary>
    /// The IsRunningUnderNSwag
    /// </summary>
    /// <returns>The <see cref="bool"/></returns>
    public static bool IsRunningUnderNSwag()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            return args.Any(a => a.Contains("dotnet-nswag", StringComparison.OrdinalIgnoreCase) || a.Contains("nswag", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}
