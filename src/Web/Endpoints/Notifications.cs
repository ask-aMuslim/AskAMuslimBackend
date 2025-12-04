using AskAMuslimBackend.Application.Notifications.Commands.MarkAsRead;
using AskAMuslimBackend.Application.Notifications.Commands.SendNotification;
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
}
