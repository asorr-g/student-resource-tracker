using System.ComponentModel.DataAnnotations;

namespace StudentResourceTracker.Models;

public class Student
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Student ID")]
    public string? StudentNumber { get; set; }

    [StringLength(200)]
    public string? Course { get; set; }

    [Display(Name = "Enrolled On")]
    public DateTime EnrolledOn { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Resource> Resources { get; set; } = new List<Resource>();

    // Computed
    public string FullName => $"{FirstName} {LastName}";
}
