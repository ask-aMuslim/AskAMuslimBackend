# Tiptap Rich Text Content - Implementation Guide

## Overview

This implementation adds support for storing and managing rich text content using Tiptap/ProseMirror JSON format for Lessons and QA entities.

## Architecture

### JSON Storage Strategy

- **Opaque Blob Storage**: JSON stored as `nvarchar(max)` without C# model mapping
- **Backward Compatible**: Existing plain text fields (`Transcript`, `QuestionText`, `AnswerText`) preserved
- **Validation**: Application-layer validation via `TiptapJsonValidator`
- **Security**: XSS prevention through HTML escaping and URL sanitization

### Database Schema

**Lessons Table:**

- `ContentJson` (nvarchar(max), nullable) - Rich text content
- `TranscriptJson` (nvarchar(max), nullable) - Rich text transcript

**QATranslations Table:**

- `QuestionTextJson` (nvarchar(max), nullable) - Rich text question
- `AnswerTextJson` (nvarchar(max), nullable) - Rich text answer

## API Usage

### 1. Upload Images for Rich Content

**Endpoint:** `POST /api/media/upload`

**Authentication:** Required (Bearer token)

**Request:**

```http
POST /api/media/upload?containerName=lesson-images
Content-Type: multipart/form-data

file: [binary image data]
```

**Response:**

```json
{
  "url": "/uploads/lesson-images/abc123-def456.jpg"
}
```

**Validation:**

- File types: `image/jpeg`, `image/png`, `image/webp`, `image/gif`
- Max size: 5MB
- Container names: `lesson-images`, `qa-images`

### 2. Create/Update Lesson with Rich Content

**Endpoint:** `POST /api/lessons`

**Request:**

```json
{
  "courseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Introduction to Prayer",
  "contentJson": {
    "type": "doc",
    "content": [
      {
        "type": "paragraph",
        "content": [
          { "type": "text", "text": "Welcome to this lesson on " },
          {
            "type": "text",
            "marks": [{ "type": "bold" }],
            "text": "prayer"
          }
        ]
      },
      {
        "type": "image",
        "attrs": {
          "src": "https://yourdomain.com/uploads/lesson-images/abc123.jpg",
          "alt": "Prayer diagram"
        }
      }
    ]
  },
  "transcriptJson": {
    "type": "doc",
    "content": [
      {
        "type": "paragraph",
        "content": [{ "type": "text", "text": "Full transcript text here..." }]
      }
    ]
  },
  "type": "Video",
  "order": 1
}
```

### 3. Create/Update QA with Rich Content

**Endpoint:** `POST /api/qas`

**Request:**

```json
{
  "imageUrl": "https://example.com/qa-image.jpg",
  "isPublished": true,
  "translations": [
    {
      "language": "en",
      "questionText": "What is Islam?",
      "questionTextJson": {
        "type": "doc",
        "content": [
          {
            "type": "heading",
            "attrs": { "level": 2 },
            "content": [{ "type": "text", "text": "What is Islam?" }]
          },
          {
            "type": "paragraph",
            "content": [
              { "type": "text", "text": "This is a detailed question with " },
              {
                "type": "text",
                "marks": [{ "type": "italic" }],
                "text": "formatting"
              }
            ]
          }
        ]
      },
      "answerText": "Islam is...",
      "answerTextJson": {
        "type": "doc",
        "content": [
          {
            "type": "paragraph",
            "content": [
              { "type": "text", "text": "Islam is a complete way of life..." }
            ]
          }
        ]
      }
    }
  ],
  "tagIds": ["tag-guid-1", "tag-guid-2"]
}
```

## Frontend Integration

### Tiptap Editor Setup (React/Vue)

