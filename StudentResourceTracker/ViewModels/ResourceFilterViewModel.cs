using Microsoft.AspNetCore.Mvc.Rendering;
using StudentResourceTracker.Models;

namespace StudentResourceTracker.ViewModels;

public class ResourceFilterViewModel
{
    public IEnumerable<Resource> Resources { get; set; } = new List<Resource>();

    // Filter inputs
    public int? SelectedStudentId { get; set; }
    public int? SelectedCategoryId { get; set; }
    public string? SearchTerm { get; set; }
    public bool? ShowCompleted { get; set; }

    // Dropdown data
    public SelectList? Students { get; set; }
    public SelectList? Categories { get; set; }
}
