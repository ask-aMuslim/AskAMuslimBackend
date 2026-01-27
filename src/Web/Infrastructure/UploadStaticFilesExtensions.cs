using Microsoft.Extensions.FileProviders;
using System.IO;

namespace AskAMuslimBackend.Web.Infrastructure;

public static class UploadStaticFilesExtensions
{
    /// <summary>
    /// Mounts the configured upload directory (or fallback temp uploads) to the "/uploads" request path.
    /// This keeps Program.cs focused and centralizes the static-file mapping logic.
    /// </summary>
    public static void UseUploadStaticFiles(this WebApplication app)
    {
        try
        {
            var uploadPathConfig = app.Configuration["FileStorage:UploadPath"];
            string uploadPhysicalPath;

            if (string.IsNullOrWhiteSpace(uploadPathConfig))
            {
                uploadPhysicalPath = Path.Combine(Path.GetTempPath(), "Uploads");
            }
            else if (Path.IsPathRooted(uploadPathConfig))
            {
                uploadPhysicalPath = uploadPathConfig;
            }
            else
            {
                uploadPhysicalPath = Path.Combine(app.Environment.ContentRootPath, uploadPathConfig);
            }

            if (!Directory.Exists(uploadPhysicalPath))
            {
                Directory.CreateDirectory(uploadPhysicalPath);
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uploadPhysicalPath),
                RequestPath = "/uploads"
            });
        }
        catch
        {
            // Ignore errors - failing to serve uploads should not crash the app
        }
    }
}
