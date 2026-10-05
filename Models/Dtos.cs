namespace StudentResourceTracker.Models;

// ---- Request bodies ----
public record CourseInput(string? Code, string? Title, string? Lecturer, string? Semester);

public record ResourceInput(
    Guid CourseId,
    string? Title,
    ResourceType Type,
    string? Url,
    string? Notes,
    ResourceStatus Status,
    Priority Priority,
    DateOnly? DueDate,
    List<string?>? Tags);

public record StatusInput(ResourceStatus Status);

// ---- Responses ----
public record CourseDto(Guid Id, string Code, string Title, string? Lecturer, string? Semester, int ResourceCount);

public record ResourceDto(
    Guid Id,
    Guid CourseId,
    string CourseCode,
    string CourseTitle,
    string Title,
    ResourceType Type,
    string? Url,
    string? Notes,
    ResourceStatus Status,
    Priority Priority,
    DateOnly? DueDate,
    List<string> Tags,
    bool IsOverdue,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public record CourseProgress(string CourseCode, string CourseTitle, int Total, int Completed, double CompletionRate);

public record CountItem(string Label, int Count);

public record UpcomingItem(Guid Id, string Title, string CourseCode, DateOnly DueDate, int DaysLeft, Priority Priority, ResourceStatus Status);

public record SummaryDto(
    int TotalCourses,
    int TotalResources,
    int Completed,
    int InProgress,
    int NotStarted,
    double CompletionRate,
    int Overdue,
    int DueWithin7Days,
    List<CourseProgress> ByCourse,
    List<CountItem> ByType,
    List<CountItem> OpenByPriority,
    List<UpcomingItem> Upcoming,
    List<UpcomingItem> OverdueItems);
