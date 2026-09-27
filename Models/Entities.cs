namespace StudentResourceTracker.Models;

public enum ResourceType
{
    LectureNotes,
    Textbook,
    Video,
    Article,
    Assignment,
    PastQuestion,
    Link,
    Other
}

public enum ResourceStatus
{
    NotStarted,
    InProgress,
    Completed
}

public enum Priority
{
    Low,
    Medium,
    High
}

public class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Lecturer { get; set; }
    public string? Semester { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class StudyResource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public string Title { get; set; } = "";
    public ResourceType Type { get; set; } = ResourceType.Other;
    public string? Url { get; set; }
    public string? Notes { get; set; }
    public ResourceStatus Status { get; set; } = ResourceStatus.NotStarted;
    public Priority Priority { get; set; } = Priority.Medium;
    public DateOnly? DueDate { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

/// <summary>Root object persisted to disk.</summary>
public class DataFile
{
    public List<Course> Courses { get; set; } = new();
    public List<StudyResource> Resources { get; set; } = new();
}
