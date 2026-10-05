using StudentResourceTracker.Models;

namespace StudentResourceTracker.Services;

/// <summary>Sample data created on first run so the dashboard isn't empty. Delete Data/tracker.json to reset.</summary>
public static class SeedData
{
    public static DataFile Create()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var db = new Course { Code = "CS 301", Title = "Database Systems", Lecturer = "Dr. Mensah", Semester = "Semester 1" };
        var se = new Course { Code = "CS 305", Title = "Software Engineering", Lecturer = "Dr. Owusu", Semester = "Semester 1" };
        var st = new Course { Code = "STAT 201", Title = "Applied Statistics", Lecturer = "Prof. Addo", Semester = "Semester 1" };

        StudyResource R(Course c, string title, ResourceType t, ResourceStatus s, Priority p, int? dueInDays, params string[] tags) => new()
        {
            CourseId = c.Id, Title = title, Type = t, Status = s, Priority = p,
            DueDate = dueInDays is null ? null : today.AddDays(dueInDays.Value),
            Tags = tags.ToList(),
            CompletedAt = s == ResourceStatus.Completed ? DateTime.UtcNow : null
        };

        return new DataFile
        {
            Courses = { db, se, st },
            Resources =
            {
                R(db, "Week 3 lecture notes: Normalization", ResourceType.LectureNotes, ResourceStatus.Completed, Priority.Medium, null, "sql", "theory"),
                R(db, "Assignment 2: ER diagram + schema", ResourceType.Assignment, ResourceStatus.InProgress, Priority.High, 3, "assignment"),
                R(db, "Database System Concepts (Ch. 1-6)", ResourceType.Textbook, ResourceStatus.InProgress, Priority.Medium, 14, "reading"),
                R(db, "Past questions 2022-2024", ResourceType.PastQuestion, ResourceStatus.NotStarted, Priority.High, 21, "exam"),
                R(se, "UML tutorial video series", ResourceType.Video, ResourceStatus.NotStarted, Priority.Low, 10, "uml"),
                R(se, "Group project proposal", ResourceType.Assignment, ResourceStatus.NotStarted, Priority.High, -2, "group"),
                R(st, "Hypothesis testing cheat sheet", ResourceType.Article, ResourceStatus.Completed, Priority.Low, null, "revision"),
                R(st, "Problem set 4", ResourceType.Assignment, ResourceStatus.NotStarted, Priority.Medium, 5, "assignment")
            }
        };
    }
}