```typescript
import { useEditor, EditorContent } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Image from "@tiptap/extension-image";

const editor = useEditor({
  extensions: [
    StarterKit,
    Image.configure({
      inline: true,
      allowBase64: false,
    }),
  ],
  content: lessonData.contentJson, // Load from API
  onUpdate: ({ editor }) => {
    const json = editor.getJSON();
    // Save json to form state
    setFormData({ ...formData, contentJson: json });
  },
});

// Image upload handler
const handleImageUpload = async (file: File) => {
  const formData = new FormData();
  formData.append("file", file);

  const response = await fetch(
    "/api/media/upload?containerName=lesson-images",
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
      },
      body: formData,
    },
  );

  const { url } = await response.json();

  // Insert image into editor
  editor.chain().focus().setImage({ src: url }).run();
};
```

### Rendering for Public Pages

**Option 1: Client-side rendering (SPA)**

```typescript
import { useEditor, EditorContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'

const ReadOnlyContent = ({ content }) => {
  const editor = useEditor({
    extensions: [StarterKit],
    content: content.contentJson,
    editable: false,
  })

  return <EditorContent editor={editor} />
}
```

**Option 2: Server-side HTML (Future)**
The `IContentRenderingService` can convert JSON to HTML:

```json
GET /api/lessons/123?format=html

{
  "id": "123",
  "title": "Lesson 1",
  "contentHtml": "<p>Welcome to this lesson on <strong>prayer</strong></p>..."
}
```

## Validation

### JSON Structure Validation

The `TiptapJsonValidator` enforces:

1. **Valid JSON structure**: Must be parseable
2. **Root type**: Must have `type: "doc"`
3. **Max depth**: 50 nested levels (prevents attack)
4. **Max size**: 2MB per JSON field
5. **No malicious patterns**: Blocks `javascript:`, `data:`, `vbscript:` URLs
6. **Image URL validation**: HTTPS only (domain whitelist ready)

### Example Valid JSON

```json
{
  "type": "doc",
  "content": [
    {
      "type": "paragraph",
      "content": [
        { "type": "text", "text": "Hello " },
        {
          "type": "text",
          "marks": [{ "type": "bold" }],
          "text": "world"
        }
      ]
    },
    {
      "type": "heading",
      "attrs": { "level": 2 },
      "content": [{ "type": "text", "text": "Section Title" }]
    }
  ]
}
```

## Security Considerations

### XSS Prevention

1. **Storage**: JSON stored as-is (no sanitization at write time)
2. **Rendering**: HTML escaped during conversion to prevent script injection
3. **URLs**: Link and image URLs sanitized to block `javascript:`, `data:`, `vbscript:`
4. **Validation**: Image URLs must be HTTPS

### URL Whitelist (Optional)

Update `TiptapJsonValidator.cs` to enable domain whitelist:

```csharp
private static readonly string[] AllowedDomains = new[]
{
    "yourdomain.com",
    "cdn.yourdomain.com",
    "storage.yourdomain.com"
};
```

Uncomment the domain validation code in `ValidateImageUrls()` method.

## Configuration

### File Storage Path

Configure in `appsettings.json`:

```json
{
  "FileStorage": {
    "UploadPath": "wwwroot/uploads"
  }
}
```

**Options:**

- Absolute path: `"C:\\Storage\\Uploads"`
- Relative path: `"wwwroot/uploads"` (relative to `ContentRootPath`)
- Not configured: Falls back to temp directory

### Static Files

Ensure static files are served in `Program.cs`:

```csharp
app.UseStaticFiles(); // Serves wwwroot
```

## Migration Strategy

### Existing Data

Plain text fields are preserved:

- `Lesson.Transcript` → Keep for backward compatibility
- `QATranslation.QuestionText` / `AnswerText` → Keep

### Migration Options

**Option 1: Lazy Migration (Recommended)**

- On first edit, convert plain text → Tiptap JSON
- Save to both fields temporarily
- Phase out plain text over time

**Option 2: Background Migration**

- Script to convert all existing plain text to JSON
- Single paragraph node with text content

**Option 3: Dual Support**

