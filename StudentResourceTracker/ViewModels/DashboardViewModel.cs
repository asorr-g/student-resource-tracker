using StudentResourceTracker.Models;

namespace StudentResourceTracker.ViewModels;

public class DashboardViewModel
{
    public int TotalStudents { get; set; }
    public int TotalResources { get; set; }
    public int CompletedResources { get; set; }
    public int TotalCategories { get; set; }

    public IEnumerable<Resource> RecentResources { get; set; } = new List<Resource>();
    public IEnumerable<Student> RecentStudents { get; set; } = new List<Student>();

    // Percentage complete
    public int CompletionPercent =>
        TotalResources == 0 ? 0 : (int)Math.Round(CompletedResources * 100.0 / TotalResources);
}
