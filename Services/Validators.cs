using StudentResourceTracker.Models;

namespace StudentResourceTracker.Services;

public static class Validators
{
    public static Dictionary<string, string[]> Course(CourseInput i)
    {
        var e = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(i.Code)) e["code"] = new[] { "Course code is required." };
        else if (i.Code.Trim().Length > 20) e["code"] = new[] { "Course code must be 20 characters or fewer." };
        if (string.IsNullOrWhiteSpace(i.Title)) e["title"] = new[] { "Course title is required." };
        else if (i.Title.Trim().Length > 120) e["title"] = new[] { "Course title must be 120 characters or fewer." };
        if (i.Lecturer?.Trim().Length > 80) e["lecturer"] = new[] { "Lecturer must be 80 characters or fewer." };
        if (i.Semester?.Trim().Length > 40) e["semester"] = new[] { "Semester must be 40 characters or fewer." };
        return e;
    }

    public static Dictionary<string, string[]> Resource(ResourceInput i)
    {
        var e = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(i.Title)) e["title"] = new[] { "Title is required." };
        else if (i.Title.Trim().Length > 200) e["title"] = new[] { "Title must be 200 characters or fewer." };
        if (!string.IsNullOrWhiteSpace(i.Url) &&
            !(Uri.TryCreate(i.Url.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            e["url"] = new[] { "URL must be a valid http:// or https:// address." };
        if (i.Notes?.Trim().Length > 2000) e["notes"] = new[] { "Notes must be 2000 characters or fewer." };
        if (!Enum.IsDefined(i.Type)) e["type"] = new[] { "Please choose a valid type." };
        if (!Enum.IsDefined(i.Status)) e["status"] = new[] { "Please choose a valid status." };
        if (!Enum.IsDefined(i.Priority)) e["priority"] = new[] { "Please choose a valid priority." };
        return e;
    }

    public static Dictionary<string, string[]> Status(StatusInput i)
    {
        var e = new Dictionary<string, string[]>();
        if (!Enum.IsDefined(i.Status)) e["status"] = new[] { "Please choose a valid status." };
        return e;
    }

    public static List<string> CleanTags(IEnumerable<string?>? tags) =>
        (tags ?? Enumerable.Empty<string?>())
            .Where(t => t is not null)
            .Select(t => t!.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0 && t.Length <= 30)
            .Distinct().Take(10).ToList();
}
