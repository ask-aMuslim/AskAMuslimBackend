namespace AskAMuslimBackend.Domain.Enums;

public enum SupportMailStatus
{
    AwaitingResponse = 1, // Last message is from the requester ("Will Respond Soon")
    Responded = 2,        // Last message is from staff
    Closed = 3
}

public enum SupportMailSenderType
{
    Requester = 1,
    Staff = 2
}