- UI checks if `*Json` field exists, else uses plain text
- Gradual rollout without breaking changes

## Supported Tiptap Nodes

The `ContentRenderingService` supports:

**Block Nodes:**

- `paragraph` → `<p>`
- `heading` (levels 1-6) → `<h1>` to `<h6>`
- `bulletList` → `<ul>`
- `orderedList` → `<ol>`
- `listItem` → `<li>`
- `blockquote` → `<blockquote>`
- `codeBlock` → `<pre><code>`
- `horizontalRule` → `<hr>`
- `hardBreak` → `<br>`

**Inline Nodes:**

- `text` → text content (HTML escaped)
- `image` → `<img>` (with src, alt, title)

**Text Marks:**

- `bold` → `<strong>`
- `italic` → `<em>`
- `underline` → `<u>`
- `strike` → `<s>`
- `code` → `<code>`
- `link` → `<a>` (with href, target, rel)

## Future Enhancements

### 1. Draft vs Published Content

Add columns:

```sql
ALTER TABLE Lessons ADD DraftContentJson nvarchar(max)
ALTER TABLE Lessons ADD PublishedContentJson nvarchar(max)
```

### 2. Content Versioning

Create version history table:

```sql
CREATE TABLE LessonContentVersions (
  Id uniqueidentifier PRIMARY KEY,
  LessonId uniqueidentifier FOREIGN KEY,
  ContentJson nvarchar(max),
  VersionNumber int,
  CreatedAt datetimeoffset,
  CreatedBy nvarchar(450)
)
```

### 3. Search Indexing

Extract plain text for search:

```csharp
var searchText = _contentRenderingService.ExtractPlainText(lesson.ContentJson);
// Index searchText in Azure Cognitive Search or SQL full-text
```

### 4. Node.js Rendering Service

For production-grade HTML rendering:

```javascript
// renderService.js
const { generateHTML } = require("@tiptap/html");
const express = require("express");

app.post("/render", (req, res) => {
  const html = generateHTML(req.body.json, extensions);
  res.json({ html });
});
```

## Testing

### Manual Test Flow

1. **Upload Image:**

```bash
curl -X POST http://localhost:5000/api/media/upload \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@test-image.jpg" \
  -F "containerName=lesson-images"
```

2. **Create Lesson with JSON:**

```bash
curl -X POST http://localhost:5000/api/lessons \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "courseId": "guid-here",
    "title": "Test Lesson",
    "contentJson": {
      "type": "doc",
      "content": [{"type": "paragraph", "content": [{"type": "text", "text": "Test"}]}]
    },
    "type": "Video",
    "order": 1
  }'
```

3. **Get Lesson:**

```bash
curl http://localhost:5000/api/lessons/LESSON_ID \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Expected Response

```json
{
  "id": "guid",
  "title": "Test Lesson",
  "contentJson": {
    "type": "doc",
    "content": [...]
  },
  "transcriptJson": null,
  "type": "Video"
}
```

## Troubleshooting

### Issue: Image upload fails with 400

**Cause:** File too large or wrong type

**Solution:** Check file size (<5MB) and type (jpg/png/webp/gif)

### Issue: JSON validation error

**Cause:** Invalid Tiptap structure

**Solution:** Ensure JSON has `type: "doc"` root and valid content array

### Issue: Images not displaying

**Cause:** Static files not served or wrong path

**Solution:**

1. Add `app.UseStaticFiles()` in `Program.cs`
2. Verify `FileStorage:UploadPath` in `appsettings.json`
3. Check image URL starts with `/uploads/`

### Issue: XSS vulnerability concern

**Cause:** Rendering unsanitized HTML

**Solution:** Use `ContentRenderingService` which escapes all HTML and sanitizes URLs

## Support

For questions or issues:

1. Check validation errors in API response
2. Review `TiptapJsonValidator` for allowed patterns
3. Verify Tiptap editor configuration matches backend schema
