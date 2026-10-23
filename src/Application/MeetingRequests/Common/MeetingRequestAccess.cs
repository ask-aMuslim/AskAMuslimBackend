using AskAMuslimBackend.Application.Common.Exceptions;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.MeetingRequests.Common;

public static class MeetingRequestAccess
{
    /// <summary>
    /// Meeting requests hold private messages and responses, so a list for a given user may only be read by that user
    /// or by someone with the <see cref="Permission.ViewMeetingRequests"/> permission.
    /// </summary>
    public static async Task EnsureCanViewListOfAsync(
        Guid ownerId,
        IUser user,
        IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(user.Id))
        {
            throw new ForbiddenAccessException();
        }

        if (Guid.TryParse(user.Id, out var currentUserId) && currentUserId == ownerId)
        {
            return;
        }

        if (!await permissionService.HasPermissionAsync(user.Id, Permission.ViewMeetingRequests, cancellationToken))
        {
            throw new ForbiddenAccessException();
        }
    }
}
