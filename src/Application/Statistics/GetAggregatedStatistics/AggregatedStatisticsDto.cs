namespace AskAMuslimBackend.Application.Statistics.GetAggregatedStatistics;

public class AggregatedStatisticsDto
{
    public UserStatistics Users { get; set; } = new();
    public CourseStatistics Courses { get; set; } = new();
    public QATranslationStatistics QATranslation { get; set; } = new();
}

public class UserStatistics
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
    public int NewThisMonth { get; set; }
    public LastLoginStatistics LastLogin { get; set; } = new();
    public UserRoleStatistics ByRole { get; set; } = new();
}

public class LastLoginStatistics
{
    public int Today { get; set; }
    public int Last7Days { get; set; }
    public int Last30Days { get; set; }
}

public class UserRoleStatistics
{
    public int Admins { get; set; }
    public int Instructors { get; set; }
    public int Preachers { get; set; }
    public int Students { get; set; }
    public int NonMuslim { get; set; }
}

public class CourseStatistics
{
    public int Total { get; set; }
    public int Published { get; set; }
    public Dictionary<string, int> ByLevel { get; set; } = new();
}

public class QATranslationStatistics
{
    public int English { get; set; }
    public int Arabic { get; set; }
    public int French { get; set; }
    public int Spanish { get; set; }
    public int German { get; set; }
}
