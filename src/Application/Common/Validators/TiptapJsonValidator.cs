using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AskAMuslimBackend.Application.Common.Validators;

public static class TiptapJsonValidator
{
    private const int MaxDepth = 50;
    private const int MaxSizeBytes = 2_097_152; // 2MB
    private static readonly string[] AllowedDomains = new[]
    {
        "yourstorage.blob.core.windows.net",
        "cdn.yourdomain.com"
    };

    public static bool IsValidTiptapJson(string? jsonString, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return true; // Null/empty is valid (optional field)
        }

        // 1. Check size limit
        if (Encoding.UTF8.GetByteCount(jsonString) > MaxSizeBytes)
        {
            errorMessage = $"Content JSON size exceeds {MaxSizeBytes / 1024 / 1024}MB limit.";
            return false;
        }

        // 2. Parse as valid JSON
        JsonNode? json;
        try
        {
            json = JsonNode.Parse(jsonString);
        }
        catch (JsonException)
        {
            errorMessage = "Content must be valid JSON.";
            return false;
        }

        if (json == null)
        {
            return true; // null JSON is valid
        }

        // 3. Check root structure (must be a Tiptap document)
        if (json is not JsonObject doc)
        {
            errorMessage = "Content JSON must be an object.";
            return false;
        }

        var type = doc["type"]?.GetValue<string>();
        if (type != "doc")
        {
            errorMessage = "Content JSON root must have type 'doc'.";
            return false;
        }

        // 4. Validate max depth (prevent deeply nested attacks)
        if (GetMaxDepth(json) > MaxDepth)
        {
            errorMessage = $"Content JSON nesting depth exceeds {MaxDepth} levels.";
            return false;
        }

        // 5. Check for malicious patterns
        if (jsonString.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
            jsonString.Contains("data:text/html", StringComparison.OrdinalIgnoreCase) ||
            jsonString.Contains("vbscript:", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Content JSON contains potentially malicious patterns.";
            return false;
        }

        // 6. Validate image URLs (if present)
        if (!ValidateImageUrls(json, out errorMessage))
        {
            return false;
        }

        return true;
    }

    private static int GetMaxDepth(JsonNode? node, int currentDepth = 0)
    {
        if (node == null || currentDepth > MaxDepth)
        {
            return currentDepth;
        }

        int maxChildDepth = currentDepth;

        if (node is JsonObject obj)
        {
            foreach (var property in obj)
            {
                int childDepth = GetMaxDepth(property.Value, currentDepth + 1);
                maxChildDepth = Math.Max(maxChildDepth, childDepth);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                int childDepth = GetMaxDepth(item, currentDepth + 1);
                maxChildDepth = Math.Max(maxChildDepth, childDepth);
            }
        }

        return maxChildDepth;
    }

    private static bool ValidateImageUrls(JsonNode json, out string? errorMessage)
    {
        errorMessage = null;
        var imageNodes = FindAllNodesByType(json, "image");

        foreach (var imageNode in imageNodes)
        {
            if (imageNode is not JsonObject imgObj)
                continue;

            var src = imgObj["attrs"]?["src"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(src))
                continue;

            // Must be HTTPS
            if (!src.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Image URLs must use HTTPS protocol.";
                return false;
            }

            // Validate against allowed domains
            if (!Uri.TryCreate(src, UriKind.Absolute, out var uri))
            {
                errorMessage = $"Invalid image URL: {src}";
                return false;
            }

            // TODO: Update AllowedDomains array with your actual storage domains
            // For now, we'll just ensure it's HTTPS and a valid URL
            // Uncomment below to enforce domain whitelist:
            /*
            if (!AllowedDomains.Any(domain => uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = $"Image URL domain '{uri.Host}' is not allowed.";
                return false;
            }
            */
        }

        return true;
    }

    private static List<JsonNode> FindAllNodesByType(JsonNode? node, string nodeType)
    {
        var results = new List<JsonNode>();
        if (node == null)
            return results;

        if (node is JsonObject obj)
        {
            // Check if this node has the type we're looking for
            if (obj["type"]?.GetValue<string>() == nodeType)
            {
                results.Add(obj);
            }

            // Recursively search all properties
            foreach (var property in obj)
            {
                results.AddRange(FindAllNodesByType(property.Value, nodeType));
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                results.AddRange(FindAllNodesByType(item, nodeType));
            }
        }

        return results;
    }
}
