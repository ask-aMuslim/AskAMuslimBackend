using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Permissions.Queries.DiagnostPermissions;

public record DiagnostPermissionsQuery : IRequest<Result<PermissionDiagnosticDto>>;

public class DiagnostPermissionsQueryHandler : IRequestHandler<DiagnostPermissionsQuery, Result<PermissionDiagnosticDto>>
{
    private readonly IUser _user;
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IPermissionService _permissionService;
    private readonly IApplicationDbContext _context;

    public DiagnostPermissionsQueryHandler(
        IUser user,
        UserManager<Domain.Entities.User> userManager,
        IPermissionService permissionService,
        IApplicationDbContext context)
    {
        _user = user;
        _userManager = userManager;
        _permissionService = permissionService;
        _context = context;
    }

    public async Task<Result<PermissionDiagnosticDto>> Handle(DiagnostPermissionsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result<PermissionDiagnosticDto>.Failure(new[] { "Unauthorized" });
        }

        var userId = Guid.Parse(_user.Id);
        var user = await _userManager.FindByIdAsync(userId.ToString());
        
        if (user == null)
        {
            return Result<PermissionDiagnosticDto>.Failure(new[] { "User not found" });
        }

        // Get admin record
        var admin = await _context.Admins
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        // Get UserPermissions record directly from database
        var userPermissionRecord = await _context.UserPermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(up => up.UserId == userId, cancellationToken);

        // Get permissions via service
        var permissionsFromService = await _permissionService.GetUserPermissionsAsync(_user.Id!, cancellationToken);

        // Test some permissions
        var testResults = new Dictionary<string, bool>
        {
            ["ViewDashboard"] = (permissionsFromService & Permission.ViewDashboard) == Permission.ViewDashboard,
            ["ViewAnalytics"] = (permissionsFromService & Permission.ViewAnalytics) == Permission.ViewAnalytics,
            ["CreateAdmin"] = (permissionsFromService & Permission.CreateAdmin) == Permission.CreateAdmin,
            ["AllPermissions"] = permissionsFromService == Permission.AllPermissions
        };

        var diagnostic = new PermissionDiagnosticDto
        {
            CurrentUserId = _user.Id,
            UserEmail = user.Email ?? "N/A",
            UserRole = user.Role.ToString(),
            IsSuperAdmin = admin?.IsSuperAdmin ?? false,
            AdminId = admin?.Id.ToString(),
            
            UserPermissionRecordExists = userPermissionRecord != null,
            PermissionsValueInDatabase = userPermissionRecord?.Permissions.ToString() ?? "NULL",
            PermissionsNumericValue = userPermissionRecord != null ? (long)userPermissionRecord.Permissions : 0,
            
            PermissionsFromService = permissionsFromService.ToString(),
            PermissionsFromServiceNumeric = (long)permissionsFromService,
            
            BitwiseTestResults = testResults,
            
            DiagnosisNotes = new List<string>
            {
                userPermissionRecord == null ? "❌ UserPermissions record is MISSING!" : "✓ UserPermissions record exists",
                admin?.IsSuperAdmin == true ? "✓ User is marked as SuperAdmin" : "❌ User is NOT marked as SuperAdmin",
                permissionsFromService == Permission.AllPermissions ? "✓ Service returns AllPermissions" : "❌ Service does NOT return AllPermissions",
                userPermissionRecord?.Permissions == Permission.AllPermissions ? "✓ Database has AllPermissions (-1)" : "❌ Database does NOT have AllPermissions",
                testResults["ViewDashboard"] ? "✓ Bitwise check works for ViewDashboard" : "❌ Bitwise check FAILS for ViewDashboard"
            }
        };

        return Result<PermissionDiagnosticDto>.Success(diagnostic);
    }
}

public class PermissionDiagnosticDto
{
    public string CurrentUserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public bool IsSuperAdmin { get; set; }
    public string? AdminId { get; set; }
    
    public bool UserPermissionRecordExists { get; set; }
    public string PermissionsValueInDatabase { get; set; } = string.Empty;
    public long PermissionsNumericValue { get; set; }
    
    public string PermissionsFromService { get; set; } = string.Empty;
    public long PermissionsFromServiceNumeric { get; set; }
    
    public Dictionary<string, bool> BitwiseTestResults { get; set; } = new();
    public List<string> DiagnosisNotes { get; set; } = new();
}
