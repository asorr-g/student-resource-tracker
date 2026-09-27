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
        if (i.Notes is { Length: > 2000 }) e["notes"] = new[] { "Notes must be 2000 characters or fewer." };
        return e;
    }

    public static List<string> CleanTags(IEnumerable<string>? tags) =>
        (tags ?? Enumerable.Empty<string>())
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0 && t.Length <= 30)
            .Distinct().Take(10).ToList();
}
