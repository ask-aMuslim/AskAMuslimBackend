namespace AskAMuslimBackend.Domain.Entities;

public class Achievement : BaseAuditableEntity
{
    public int HijriYear { get; set; }
    public int HijriMonth { get; set; }
    public int NewMuslims { get; set; }
    public int Dialogues { get; set; }
    public int CoursesCompleted { get; set; }
    public int ActiveVolunteers { get; set; }
    public int SocialMediaReach { get; set; }
    public int EventsHeld { get; set; }
}
