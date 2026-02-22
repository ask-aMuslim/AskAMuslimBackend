
using AskAMuslimBackend.Application.Reports.Commands;
using AskAMuslimBackend.Application.Reports.Queries;
using AskAMuslimBackend.Domain.Constants;

namespace AskAMuslimBackend.Web.Endpoints;

public class Reports : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetAllReports)
            .RequireAuthorization(Policies.ViewReports)
            .WithName("GetReports")
            .WithSummary("Get all reports");

        groupBuilder.MapGet(GetReportById, "{id}")
            .RequireAuthorization(Policies.ViewReports)
            .WithName("GetReport")
            .WithSummary("Get report by id");

        groupBuilder.MapPost(CreateReport)
            .RequireAuthorization()
            .WithName("CreateReport")
            .WithSummary("Create a new report");

        groupBuilder.MapPut(UpdateReportStatus, "{id}/status")
            .RequireAuthorization(Policies.ManageReports)
            .WithName("UpdateReportStatus")
            .WithSummary("Update report status");

        groupBuilder.MapDelete(DeleteReport, "{id}")
            .RequireAuthorization(Policies.ManageReports)
            .WithName("DeleteReport")
            .WithSummary("Delete report");
    }

    public async Task<IResult> GetAllReports(ISender sender, [AsParameters] GetAllReportsQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetReportById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetReportByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateReport(ISender sender, CreateReportCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Reports)}/{result.Data}");
    }

    public async Task<IResult> UpdateReportStatus(ISender sender, Guid id, UpdateReportStatusCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> DeleteReport(ISender sender, Guid id)
    {
        var result = await sender.Send(new DeleteReportCommand(id));
        return result.ToHttpResult();
    }
}
