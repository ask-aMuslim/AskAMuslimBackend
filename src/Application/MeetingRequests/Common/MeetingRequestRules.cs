namespace AskAMuslimBackend.Application.MeetingRequests.Common;

public static class MeetingRequestRules
{
    /// <summary>A scholar has to review and accept a request, so a meeting can't be requested for sooner than this.</summary>
    public static readonly TimeSpan MinimumNotice = TimeSpan.FromHours(24);

    /// <summary>
    /// Clients check the notice period against their own clock a moment before the request arrives, so a slot
    /// exactly at the limit must not be refused for a few seconds, or a slightly fast or slow clock.
    /// </summary>
    public static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(5);

    /// <summary>The earliest start time a request created at <paramref name="now"/> may ask for.</summary>
    public static DateTime EarliestStartUtc(DateTimeOffset now) =>
        (now + MinimumNotice - ClockTolerance).UtcDateTime;
}
