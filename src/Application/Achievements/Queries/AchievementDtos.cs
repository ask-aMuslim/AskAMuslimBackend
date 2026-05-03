namespace AskAMuslimBackend.Application.Achievements.Queries;

public class AchievementSummaryDto
{
    public GeneralAchievementStatsDto General { get; set; } = new();
    public List<AchievementYearStatsDto> Years { get; set; } = new();
}

public class GeneralAchievementStatsDto
{
    public int TotalNewMuslims { get; set; }
    public int TotalEvents { get; set; }
    public int TotalDialogues { get; set; }
}

public class AchievementYearStatsDto
{
    public int HijriYear { get; set; }
    public int NewMuslims { get; set; }
    public int Events { get; set; }
    public int Dialogues { get; set; }
}

public class AchievementYearDto
{
    public int HijriYear { get; set; }
    public AchievementYearStatsDto Totals { get; set; } = new();
    public List<AchievementMonthDto> Months { get; set; } = new();
}

public class AchievementMonthDto
{
    public int HijriMonth { get; set; }
    public int NewMuslims { get; set; }
    public int Dialogues { get; set; }
    public int CoursesCompleted { get; set; }
    public int ActiveVolunteers { get; set; }
    public int SocialMediaReach { get; set; }
    public int EventsHeld { get; set; }
}
