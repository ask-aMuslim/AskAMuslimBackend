namespace AskAMuslimBackend.Domain.Enums;

public enum ReportStatus
{
    Pending = 1,
    UnderReview = 2,
    Resolved = 3,
    Dismissed = 4
}

public enum ReportType
{
    QA = 1,
    Video = 2,
    Question = 3,
    Comment = 4,
    User = 5,
    Course = 6,
    Event = 7,
    Other = 99
}

public enum ReportReason
{
    InappropriateContent = 1,
    Spam = 2,
    Harassment = 3,
    Misinformation = 4,
    OffensiveLanguage = 5,
    Copyright = 6,
    Privacy = 7,
    Other = 99
}
