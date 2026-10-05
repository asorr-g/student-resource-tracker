using StudentResourceTracker.Models;
using StudentResourceTracker.Services;

namespace StudentResourceTracker.Tests;

public sealed class AnalyticsServiceTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 10, 4);
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "srt-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private AnalyticsService ServiceWith(Action<DataFile, Course> setup)
    {
        var store = new DataStore(Path.Combine(_dir, "tracker.json"));
        store.Write(d =>
        {
            d.Courses.Clear();
            d.Resources.Clear();
            var course = new Course { Code = "CS 1", Title = "Intro" };
            d.Courses.Add(course);
            setup(d, course);
            return 0;
        });
        return new AnalyticsService(store);
    }

    private static StudyResource Item(Course c, int? dueInDays, ResourceStatus status = ResourceStatus.NotStarted) => new()
    {
        CourseId = c.Id, Title = $"due {dueInDays}", Status = status,
        DueDate = dueInDays is null ? null : Today.AddDays(dueInDays.Value)
    };

    [Fact]
    public void Due_within_7_days_counts_today_through_six_days_ahead()
    {
        var s = ServiceWith((d, c) => d.Resources.AddRange(new[] { Item(c, 0), Item(c, 6), Item(c, 7), Item(c, -1) }))
            .BuildSummary(Today);

        Assert.Equal(2, s.DueWithin7Days);
        Assert.Equal(1, s.Overdue);
    }

    [Fact]
    public void Overdue_items_do_not_crowd_out_upcoming_deadlines()
    {
        var s = ServiceWith((d, c) =>
        {
            for (var i = 1; i <= 10; i++) d.Resources.Add(Item(c, -i));
            d.Resources.Add(Item(c, 2));
        }).BuildSummary(Today);

        var upcoming = Assert.Single(s.Upcoming);
        Assert.Equal(2, upcoming.DaysLeft);
        Assert.Equal(8, s.OverdueItems.Count);
        Assert.Equal(-10, s.OverdueItems[0].DaysLeft);
        Assert.Equal(10, s.Overdue);
    }

    [Fact]
    public void Completed_items_are_excluded_from_deadlines_and_counted_in_completion_rate()
    {
        var s = ServiceWith((d, c) => d.Resources.AddRange(new[]
        {
            Item(c, -3, ResourceStatus.Completed), Item(c, 1, ResourceStatus.Completed), Item(c, 1), Item(c, null)
        })).BuildSummary(Today);

        Assert.Equal(50, s.CompletionRate);
        Assert.Equal(0, s.Overdue);
        Assert.Single(s.Upcoming);
        Assert.Equal(50, Assert.Single(s.ByCourse).CompletionRate);
    }
}
