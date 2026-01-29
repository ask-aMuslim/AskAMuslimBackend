using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace AskAMuslimBackend.Infrastructure.Services;

public class OtpService : IOtpService
{
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;
    private readonly ILogger<OtpService> _logger;
    private const int OtpExpirationMinutes = 10;
    private const int MaxAttempts = 5;

    public OtpService(IMemoryCache cache, IEmailService emailService, ILogger<OtpService> logger)
    {
        _cache = cache;
        _emailService = emailService;
        _logger = logger;
    }

    public Task<string> GenerateOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        // Generate a secure 6-digit OTP
        var otp = GenerateSecureOtp();
        
        var otpRecord = new OtpRecord
        {
            Email = email.ToLowerInvariant(),
            Otp = otp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpirationMinutes),
            AttemptCount = 0
        };

        var cacheKey = GetCacheKey(email);
        _cache.Set(cacheKey, otpRecord, TimeSpan.FromMinutes(OtpExpirationMinutes));

        _logger.LogInformation("OTP generated for {Email}, expires at {ExpiresAt}", email, otpRecord.ExpiresAt);
        
        return Task.FromResult(otp);
    }

    public Task<bool> ValidateOtpAsync(string email, string otp, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(email);
        
        if (!_cache.TryGetValue<OtpRecord>(cacheKey, out var otpRecord) || otpRecord == null)
        {
            _logger.LogWarning("OTP validation failed: No OTP found for {Email}", email);
            return Task.FromResult(false);
        }

        // Check expiration
        if (DateTime.UtcNow > otpRecord.ExpiresAt)
        {
            _logger.LogWarning("OTP validation failed: OTP expired for {Email}", email);
            _cache.Remove(cacheKey);
            return Task.FromResult(false);
        }

        // Check max attempts
        if (otpRecord.AttemptCount >= MaxAttempts)
        {
            _logger.LogWarning("OTP validation failed: Max attempts reached for {Email}", email);
            _cache.Remove(cacheKey);
            return Task.FromResult(false);
        }

        // Increment attempt count
        otpRecord.AttemptCount++;
        _cache.Set(cacheKey, otpRecord, otpRecord.ExpiresAt - DateTime.UtcNow);

        // Validate OTP
        var isValid = otpRecord.Otp == otp;
        
        if (isValid)
        {
            _logger.LogInformation("OTP validated successfully for {Email}", email);
            _cache.Remove(cacheKey); // Remove after successful validation
        }
        else
        {
            _logger.LogWarning("OTP validation failed: Invalid OTP for {Email}. Attempt {Attempt}/{MaxAttempts}", 
                email, otpRecord.AttemptCount, MaxAttempts);
        }

        return Task.FromResult(isValid);
    }

    public Task RemoveOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(email);
        _cache.Remove(cacheKey);
        _logger.LogInformation("OTP removed for {Email}", email);
        return Task.CompletedTask;
    }

    public async Task SendOtpEmailAsync(string email, string otp, string userName, CancellationToken cancellationToken = default)
    {
        var subject = "Password Reset OTP - Ask A Muslim";
        var body = GenerateOtpEmailHtml(otp, userName);

        await _emailService.SendEmailAsync(email, subject, body, isHtml: true);
        _logger.LogInformation("OTP email sent to {Email}", email);
    }

    private static string GenerateSecureOtp()
    {
        // Generate a cryptographically secure 6-digit OTP
        var number = RandomNumberGenerator.GetInt32(100000, 999999);
        return number.ToString();
    }

    private static string GetCacheKey(string email)
    {
        return $"OTP_{email.ToLowerInvariant()}";
    }

    private static string GenerateOtpEmailHtml(string otp, string userName)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Reset OTP</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f7fa;
            margin: 0;
            padding: 0;
        }}
        .email-container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 40px 20px;
            text-align: center;
            color: #ffffff;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 600;
        }}
        .content {{
            padding: 40px 30px;
            color: #333333;
        }}
        .greeting {{
            font-size: 18px;
            margin-bottom: 20px;
            color: #333333;
        }}
        .message {{
            font-size: 16px;
            line-height: 1.6;
            color: #555555;
            margin-bottom: 30px;
        }}
        .otp-container {{
            background-color: #f8f9fa;
            border: 2px dashed #667eea;
            border-radius: 8px;
            padding: 30px;
            text-align: center;
            margin: 30px 0;
        }}
        .otp-label {{
            font-size: 14px;
            color: #666666;
            margin-bottom: 10px;
            text-transform: uppercase;
            letter-spacing: 1px;
        }}
        .otp-code {{
            font-size: 36px;
            font-weight: 700;
            color: #667eea;
            letter-spacing: 8px;
            margin: 10px 0;
            font-family: 'Courier New', monospace;
        }}
        .expiry-note {{
            font-size: 14px;
            color: #999999;
            margin-top: 10px;
        }}
        .warning {{
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .warning-text {{
            font-size: 14px;
            color: #856404;
            margin: 0;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 30px;
            text-align: center;
            border-top: 1px solid #e9ecef;
        }}
        .footer-text {{
            font-size: 13px;
            color: #6c757d;
            margin: 5px 0;
        }}
        .support-link {{
            color: #667eea;
            text-decoration: none;
        }}
        .support-link:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <h1>🔐 Password Reset Request</h1>
        </div>
        
        <div class=""content"">
            <div class=""greeting"">
                Hello {userName},
            </div>
            
            <div class=""message"">
                We received a request to reset your password for your Ask A Muslim account. 
                Use the One-Time Password (OTP) below to complete the password reset process.
            </div>
            
            <div class=""otp-container"">
                <div class=""otp-label"">Your OTP Code</div>
                <div class=""otp-code"">{otp}</div>
                <div class=""expiry-note"">⏰ This code expires in 10 minutes</div>
            </div>
            
            <div class=""warning"">
                <p class=""warning-text"">
                    <strong>⚠️ Security Notice:</strong> If you didn't request a password reset, 
                    please ignore this email or contact our support team immediately. 
                    Never share this OTP with anyone.
                </p>
            </div>
        </div>
        
        <div class=""footer"">
            <p class=""footer-text"">
                Need help? <a href=""mailto:support@ask-a-muslim.com"" class=""support-link"">Contact Support</a>
            </p>
            <p class=""footer-text"">
                © 2026 Ask A Muslim. All rights reserved.
            </p>
        </div>
    </div>
</body>
</html>";
    }
}
