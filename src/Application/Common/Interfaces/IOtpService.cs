namespace AskAMuslimBackend.Application.Common.Interfaces;

public interface IOtpService
{
    /// <summary>
    /// Generates a 6-digit OTP and stores it with expiration
    /// </summary>
    Task<string> GenerateOtpAsync(string email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates the OTP for the given email
    /// </summary>
    Task<bool> ValidateOtpAsync(string email, string otp, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes the OTP after successful validation or expiration
    /// </summary>
    Task RemoveOtpAsync(string email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends OTP email with HTML template
    /// </summary>
    Task SendOtpEmailAsync(string email, string otp, string userName, CancellationToken cancellationToken = default);
}
