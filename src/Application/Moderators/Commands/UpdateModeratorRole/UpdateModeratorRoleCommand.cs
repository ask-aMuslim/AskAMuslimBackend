using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace AskAMuslimBackend.Application.Moderators.Commands.UpdateModeratorRole;

[Authorize(Policy = Policies.ManageAdmins)]
public record UpdateModeratorRoleCommand : IRequest<Result<Guid>>
{
    public Guid Id { get; init; }
    public UserRole NewRole { get; init; }
}

public class UpdateModeratorRoleCommandValidator : AbstractValidator<UpdateModeratorRoleCommand>
{
    public UpdateModeratorRoleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Moderator ID is required");

        RuleFor(x => x.NewRole)
            .Must(role => role == UserRole.Admin || role == UserRole.Instructor || role == UserRole.Preacher)
            .WithMessage("New role must be Admin, Instructor, or Preacher");
    }
}

public class UpdateModeratorRoleCommandHandler : IRequestHandler<UpdateModeratorRoleCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<Domain.Entities.User> _userManager;

    public UpdateModeratorRoleCommandHandler(
        IApplicationDbContext context,
        UserManager<Domain.Entities.User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<Result<Guid>> Handle(UpdateModeratorRoleCommand request, CancellationToken cancellationToken)
    {
        Guid userId;
        UserRole currentRole;
        string? bio = null;
        string? imageUrl = null;
        string? phoneNumber = null;
        string? specialization = null;
        string? website = null;
        string? linkedInProfile = null;
        int yearsOfExperience = 0;
        bool isVerified = false;
        bool isActive = true;
        DateTime joinedDate = DateTime.UtcNow;
        decimal rating = 0;
        int totalReviews = 0;
        bool isSuperAdmin = false;
        string? department = null;
        string? certifications = null;
        string? organization = null;

        // Try to find the moderator in Admin, Instructor, or Preacher tables
        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (admin != null)
        {
            currentRole = UserRole.Admin;
            userId = admin.UserId;
            bio = admin.Bio;
            imageUrl = admin.ImageUrl;
            phoneNumber = admin.PhoneNumber;
            isActive = admin.IsActive;
            joinedDate = admin.JoinedDate;
            isSuperAdmin = admin.IsSuperAdmin;
            department = admin.Department;

            // Check if already the requested role
            if (currentRole == request.NewRole)
            {
                return Result<Guid>.Failure(new[] { "Moderator is already in the requested role" });
            }

            // Remove from Admins table
            _context.Admins.Remove(admin);
        }
        else
        {
            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

            if (instructor != null)
            {
                currentRole = UserRole.Instructor;
                userId = instructor.UserId;
                bio = instructor.Bio;
                imageUrl = instructor.ImageUrl;
                phoneNumber = instructor.PhoneNumber;
                specialization = instructor.Specialization;
                website = instructor.Website;
                linkedInProfile = instructor.LinkedInProfile;
                yearsOfExperience = instructor.YearsOfExperience;
                isVerified = instructor.IsVerified;
                isActive = instructor.IsActive;
                joinedDate = instructor.JoinedDate;
                rating = instructor.Rating;
                totalReviews = instructor.TotalReviews;

                if (currentRole == request.NewRole)
                {
                    return Result<Guid>.Failure(new[] { "Moderator is already in the requested role" });
                }

                // Remove from Instructors table
                _context.Instructors.Remove(instructor);
            }
            else
            {
                var preacher = await _context.Preachers
                    .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

                if (preacher != null)
                {
                    currentRole = UserRole.Preacher;
                    userId = preacher.UserId;
                    bio = preacher.Bio;
                    imageUrl = preacher.ImageUrl;
                    phoneNumber = preacher.PhoneNumber;
                    specialization = preacher.Specialization;
                    website = preacher.Website;
                    linkedInProfile = preacher.LinkedInProfile;
                    yearsOfExperience = preacher.YearsOfExperience;
                    isVerified = preacher.IsVerified;
                    isActive = preacher.IsActive;
                    joinedDate = preacher.JoinedDate;
                    rating = preacher.Rating;
                    totalReviews = preacher.TotalReviews;
                    certifications = preacher.Certifications;
                    organization = preacher.Organization;

                    if (currentRole == request.NewRole)
                    {
                        return Result<Guid>.Failure(new[] { "Moderator is already in the requested role" });
                    }

                    // Remove from Preachers table
                    _context.Preachers.Remove(preacher);
                }
                else
                {
                    return Result<Guid>.Failure(new[] { "Moderator not found" });
                }
            }
        }

        // Get the associated user
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result<Guid>.Failure(new[] { "Associated user not found" });
        }

        // Update user role
        user.Role = request.NewRole;

        // Update ASP.NET Identity role
        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            return Result<Guid>.Failure(removeResult.Errors.Select(e => e.Description).ToArray());
        }

        var newRoleName = GetRoleName(request.NewRole);
        var addResult = await _userManager.AddToRoleAsync(user, newRoleName);
        if (!addResult.Succeeded)
        {
            return Result<Guid>.Failure(addResult.Errors.Select(e => e.Description).ToArray());
        }

        // Update user entity
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result<Guid>.Failure(updateResult.Errors.Select(e => e.Description).ToArray());
        }

        // Create new entity in the appropriate table
        Guid newEntityId;
        switch (request.NewRole)
        {
            case UserRole.Admin:
                var newAdmin = new Domain.Entities.Admin
                {
                    UserId = userId,
                    Bio = bio,
                    ImageUrl = imageUrl,
                    PhoneNumber = phoneNumber,
                    Department = department,
                    IsSuperAdmin = isSuperAdmin,
                    IsActive = isActive,
                    JoinedDate = joinedDate
                };
                _context.Admins.Add(newAdmin);
                await _context.SaveChangesAsync(cancellationToken);
                newEntityId = newAdmin.Id;
                break;

            case UserRole.Instructor:
                var newInstructor = new Domain.Entities.Instructor
                {
                    UserId = userId,
                    Bio = bio,
                    ImageUrl = imageUrl,
                    PhoneNumber = phoneNumber,
                    Specialization = specialization,
                    Website = website,
                    LinkedInProfile = linkedInProfile,
                    YearsOfExperience = yearsOfExperience,
                    IsVerified = isVerified,
                    IsActive = isActive,
                    JoinedDate = joinedDate,
                    Rating = rating,
                    TotalReviews = totalReviews
                };
                _context.Instructors.Add(newInstructor);
                await _context.SaveChangesAsync(cancellationToken);
                newEntityId = newInstructor.Id;
                break;

            case UserRole.Preacher:
                var newPreacher = new Domain.Entities.Preacher
                {
                    UserId = userId,
                    Bio = bio,
                    ImageUrl = imageUrl,
                    PhoneNumber = phoneNumber,
                    Specialization = specialization,
                    Website = website,
                    LinkedInProfile = linkedInProfile,
                    YearsOfExperience = yearsOfExperience,
                    IsVerified = isVerified,
                    IsActive = isActive,
                    JoinedDate = joinedDate,
                    Rating = rating,
                    TotalReviews = totalReviews,
                    Certifications = certifications,
                    Organization = organization
                };
                _context.Preachers.Add(newPreacher);
                await _context.SaveChangesAsync(cancellationToken);
                newEntityId = newPreacher.Id;
                break;

            default:
                return Result<Guid>.Failure(new[] { "Invalid role" });
        }

        return Result<Guid>.Success(newEntityId);
    }

    private static string GetRoleName(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => Domain.Constants.Roles.Administrator,
            UserRole.Instructor => Domain.Constants.Roles.Instructor,
            UserRole.Preacher => Domain.Constants.Roles.Preacher,
            _ => role.ToString()
        };
    }
}
