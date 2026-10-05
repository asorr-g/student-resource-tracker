using System.ComponentModel.DataAnnotations;

namespace StudentResourceTracker.Models;

public class Category
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Category Name")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Navigation
    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
}
