using StudentResourceTracker.Models;

namespace StudentResourceTracker.Services;

public static class Mapping
{
    public static ResourceDto ToDto(StudyResource r, DataFile d, DateOnly today)
    {
        var course = d.Courses.FirstOrDefault(c => c.Id == r.CourseId);
        return new ResourceDto(
            r.Id, r.CourseId, course?.Code ?? "?", course?.Title ?? "Unknown course",
            r.Title, r.Type, r.Url, r.Notes, r.Status, r.Priority, r.DueDate, r.Tags,
            r.Status != ResourceStatus.Completed && r.DueDate < today,
            r.CreatedAt, r.CompletedAt);
    }

    public static void ApplyStatus(StudyResource r, ResourceStatus status)
    {
        r.Status = status;
        r.CompletedAt = status == ResourceStatus.Completed ? (r.CompletedAt ?? DateTime.UtcNow) : null;
    }
}
