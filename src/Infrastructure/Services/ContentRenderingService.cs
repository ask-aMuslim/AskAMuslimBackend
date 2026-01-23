using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AskAMuslimBackend.Application.Common.Interfaces;

namespace AskAMuslimBackend.Infrastructure.Services;

/// <summary>
/// Basic implementation of content rendering service
/// Converts Tiptap JSON to HTML manually
/// TODO: Replace with Node.js microservice for production-grade rendering
/// </summary>
public class ContentRenderingService : IContentRenderingService
{
    public Task<string?> RenderToHtmlAsync(string? tiptapJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tiptapJson))
            return Task.FromResult<string?>(null);

        try
        {
            var json = JsonNode.Parse(tiptapJson);
            if (json == null)
                return Task.FromResult<string?>(null);

            var html = RenderNode(json);
            
            // TODO: Add HTML sanitization here using HtmlSanitizer library
            // var sanitizer = new HtmlSanitizer();
            // return Task.FromResult<string?>(sanitizer.Sanitize(html));
            
            return Task.FromResult<string?>(html);
        }
        catch (JsonException)
        {
            return Task.FromResult<string?>(null);
        }
    }

    public string? ExtractPlainText(string? tiptapJson, int maxLength = 0)
    {
        if (string.IsNullOrWhiteSpace(tiptapJson))
            return null;

        try
        {
            var json = JsonNode.Parse(tiptapJson);
            if (json == null)
                return null;

            var text = ExtractTextFromNode(json);
            
            if (maxLength > 0 && text.Length > maxLength)
            {
                return text.Substring(0, maxLength).TrimEnd() + "...";
            }

            return text;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string RenderNode(JsonNode node)
    {
        if (node is not JsonObject obj)
            return string.Empty;

        var type = obj["type"]?.GetValue<string>();
        var content = obj["content"] as JsonArray;

        return type switch
        {
            "doc" => RenderChildren(content),
            "paragraph" => $"<p>{RenderChildren(content)}</p>",
            "heading" => RenderHeading(obj, content),
            "bulletList" => $"<ul>{RenderChildren(content)}</ul>",
            "orderedList" => $"<ol>{RenderChildren(content)}</ol>",
            "listItem" => $"<li>{RenderChildren(content)}</li>",
            "blockquote" => $"<blockquote>{RenderChildren(content)}</blockquote>",
            "codeBlock" => $"<pre><code>{RenderChildren(content)}</code></pre>",
            "hardBreak" => "<br>",
            "horizontalRule" => "<hr>",
            "image" => RenderImage(obj),
            "text" => RenderText(obj),
            _ => RenderChildren(content) // Unknown nodes - just render children
        };
    }

    private string RenderHeading(JsonObject node, JsonArray? content)
    {
        var attrs = node["attrs"] as JsonObject;
        var level = attrs?["level"]?.GetValue<int>() ?? 1;
        level = Math.Clamp(level, 1, 6); // Ensure valid heading level

        return $"<h{level}>{RenderChildren(content)}</h{level}>";
    }

    private string RenderImage(JsonObject node)
    {
        var attrs = node["attrs"] as JsonObject;
        var src = attrs?["src"]?.GetValue<string>() ?? "";
        var alt = attrs?["alt"]?.GetValue<string>() ?? "";
        var title = attrs?["title"]?.GetValue<string>();

        // Escape attributes to prevent XSS
        src = System.Net.WebUtility.HtmlEncode(src);
        alt = System.Net.WebUtility.HtmlEncode(alt);

        if (!string.IsNullOrEmpty(title))
        {
            title = System.Net.WebUtility.HtmlEncode(title);
            return $"<img src=\"{src}\" alt=\"{alt}\" title=\"{title}\" />";
        }

        return $"<img src=\"{src}\" alt=\"{alt}\" />";
    }

    private string RenderText(JsonObject node)
    {
        var text = node["text"]?.GetValue<string>() ?? "";
        var marks = node["marks"] as JsonArray;

        // Escape HTML to prevent XSS
        text = System.Net.WebUtility.HtmlEncode(text);

        if (marks == null || marks.Count == 0)
            return text;

        // Apply marks (formatting)
        foreach (var mark in marks)
        {
            if (mark is not JsonObject markObj)
                continue;

            var markType = markObj["type"]?.GetValue<string>();
            text = markType switch
            {
                "bold" => $"<strong>{text}</strong>",
                "italic" => $"<em>{text}</em>",
                "underline" => $"<u>{text}</u>",
                "strike" => $"<s>{text}</s>",
                "code" => $"<code>{text}</code>",
                "link" => RenderLink(text, markObj),
                _ => text
            };
        }

        return text;
    }

    private string RenderLink(string text, JsonObject markNode)
    {
        var attrs = markNode["attrs"] as JsonObject;
        var href = attrs?["href"]?.GetValue<string>() ?? "";
        var target = attrs?["target"]?.GetValue<string>();

        // Sanitize href to prevent javascript: and data: URLs
        if (href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            href.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase))
        {
            href = "#"; // Replace malicious URLs with safe placeholder
        }

        href = System.Net.WebUtility.HtmlEncode(href);

        if (!string.IsNullOrEmpty(target))
        {
            target = System.Net.WebUtility.HtmlEncode(target);
            return $"<a href=\"{href}\" target=\"{target}\" rel=\"noopener noreferrer\">{text}</a>";
        }

        return $"<a href=\"{href}\">{text}</a>";
    }

    private string RenderChildren(JsonArray? content)
    {
        if (content == null || content.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var child in content)
        {
            if (child != null)
            {
                sb.Append(RenderNode(child));
            }
        }

        return sb.ToString();
    }

    private string ExtractTextFromNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            // If this is a text node, return its text
            var type = obj["type"]?.GetValue<string>();
            if (type == "text")
            {
                return obj["text"]?.GetValue<string>() ?? "";
            }

            // Otherwise, recursively extract from content
            var content = obj["content"] as JsonArray;
            if (content != null)
            {
                var sb = new StringBuilder();
                foreach (var child in content)
                {
                    if (child != null)
                    {
                        var childText = ExtractTextFromNode(child);
                        if (!string.IsNullOrEmpty(childText))
                        {
                            sb.Append(childText);
                            
                            // Add space after block elements
                            if (type is "paragraph" or "heading" or "listItem")
                            {
                                sb.Append(' ');
                            }
                        }
                    }
                }
                return sb.ToString();
            }
        }
        else if (node is JsonArray array)
        {
            var sb = new StringBuilder();
            foreach (var child in array)
            {
                if (child != null)
                {
                    sb.Append(ExtractTextFromNode(child));
                }
            }
            return sb.ToString();
        }

        return string.Empty;
    }
}
