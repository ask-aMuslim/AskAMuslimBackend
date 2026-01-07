using AskAMuslimBackend.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Initialize database in all environments (except during NSwag generation)

await app.InitialiseDatabaseAsync();

// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
app.UseHsts();

app.UseHealthChecks("/health");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors();

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

app.UseAuthentication();
app.UseAuthorization();

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
