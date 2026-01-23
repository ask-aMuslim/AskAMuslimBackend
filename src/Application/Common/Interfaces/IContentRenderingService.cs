namespace AskAMuslimBackend.Application.Common.Interfaces;

/// <summary>
/// Service for rendering Tiptap JSON content to HTML and extracting plain text
/// </summary>
public interface IContentRenderingService
{
    /// <summary>
    /// Converts Tiptap JSON to sanitized HTML
    /// </summary>
    /// <param name="tiptapJson">The Tiptap JSON string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sanitized HTML string, or null if input is null/empty</returns>
    Task<string?> RenderToHtmlAsync(string? tiptapJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts plain text from Tiptap JSON for search indexing and meta descriptions
    /// </summary>
    /// <param name="tiptapJson">The Tiptap JSON string</param>
    /// <param name="maxLength">Maximum length of extracted text (0 for unlimited)</param>
    /// <returns>Plain text string, or null if input is null/empty</returns>
    string? ExtractPlainText(string? tiptapJson, int maxLength = 0);
}
