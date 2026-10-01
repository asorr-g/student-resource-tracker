using System.Text.RegularExpressions;
using StudentResourceTracker.Models;

namespace StudentResourceTracker.Components;

public static class Labels
{
    public static string Pretty(string value) =>
        Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");

    public static string Pretty(Enum value) => Pretty(value.ToString());

    public static string Date(DateOnly? date) =>
        date is null ? "—" : date.Value.ToString("d MMM yyyy");

    public static string TypeColor(ResourceType type) => type switch
    {
        ResourceType.LectureNotes => "#0f766e",
        ResourceType.Textbook => "#7c3aed",
        ResourceType.Video => "#db2777",
        ResourceType.Article => "#0369a1",
        ResourceType.Assignment => "#c2410c",
        ResourceType.PastQuestion => "#b45309",
        ResourceType.Link => "#15803d",
        _ => "#57534e"
    };
}
