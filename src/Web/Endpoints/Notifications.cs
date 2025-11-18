using AskAMuslimBackend.Application.Notifications.Commands.MarkAsRead;
using AskAMuslimBackend.Application.Notifications.Commands.SendNotification;
using AskAMuslimBackend.Application.Notifications.Queries.GetUserNotifications;
using AskAMuslimBackend.Domain.Constants;

namespace AskAMuslimBackend.Web.Endpoints;

public class Notifications : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder app)
    {
        app.MapGet(GetUserNotifications, "{userId}").RequireAuthorization();
        app.MapPost(SendNotification, "send").RequireAuthorization();
        app.MapPut(MarkAsRead, "{notificationId}/mark-read").RequireAuthorization();
    }

    public async Task<List<NotificationDto>> GetUserNotifications(ISender sender, Guid userId, [AsParameters] GetUserNotificationsQuery query)
    {
        return await sender.Send(query with { UserId = userId });
    }

    public async Task<Guid> SendNotification(ISender sender, SendNotificationCommand command)
    {
        return await sender.Send(command);
    }

    public async Task<IResult> MarkAsRead(ISender sender, Guid notificationId)
    {
        await sender.Send(new MarkNotificationAsReadCommand { NotificationId = notificationId });
        return Results.NoContent();
    }
}
