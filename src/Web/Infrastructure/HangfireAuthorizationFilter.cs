using Hangfire.Dashboard;

namespace AskAMuslimBackend.Web.Infrastructure;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Allow access only to authenticated administrators
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("Administrator");
    }
}
