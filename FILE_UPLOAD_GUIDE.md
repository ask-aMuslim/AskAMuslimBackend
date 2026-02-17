# File Upload Implementation Guide

This document provides comprehensive information about the file upload functionality in the AskAMuslimBackend API.

## Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)
- [Upload Endpoints](#upload-endpoints)
- [File Types & Validation](#file-types--validation)
- [Storage & Configuration](#storage--configuration)
- [Frontend Integration](#frontend-integration)
- [Docker Configuration](#docker-configuration)
- [Security Considerations](#security-considerations)

## Overview

The backend supports uploading various file types including images, videos, audio files, and documents. Files are organized into containers (folders) and served via static file middleware.

### Key Features
- ✅ Multiple file type support (images, videos, audio, documents)
- ✅ File size validation per file type
- ✅ File extension and MIME type validation
- ✅ Unique filename generation (GUID-based)
- ✅ Container-based organization
- ✅ Persistent volume storage
- ✅ Static file serving

## Architecture

### Application Layer
**Commands:**
- `UploadAttachmentCommand` - General purpose file upload (most flexible)
- `UploadFileCommand` - File upload with container name validation
- `UploadImageCommand` - Image upload with container name validation

**Interfaces:**
- `IFileStorageService` - Abstraction for file storage operations

### Infrastructure Layer
**Services:**
- `FileStorageService` - Implementation of file storage using local filesystem

### Web Layer
**Endpoints:**
- `POST /api/media/upload` - Upload file endpoint

**Static File Serving:**
- Files are served from `/uploads/{containerName}/{filename}`
- Configured via `UseUploadStaticFiles()` extension

## Upload Endpoints

### Upload Attachment
```http
POST /api/media/upload
Content-Type: multipart/form-data
Authorization: Bearer {token}

Parameters:
- file: (binary) - The file to upload
- containerName: (optional) - The container name (default: "attachments")
```

**Example with cURL:**
```bash
curl -X POST https://aam-api.ask-a-muslim.com/api/media/upload \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "file=@/path/to/image.jpg" \
  -F "containerName=lesson-images"
```

**Example Response:**
```json
{
  "data": "/uploads/lesson-images/a1b2c3d4-e5f6-7890-abcd-ef1234567890.jpg",
  "succeeded": true,
  "errors": null
}
```

### Common Container Names
- `lesson-images` - Lesson thumbnail images
- `lesson-content` - Lesson content files (PDFs, videos, etc.)
- `lesson-thumbnails` - Lesson thumbnail images
- `event-images` - Event promotional images
- `editor-images` - Rich text editor images
- `qa-images` - Q&A related images
- `attachments` - General attachments

## File Types & Validation

### Supported File Types

#### Images
- **Extensions:** `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`, `.bmp`, `.svg`
- **MIME Types:** `image/jpeg`, `image/png`, `image/webp`, `image/gif`, `image/bmp`, `image/svg+xml`
- **Max Size:** 10 MB

#### Videos
- **Extensions:** `.mp4`, `.avi`, `.mov`, `.wmv`, `.flv`, `.mkv`, `.webm`, `.m4v`, `.mpeg`, `.mpg`
- **MIME Types:** `video/mp4`, `video/x-msvideo`, `video/quicktime`, `video/x-ms-wmv`, `video/x-flv`, `video/x-matroska`, `video/webm`, `video/mpeg`
- **Max Size:** 500 MB

#### Audio
- **Extensions:** `.mp3`, `.wav`, `.aac`, `.ogg`, `.flac`, `.m4a`, `.wma`, `.aiff`
- **MIME Types:** `audio/mpeg`, `audio/wav`, `audio/aac`, `audio/ogg`, `audio/flac`, `audio/x-m4a`, `audio/x-ms-wma`, `audio/aiff`
- **Max Size:** 50 MB

#### Documents
- **Extensions:** `.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.ppt`, `.pptx`, `.txt`, `.rtf`, `.odt`, `.ods`, `.odp`
- **MIME Types:** `application/pdf`, `application/msword`, `application/vnd.openxmlformats-officedocument.*`, etc.
- **Max Size:** 50 MB

### Validation Rules

All upload commands include FluentValidation validators that check:

1. **File Presence:** File must not be null
2. **File Extension:** Must match allowed extensions
3. **Content Type:** Must match allowed MIME types
4. **File Size:** Must be within size limits based on file type
5. **Container Name:** Must not be empty (and validated for specific commands)

**Example Validation Error:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "File": [
      "Image file size must not exceed 10MB."
    ]
  }
}
```

## Storage & Configuration

### Configuration Options

Add to `appsettings.json` or `appsettings.Production.json`:

```json
{
  "FileStorage": {
    "UploadPath": "/app/uploads"
  }
}
```

Or via environment variables:
```bash
FileStorage__UploadPath=/app/uploads
```

### Path Resolution

The `FileStorageService` resolves paths in the following order:

1. **Configured Absolute Path:** If `FileStorage:UploadPath` is an absolute path, use it directly
2. **Configured Relative Path:** If relative, combine with `ContentRootPath/wwwroot`
3. **Default Path:** If not configured, defaults to `ContentRootPath/wwwroot/uploads`
4. **Fallback to Temp:** If directory creation fails, uses `Path.GetTempPath()/Uploads`

### File Organization

Files are organized as follows:
```
uploads/
├── lesson-images/
│   ├── a1b2c3d4-e5f6-7890-abcd-ef1234567890.jpg
│   └── b2c3d4e5-f6g7-8901-bcde-fg2345678901.png
├── lesson-content/
│   ├── c3d4e5f6-g7h8-9012-cdef-gh3456789012.pdf
│   └── d4e5f6g7-h8i9-0123-defg-hi4567890123.mp4
├── event-images/
│   └── e5f6g7h8-i9j0-1234-efgh-ij5678901234.webp
└── attachments/
    └── f6g7h8i9-j0k1-2345-fghi-jk6789012345.txt
```

### Filename Generation

- Filenames are generated using `Guid.NewGuid()` + original file extension
- This ensures uniqueness and prevents filename conflicts
- Original filename is not preserved (security best practice)

## Frontend Integration

### JavaScript/TypeScript Example

```typescript
async function uploadFile(file: File, containerName: string = 'attachments'): Promise<string> {
  const formData = new FormData();
  formData.append('file', file);
  
  const response = await fetch(
    `https://aam-api.ask-a-muslim.com/api/media/upload?containerName=${containerName}`,
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${getAuthToken()}`,
      },
      body: formData,
    }
  );
  
  if (!response.ok) {
    throw new Error('Upload failed');
  }
  
  const result = await response.json();
  return result.data; // Returns the URL: /uploads/container/filename.ext
}
```

### Angular Example (from AdminPanel)

```typescript
import { ApiSdk } from '@api-sdk';

// Upload an image
async uploadEventImage(file: File) {
  try {
    const resp = await ApiSdk.uploadAttachment({
      body: { file },
      query: { containerName: 'event-images' },
    });
    
    const url = resp?.url || resp?.data?.url || resp;
    console.log('Uploaded file URL:', url);
    return url;
  } catch (error) {
    console.error('Upload failed:', error);
    throw error;
  }
}
```

### React/Vue Example

```typescript
const handleFileUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
  const file = event.target.files?.[0];
  if (!file) return;
  
  try {
    const url = await uploadFile(file, 'lesson-images');
    console.log('File uploaded to:', url);
    // Use the URL in your application
    setImageUrl(url);
  } catch (error) {
    console.error('Upload error:', error);
  }
};
```

## Docker Configuration

### Docker Compose Setup

The file upload system is configured in `docker-compose.yml`:

```yaml
services:
  api:
    build: ./api
    environment:
      - FileStorage__UploadPath=/app/uploads
    volumes:
      - api_uploads:/app/uploads

volumes:
  api_uploads:
    driver: local
    driver_opts:
      type: none
      o: bind
      device: /srv/docker/volumes/api_uploads
```

### Volume Setup

1. **Create the upload directory on the host:**
   ```bash
   sudo mkdir -p /srv/docker/volumes/api_uploads
   sudo chown -R 1000:1000 /srv/docker/volumes/api_uploads
   ```

2. **Set appropriate permissions:**
   ```bash
   sudo chmod -R 755 /srv/docker/volumes/api_uploads
   ```

3. **Verify volume mount:**
   ```bash
   docker exec -it api ls -la /app/uploads
   ```

### Nginx Configuration

Files are accessible through the API container's static file middleware. Nginx proxies requests to the API:

```nginx
# In nginx/conf/default.conf
location / {
    proxy_pass http://api:8080;
    # ... other proxy settings
}
```

The API serves files from `/uploads/*` via the `UseUploadStaticFiles()` middleware.

## Security Considerations

### Authentication & Authorization
- All upload endpoints require authentication (`RequireAuthorization()`)
- Ensure proper JWT token is included in requests
- Consider adding role-based authorization for sensitive containers

### File Validation
✅ **Implemented:**
- File extension validation
- MIME type validation
- File size limits
- Unique filename generation (prevents path traversal)

⚠️ **Additional Recommendations:**
- Consider virus scanning for uploaded files
- Implement rate limiting for upload endpoints
- Add audit logging for file uploads
- Consider using Content Security Policy headers

### Best Practices

1. **Never trust client-provided filenames** - Always generate new filenames
2. **Validate both extension and MIME type** - Prevents disguised malicious files
3. **Limit file sizes** - Prevents denial of service attacks
4. **Use persistent volumes** - Data survives container restarts
5. **Set proper file permissions** - Prevents unauthorized access
6. **Implement backup strategy** - Regular backups of upload directory
7. **Monitor disk usage** - Set up alerts for disk space

### File Cleanup

Currently, files are not automatically deleted when entities are removed. Consider implementing:

1. **Soft delete with cleanup job:**
   ```csharp
   public async Task CleanupOrphanedFiles()
   {
       // Find files not referenced in database
       // Delete files older than X days
   }
   ```

2. **Reference counting:**
   - Track file usage across entities
   - Delete only when reference count reaches zero

3. **Scheduled cleanup:**
   - Use Hangfire to schedule cleanup jobs
   - Run daily/weekly based on needs

## Troubleshooting

### Common Issues

**Issue:** "File is empty or null"
- **Cause:** File not properly included in request
- **Solution:** Ensure `Content-Type: multipart/form-data` header and proper FormData usage

**Issue:** "Failed to upload: UnauthorizedAccessException"
- **Cause:** Container process lacks permissions to write to upload directory
- **Solution:** Check directory permissions: `chmod 755` and `chown` to appropriate user

**Issue:** "File size exceeds limit"
- **Cause:** File is larger than allowed size for its type
- **Solution:** Compress file or split into smaller parts

**Issue:** 404 when accessing uploaded file
- **Cause:** Static file middleware not configured or volume not mounted
- **Solution:** Verify `UseUploadStaticFiles()` is called in Program.cs and volume is mounted

### Debugging

1. **Check file was saved:**
   ```bash
   docker exec -it api find /app/uploads -type f -name "*.jpg"
   ```

2. **Check permissions:**
   ```bash
   docker exec -it api ls -la /app/uploads
   ```

3. **Check disk space:**
   ```bash
   docker exec -it api df -h
   ```

4. **View API logs:**
   ```bash
   docker logs api | grep -i upload
   ```

## Future Enhancements

### Potential Improvements

1. **Cloud Storage Integration**
   - Azure Blob Storage
   - AWS S3
   - Google Cloud Storage

2. **Image Processing**
   - Automatic thumbnail generation
   - Image optimization/compression
   - Format conversion

3. **CDN Integration**
   - CloudFlare
   - Azure CDN
   - AWS CloudFront

4. **Advanced Features**
   - Chunked upload for large files
   - Resume interrupted uploads
   - Progress reporting
   - Multi-file upload support
   - ZIP archive upload/extraction

### Example: Cloud Storage Interface

```csharp
public interface IFileStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string containerName, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string fileUrl, string containerName, CancellationToken cancellationToken = default);
    Task<Stream> DownloadFileAsync(string fileUrl, string containerName, CancellationToken cancellationToken = default);
    
    // Future enhancements
    Task<string> UploadFileAsync(Stream stream, string fileName, string containerName, CancellationToken cancellationToken = default);
    Task<string> GetDownloadUrlAsync(string fileUrl, string containerName, TimeSpan expiresIn, CancellationToken cancellationToken = default);
    Task<FileMetadata> GetFileMetadataAsync(string fileUrl, string containerName, CancellationToken cancellationToken = default);
}
```

## Resources

- [ASP.NET Core File Uploads](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads)
- [ASP.NET Core Static Files](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files)
- [FluentValidation](https://docs.fluentvalidation.net/)
- [Docker Volumes](https://docs.docker.com/storage/volumes/)

---

**Last Updated:** February 17, 2026  
**Version:** 1.0.0  
**Maintainer:** AskAMuslimBackend Team
