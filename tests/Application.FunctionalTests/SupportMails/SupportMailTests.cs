using AskAMuslimBackend.Application.SupportMails.Commands.AssignSupportMail;
using AskAMuslimBackend.Application.SupportMails.Commands.CreateSupportMail;
using AskAMuslimBackend.Application.SupportMails.Commands.MarkMySupportMailAsRead;
using AskAMuslimBackend.Application.SupportMails.Commands.ReplyToMySupportMail;
using AskAMuslimBackend.Application.SupportMails.Commands.ReplyToSupportMail;
using AskAMuslimBackend.Application.SupportMails.Commands.UpdateSupportMailStatus;
using AskAMuslimBackend.Application.SupportMails.Queries.GetMySupportMailById;
using AskAMuslimBackend.Application.SupportMails.Queries.GetMySupportMails;
using AskAMuslimBackend.Application.SupportMails.Queries.GetSupportMailById;
using AskAMuslimBackend.Application.SupportMails.Queries.GetSupportMailEligibility;
using AskAMuslimBackend.Application.SupportMails.Queries.GetSupportMails;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.FunctionalTests.SupportMails;

using static Testing;

public class SupportMailTests : BaseTestFixture
{
    private const string Password = "Testing1234!";

    private static Task<string> RunAsRequesterAsync(string email = "requester@local") =>
        RunAsUserAsync(email, Password, UserRole.Student);

    private static Task<string> RunAsAdminAsync() =>
        RunAsUserAsync("admin@local", Password, UserRole.Admin);

    private static async Task<Guid> CreatePreacherAsync(string email)
    {
        var userId = Guid.Parse(await RunAsUserAsync(email, Password, UserRole.Preacher));
        await AddAsync(new Preacher { UserId = userId });
        return userId;
    }

