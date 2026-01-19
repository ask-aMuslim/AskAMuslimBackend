using AskAMuslimBackend.Application.Notifications.Commands.MarkAsRead;
using AskAMuslimBackend.Application.Notifications.Commands.RegisterFcmToken;
using AskAMuslimBackend.Application.Notifications.Commands.SendBroadcastNotification;
using AskAMuslimBackend.Application.Notifications.Commands.SendBulkNotifications;
using AskAMuslimBackend.Application.Notifications.Commands.SendNotification;
using AskAMuslimBackend.Application.Notifications.Commands.SendRoleBasedNotification;
using AskAMuslimBackend.Application.Notifications.Queries.GetUserNotifications;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

public class Notifications : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder app)
    {
        app.MapGet(GetUserNotifications, "{userId}").RequireAuthorization();
        app.MapPost(SendNotification, "send").RequireAuthorization();
        app.MapPut(MarkAsRead, "{notificationId}/mark-read").RequireAuthorization();
        
        // FCM Token Management
        app.MapPost(RegisterFcmToken, "register-token").RequireAuthorization();
        
        // Admin-only bulk notification endpoints
        app.MapPost(SendBulkNotifications, "send-to-users")
            .RequireAuthorization(Policies.ManageAllUsers);
        
        app.MapPost(SendRoleBasedNotification, "send-by-role")
            .RequireAuthorization(Policies.ManageAllUsers);
        
        app.MapPost(SendBroadcastNotification, "broadcast")
            .RequireAuthorization(Policies.ManageAllUsers);
    }

    public async Task<IResult> GetUserNotifications(ISender sender, Guid userId, [AsParameters] GetUserNotificationsQuery query)
    {
        var result = await sender.Send(query with { UserId = userId });
        return result.ToHttpResult();
    }

    public async Task<IResult> SendNotification(ISender sender, SendNotificationCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/Notifications/{result.Data}");
    }

    public async Task<IResult> MarkAsRead(ISender sender, Guid notificationId)
    {
        var result = await sender.Send(new MarkNotificationAsReadCommand { NotificationId = notificationId });
        return result.ToHttpResult();
    }

    public async Task<IResult> RegisterFcmToken(ISender sender, RegisterFcmTokenCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> SendBulkNotifications(ISender sender, SendBulkNotificationsCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> SendRoleBasedNotification(ISender sender, SendRoleBasedNotificationCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> SendBroadcastNotification(ISender sender, SendBroadcastNotificationCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}

