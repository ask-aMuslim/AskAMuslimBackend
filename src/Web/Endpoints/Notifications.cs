using AskAMuslimBackend.Application.Notifications.Commands.MarkAsRead;
using AskAMuslimBackend.Application.Notifications.Commands.MarkMyNotificationsAsRead;
using AskAMuslimBackend.Application.Notifications.Commands.UnregisterFcmToken;
using AskAMuslimBackend.Application.Notifications.Queries.GetMyNotifications;
using AskAMuslimBackend.Application.Notifications.Commands.RegisterFcmToken;
using AskAMuslimBackend.Application.Notifications.Commands.SendBroadcastNotification;
using AskAMuslimBackend.Application.Notifications.Commands.SendBulkNotifications;
using AskAMuslimBackend.Application.Notifications.Commands.SendNotification;
using AskAMuslimBackend.Application.Notifications.Commands.SendRoleBasedNotification;
using AskAMuslimBackend.Application.Notifications.Queries.GetUserNotifications;
using AskAMuslimBackend.Domain.Constants;

namespace AskAMuslimBackend.Web.Endpoints;

public class Notifications : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder app)
    {
        app.MapGet(GetUserNotifications, "{userId}")
            .RequireAuthorization(Policies.ViewNotifications);
        app.MapPost(SendNotification, "send")
            .RequireAuthorization(Policies.SendNotifications);
        app.MapPut(MarkAsRead, "{notificationId}/mark-read")
            .RequireAuthorization(Policies.ViewNotifications);

        // Current user's own in-app notifications (any signed-in user)
        app.MapGet(GetMyNotifications, "me")
            .RequireAuthorization()
            .WithName("GetMyNotifications");
        app.MapGet(GetMyUnreadCount, "me/unread-count")
            .RequireAuthorization()
            .WithName("GetMyUnreadNotificationsCount");
        app.MapPut(MarkMyNotificationAsRead, "me/{notificationId:guid}/read")
            .RequireAuthorization()
            .WithName("MarkMyNotificationAsRead");
        app.MapPut(MarkAllMyNotificationsAsRead, "me/read-all")
            .RequireAuthorization()
            .WithName("MarkAllMyNotificationsAsRead");

        // FCM Token Management (one token per device; call unregister on logout)
        app.MapPost(RegisterFcmToken, "register-token")
            .RequireAuthorization();
        app.MapPost(UnregisterFcmToken, "unregister-token")
            .RequireAuthorization()
            .WithName("UnregisterFcmToken");

        // Admin-only bulk notification endpoints
        app.MapPost(SendBulkNotifications, "send-to-users")
            .RequireAuthorization(Policies.ManageAllUsers);

        app.MapPost(SendRoleBasedNotification, "send-by-role")
            .RequireAuthorization(Policies.ManageAllUsers);

        app.MapPost(SendBroadcastNotification, "broadcast")
            .RequireAuthorization(Policies.ManageAllUsers);
    }

    public async Task<IResult> GetUserNotifications(ISender sender, Guid userId, bool unreadOnly = false)
    {
        var result = await sender.Send(new GetUserNotificationsQuery
        {
            UserId = userId,
            UnreadOnly = unreadOnly
        });
        return result.ToHttpResult();
    }

    public async Task<IResult> SendNotification(ISender sender, SendNotificationCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/Notifications/{result.Data}");
    }

    public async Task<IResult> MarkAsRead(ISender sender, Guid notificationId, MarkNotificationAsReadCommand command)
    {
        var result = await sender.Send(command with { NotificationId = notificationId });
        return result.ToHttpResult();
    }

    public async Task<IResult> RegisterFcmToken(ISender sender, RegisterFcmTokenCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> UnregisterFcmToken(ISender sender, UnregisterFcmTokenCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetMyNotifications(ISender sender, [AsParameters] GetMyNotificationsQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetMyUnreadCount(ISender sender)
    {
        var result = await sender.Send(new GetMyUnreadNotificationsCountQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> MarkMyNotificationAsRead(ISender sender, Guid notificationId)
    {
        var result = await sender.Send(new MarkMyNotificationAsReadCommand(notificationId));
        return result.ToHttpResult();
    }

    public async Task<IResult> MarkAllMyNotificationsAsRead(ISender sender)
    {
        var result = await sender.Send(new MarkAllMyNotificationsAsReadCommand());
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
