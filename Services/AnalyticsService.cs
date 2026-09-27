using StudentResourceTracker.Models;

namespace StudentResourceTracker.Services;

/// <summary>Builds the dashboard/report figures from the raw data.</summary>
public class AnalyticsService
{
    private readonly DataStore _store;
    public AnalyticsService(DataStore store) => _store = store;

    public SummaryDto BuildSummary()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return _store.Read(d =>
        {
            var total = d.Resources.Count;
            var completed = d.Resources.Count(r => r.Status == ResourceStatus.Completed);
            var inProgress = d.Resources.Count(r => r.Status == ResourceStatus.InProgress);
            var notStarted = d.Resources.Count(r => r.Status == ResourceStatus.NotStarted);
            var open = d.Resources.Where(r => r.Status != ResourceStatus.Completed).ToList();

            var byCourse = d.Courses.OrderBy(c => c.Code).Select(c =>
            {
                var rs = d.Resources.Where(r => r.CourseId == c.Id).ToList();
                var done = rs.Count(r => r.Status == ResourceStatus.Completed);
                return new CourseProgress(c.Code, c.Title, rs.Count, done, rs.Count == 0 ? 0 : Math.Round(100.0 * done / rs.Count, 1));
            }).ToList();

            var byType = d.Resources.GroupBy(r => r.Type)
                .OrderByDescending(g => g.Count())
                .Select(g => new CountItem(g.Key.ToString(), g.Count())).ToList();

            var byPriority = Enum.GetValues<Priority>().Reverse()
                .Select(p => new CountItem(p.ToString(), open.Count(r => r.Priority == p))).ToList();

            var upcoming = open.Where(r => r.DueDate.HasValue)
                .OrderBy(r => r.DueDate)
                .Take(8)
                .Select(r => new UpcomingItem(
                    r.Id, r.Title,
                    d.Courses.FirstOrDefault(c => c.Id == r.CourseId)?.Code ?? "?",
                    r.DueDate!.Value,
                    r.DueDate.Value.DayNumber - today.DayNumber,
                    r.Priority, r.Status)).ToList();

            return new SummaryDto(
                d.Courses.Count, total, completed, inProgress, notStarted,
                total == 0 ? 0 : Math.Round(100.0 * completed / total, 1),
                open.Count(r => r.DueDate < today),
                open.Count(r => r.DueDate >= today && r.DueDate <= today.AddDays(7)),
                byCourse, byType, byPriority, upcoming);
        });
    }
}
