using System.ComponentModel.DataAnnotations;

namespace StudentResourceTracker.Models;

public enum ResourceType
{
    Article,
    Video,
    Document,
    Link,
    Note,
    Assignment,
    Other
}

public class Resource
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Url, StringLength(500)]
    public string? Url { get; set; }

    [Display(Name = "Resource Type")]
    public ResourceType Type { get; set; } = ResourceType.Other;

    [Display(Name = "Added On")]
    public DateTime AddedOn { get; set; } = DateTime.UtcNow;

    [Display(Name = "Is Completed")]
    public bool IsCompleted { get; set; } = false;

    // Foreign keys
    [Display(Name = "Student")]
    public int? StudentId { get; set; }

    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    // Navigation
    public Student? Student { get; set; }
    public Category? Category { get; set; }
}
