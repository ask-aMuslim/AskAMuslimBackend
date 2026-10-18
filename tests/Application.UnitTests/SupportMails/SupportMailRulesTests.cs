using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.SupportMails.Commands.CreateSupportMail;
using AskAMuslimBackend.Application.SupportMails.Commands.ReplyToSupportMail;
using AskAMuslimBackend.Application.SupportMails.Common;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace AskAMuslimBackend.Application.UnitTests.SupportMails;

public class SupportMailRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public void RequesterMessage_SetsAwaitingResponse()
    {
        var mail = new SupportMail { Status = SupportMailStatus.Responded };

        SupportMailRules.AddMessage(mail, Guid.NewGuid(), SupportMailSenderType.Requester, " hello ", Now);

        mail.Status.ShouldBe(SupportMailStatus.AwaitingResponse);
        mail.LastMessageSenderType.ShouldBe(SupportMailSenderType.Requester);
        mail.LastMessageAt.ShouldBe(Now);
        mail.Messages.Single().Body.ShouldBe("hello");
    }

    [Test]
    public void StaffMessage_SetsResponded()
    {
        var mail = new SupportMail();

        SupportMailRules.AddMessage(mail, Guid.NewGuid(), SupportMailSenderType.Staff, "answer", Now);

        mail.Status.ShouldBe(SupportMailStatus.Responded);
        mail.LastMessageSenderType.ShouldBe(SupportMailSenderType.Staff);
    }

    [Test]
    public void MessageOnClosedMail_ReopensIt()
    {
        var mail = new SupportMail { Status = SupportMailStatus.Closed, ClosedAt = Now.AddDays(-1) };

        SupportMailRules.AddMessage(mail, Guid.NewGuid(), SupportMailSenderType.Requester, "one more thing", Now);

        mail.Status.ShouldBe(SupportMailStatus.AwaitingResponse);
        mail.ClosedAt.ShouldBeNull();
    }

    [Test]
    public async Task AssignedModerator_CanAccessWithoutPermission()
    {
        var moderatorId = Guid.NewGuid();
        var mail = new SupportMail { AssignedModeratorId = moderatorId };
        var permissions = new Mock<IPermissionService>(MockBehavior.Strict);

        var canAccess = await SupportMailRules.CanStaffAccessAsync(mail, moderatorId, Permission.ManageInquiryRequests, permissions.Object, CancellationToken.None);

        canAccess.ShouldBeTrue();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task OtherStaff_AccessDependsOnPermission(bool hasPermission)
    {
        var userId = Guid.NewGuid();
        var mail = new SupportMail { AssignedModeratorId = Guid.NewGuid() };
        var permissions = new Mock<IPermissionService>();
        permissions
            .Setup(p => p.HasPermissionAsync(userId.ToString(), Permission.ManageInquiryRequests, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasPermission);

        var canAccess = await SupportMailRules.CanStaffAccessAsync(mail, userId, Permission.ManageInquiryRequests, permissions.Object, CancellationToken.None);

        canAccess.ShouldBe(hasPermission);
    }

    [Test]
    public void CreateValidator_RejectsEmptySubjectAndLongExplanation()
    {
        var validator = new CreateSupportMailCommandValidator();

        var result = validator.Validate(new CreateSupportMailCommand
        {
            Topic = MeetingInquiryTopic.Fiqh,
            Subject = "",
            Explanation = new string('x', SupportMailRules.ExplanationMaxLength + 1)
        });

        result.Errors.Select(e => e.PropertyName).ShouldBe(new[] { "Subject", "Explanation" }, ignoreOrder: true);
    }

    [Test]
    public void CreateValidator_AcceptsMissingExplanation()
    {
        var validator = new CreateSupportMailCommandValidator();

        var result = validator.Validate(new CreateSupportMailCommand
        {
            Topic = MeetingInquiryTopic.GeneralInquiry,
            Subject = "Music Enquiry"
        });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ReplyValidator_RejectsEmptyBody()
    {
        var validator = new ReplyToSupportMailCommandValidator();

        var result = validator.Validate(new ReplyToSupportMailCommand { Id = Guid.NewGuid(), Body = " " });

        result.IsValid.ShouldBeFalse();
    }
}