    private static async Task<Guid> CreateMailAsync(string subject = "Music Enquiry", string? explanation = "Is music allowed?")
    {
        var result = await SendAsync(new CreateSupportMailCommand
        {
            Topic = MeetingInquiryTopic.Fiqh,
            Subject = subject,
            Explanation = explanation
        });

        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));
        return result.Data;
    }

    private static Task MoveCreatedBackAsync(Guid mailId, TimeSpan by) =>
        ExecuteDbContextAsync(context => context.SupportMails
            .Where(m => m.Id == mailId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Created, m => m.Created - by)));

    [Test]
    public async Task Create_StoresFirstMessageAndBlocksSecondWithinAnHour()
    {
        await RunAsRequesterAsync();

        var mailId = await CreateMailAsync();

        var mail = await SendAsync(new GetMySupportMailByIdQuery(mailId));
        mail.Data!.Number.ShouldBe(1);
        mail.Data.Subject.ShouldBe("Music Enquiry");
        mail.Data.Status.ShouldBe(SupportMailStatus.AwaitingResponse);
        mail.Data.Messages.Single().Body.ShouldBe("Is music allowed?");

        var second = await SendAsync(new CreateSupportMailCommand { Topic = MeetingInquiryTopic.Quran, Subject = "Another" });
        second.Succeeded.ShouldBeFalse();

        var eligibility = await SendAsync(new GetSupportMailEligibilityQuery());
        eligibility.Data!.CanCreate.ShouldBeFalse();
        eligibility.Data.NextAllowedAt.ShouldNotBeNull();
    }

    [Test]
    public async Task Create_WithoutExplanation_UsesSubjectAsFirstMessage()
    {
        await RunAsRequesterAsync();

        var mailId = await CreateMailAsync(subject: "Short question", explanation: null);

        var mail = await SendAsync(new GetMySupportMailByIdQuery(mailId));
        mail.Data!.Messages.Single().Body.ShouldBe("Short question");
    }

    [Test]
    public async Task Create_AfterCooldown_IncrementsNumberPerUser()
    {
        await RunAsRequesterAsync();
        var firstId = await CreateMailAsync();
        await MoveCreatedBackAsync(firstId, TimeSpan.FromHours(2));

        var secondId = await CreateMailAsync(subject: "Second");

        (await SendAsync(new GetMySupportMailByIdQuery(secondId))).Data!.Number.ShouldBe(2);

        // Another user's numbering starts from 1
        await RunAsRequesterAsync("other@local");
        var otherId = await CreateMailAsync();
        (await SendAsync(new GetMySupportMailByIdQuery(otherId))).Data!.Number.ShouldBe(1);
    }

    [Test]
    public async Task OtherUser_CannotReadOrReplyToMail()
    {
        await RunAsRequesterAsync();
        var mailId = await CreateMailAsync();

        await RunAsRequesterAsync("intruder@local");

        (await SendAsync(new GetMySupportMailByIdQuery(mailId))).Succeeded.ShouldBeFalse();
        (await SendAsync(new ReplyToMySupportMailCommand { Id = mailId, Body = "hi" })).Succeeded.ShouldBeFalse();
        (await SendAsync(new GetMySupportMailsQuery())).Data!.Items.ShouldBeEmpty();
    }

    [Test]
    public async Task Replies_UpdateStatusAndUnreadFlag()
    {
        await RunAsRequesterAsync();
        var mailId = await CreateMailAsync();

        await RunAsAdminAsync();
        var staffReply = await SendAsync(new ReplyToSupportMailCommand { Id = mailId, Body = "Thank you for your inquiry." });
        staffReply.Succeeded.ShouldBeTrue(string.Join(", ", staffReply.Errors));

        await RunAsRequesterAsync();
        var list = await SendAsync(new GetMySupportMailsQuery());
        var item = list.Data!.Items.Single();
        item.Status.ShouldBe(SupportMailStatus.Responded);
        item.HasUnreadReply.ShouldBeTrue();
        item.Preview.ShouldBe("Is music allowed?");

        var detail = await SendAsync(new GetMySupportMailByIdQuery(mailId));
        var staffMessage = detail.Data!.Messages.Last();
        staffMessage.SenderType.ShouldBe(SupportMailSenderType.Staff);
        staffMessage.SenderName.ShouldBeNull(); // shown as "Support Team"

        await SendAsync(new MarkMySupportMailAsReadCommand(mailId));
        (await SendAsync(new GetMySupportMailsQuery())).Data!.Items.Single().HasUnreadReply.ShouldBeFalse();

        await SendAsync(new ReplyToMySupportMailCommand { Id = mailId, Body = "Where can I learn more?" });
        var afterUserReply = await SendAsync(new GetMySupportMailByIdQuery(mailId));
        afterUserReply.Data!.Status.ShouldBe(SupportMailStatus.AwaitingResponse);
        afterUserReply.Data.Messages.Count.ShouldBe(3);
    }

    [Test]
    public async Task AssignedModerator_CanReadAndReply_UnassignedCannot()
    {
        var assignedId = await CreatePreacherAsync("assigned@local");
        await CreatePreacherAsync("unassigned@local");

        await RunAsRequesterAsync();
        var mailId = await CreateMailAsync();

        await RunAsAdminAsync();
        var assign = await SendAsync(new AssignSupportMailCommand { Id = mailId, ModeratorId = assignedId });
        assign.Succeeded.ShouldBeTrue(string.Join(", ", assign.Errors));

        await RunAsUserAsync("unassigned@local", Password, UserRole.Preacher);
        (await SendAsync(new GetSupportMailByIdQuery(mailId))).Succeeded.ShouldBeFalse();
        (await SendAsync(new ReplyToSupportMailCommand { Id = mailId, Body = "nope" })).Succeeded.ShouldBeFalse();
        (await SendAsync(new GetSupportMailsQuery { AssignedToMe = true })).Data!.Items.ShouldBeEmpty();

        await RunAsUserAsync("assigned@local", Password, UserRole.Preacher);
        (await SendAsync(new GetSupportMailsQuery { AssignedToMe = true })).Data!.Items.Single().Id.ShouldBe(mailId);
        (await SendAsync(new GetSupportMailByIdQuery(mailId))).Succeeded.ShouldBeTrue();
        (await SendAsync(new ReplyToSupportMailCommand { Id = mailId, Body = "Here is guidance." })).Succeeded.ShouldBeTrue();

        var mail = await FindAsync<SupportMail>(mailId);
        mail!.Status.ShouldBe(SupportMailStatus.Responded);
    }

    [Test]
    public async Task UserReply_ReopensClosedMail()
    {
        await RunAsRequesterAsync();
        var mailId = await CreateMailAsync();

        await RunAsAdminAsync();
        await SendAsync(new CloseSupportMailCommand(mailId));
        (await FindAsync<SupportMail>(mailId))!.Status.ShouldBe(SupportMailStatus.Closed);

        await RunAsRequesterAsync();
        var reply = await SendAsync(new ReplyToMySupportMailCommand { Id = mailId, Body = "One more question" });
        reply.Succeeded.ShouldBeTrue();

        var mail = await FindAsync<SupportMail>(mailId);
        mail!.Status.ShouldBe(SupportMailStatus.AwaitingResponse);
        mail.ClosedAt.ShouldBeNull();
    }
}
