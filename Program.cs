using System.Text;
using System.Text.Json.Serialization;
using StudentResourceTracker.Models;
using StudentResourceTracker.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// Data file location can be overridden: dotnet run --DataFile=/path/to/tracker.json
var dataPath = builder.Configuration["DataFile"]
               ?? Path.Combine(builder.Environment.ContentRootPath, "Data", "tracker.json");
builder.Services.AddSingleton(new DataStore(dataPath));
builder.Services.AddSingleton<AnalyticsService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api");

// ---------- Mapping helper ----------
static ResourceDto ToDto(StudyResource r, DataFile d, DateOnly today)
{
    var course = d.Courses.FirstOrDefault(c => c.Id == r.CourseId);
    return new ResourceDto(
        r.Id, r.CourseId, course?.Code ?? "?", course?.Title ?? "Unknown course",
        r.Title, r.Type, r.Url, r.Notes, r.Status, r.Priority, r.DueDate, r.Tags,
        r.Status != ResourceStatus.Completed && r.DueDate < today,
        r.CreatedAt, r.CompletedAt);
}

static void ApplyStatus(StudyResource r, ResourceStatus status)
{
    r.Status = status;
    r.CompletedAt = status == ResourceStatus.Completed ? (r.CompletedAt ?? DateTime.UtcNow) : null;
}

// =====================================================================
// COURSES
// =====================================================================
api.MapGet("/courses", (DataStore s) =>
    s.Read(d => d.Courses
        .OrderBy(c => c.Code)
        .Select(c => new CourseDto(c.Id, c.Code, c.Title, c.Lecturer, c.Semester,
            d.Resources.Count(r => r.CourseId == c.Id)))
        .ToList()));

api.MapPost("/courses", (CourseInput input, DataStore s) =>
{
    var errors = Validators.Course(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    return s.Write<IResult>(d =>
    {
        var code = input.Code!.Trim();
        if (d.Courses.Any(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            return Results.Conflict(new { error = $"A course with code '{code}' already exists." });

        var course = new Course
        {
            Code = code,
            Title = input.Title!.Trim(),
            Lecturer = input.Lecturer?.Trim(),
            Semester = input.Semester?.Trim()
        };
        d.Courses.Add(course);
        return Results.Created($"/api/courses/{course.Id}",
            new CourseDto(course.Id, course.Code, course.Title, course.Lecturer, course.Semester, 0));
    });
});

api.MapPut("/courses/{id:guid}", (Guid id, CourseInput input, DataStore s) =>
{
    var errors = Validators.Course(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    return s.Write<IResult>(d =>
    {
        var course = d.Courses.FirstOrDefault(c => c.Id == id);
        if (course is null) return Results.NotFound();

        var code = input.Code!.Trim();
        if (d.Courses.Any(c => c.Id != id && c.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            return Results.Conflict(new { error = $"A course with code '{code}' already exists." });

        course.Code = code;
        course.Title = input.Title!.Trim();
        course.Lecturer = input.Lecturer?.Trim();
        course.Semester = input.Semester?.Trim();
        return Results.Ok(new CourseDto(course.Id, course.Code, course.Title, course.Lecturer, course.Semester,
            d.Resources.Count(r => r.CourseId == id)));
    });
});

// Deleting a course also deletes its resources.
api.MapDelete("/courses/{id:guid}", (Guid id, DataStore s) =>
    s.Write<IResult>(d =>
    {
        var course = d.Courses.FirstOrDefault(c => c.Id == id);
        if (course is null) return Results.NotFound();
        d.Resources.RemoveAll(r => r.CourseId == id);
        d.Courses.Remove(course);
        return Results.NoContent();
    }));

// =====================================================================
// RESOURCES
// =====================================================================
api.MapGet("/resources", (
    DataStore s,
    Guid? courseId, ResourceType? type, ResourceStatus? status, Priority? priority,
    string? q, bool? overdue) =>
{
    var today = DateOnly.FromDateTime(DateTime.Today);
    return s.Read(d =>
    {
        IEnumerable<StudyResource> query = d.Resources;
        if (courseId is not null) query = query.Where(r => r.CourseId == courseId);
        if (type is not null) query = query.Where(r => r.Type == type);
        if (status is not null) query = query.Where(r => r.Status == status);
        if (priority is not null) query = query.Where(r => r.Priority == priority);
        if (overdue == true) query = query.Where(r => r.Status != ResourceStatus.Completed && r.DueDate < today);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(r =>
                r.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (r.Notes?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                r.Tags.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        // Open items first, then by due date (undated last), then title.
        return query
            .OrderBy(r => r.Status == ResourceStatus.Completed)
            .ThenBy(r => r.DueDate ?? DateOnly.MaxValue)
            .ThenBy(r => r.Title)
            .Select(r => ToDto(r, d, today))
            .ToList();
    });
});

api.MapPost("/resources", (ResourceInput input, DataStore s) =>
{
    var errors = Validators.Resource(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var today = DateOnly.FromDateTime(DateTime.Today);
    return s.Write<IResult>(d =>
    {
        if (!d.Courses.Any(c => c.Id == input.CourseId))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["courseId"] = new[] { "Please choose a valid course." } });

        var r = new StudyResource
        {
            CourseId = input.CourseId,
            Title = input.Title!.Trim(),
            Type = input.Type,
            Url = string.IsNullOrWhiteSpace(input.Url) ? null : input.Url.Trim(),
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
            Priority = input.Priority,
            DueDate = input.DueDate,
            Tags = Validators.CleanTags(input.Tags)
        };
        ApplyStatus(r, input.Status);
        d.Resources.Add(r);
        return Results.Created($"/api/resources/{r.Id}", ToDto(r, d, today));
    });
});

api.MapPut("/resources/{id:guid}", (Guid id, ResourceInput input, DataStore s) =>
{
    var errors = Validators.Resource(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var today = DateOnly.FromDateTime(DateTime.Today);
    return s.Write<IResult>(d =>
    {
        var r = d.Resources.FirstOrDefault(x => x.Id == id);
        if (r is null) return Results.NotFound();
        if (!d.Courses.Any(c => c.Id == input.CourseId))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["courseId"] = new[] { "Please choose a valid course." } });

        r.CourseId = input.CourseId;
        r.Title = input.Title!.Trim();
        r.Type = input.Type;
        r.Url = string.IsNullOrWhiteSpace(input.Url) ? null : input.Url.Trim();
        r.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
        r.Priority = input.Priority;
        r.DueDate = input.DueDate;
        r.Tags = Validators.CleanTags(input.Tags);
        ApplyStatus(r, input.Status);
        return Results.Ok(ToDto(r, d, today));
    });
});

// Quick status change from the table without opening the edit form.
api.MapPatch("/resources/{id:guid}/status", (Guid id, StatusInput input, DataStore s) =>
{
    var today = DateOnly.FromDateTime(DateTime.Today);
    return s.Write<IResult>(d =>
    {
        var r = d.Resources.FirstOrDefault(x => x.Id == id);
        if (r is null) return Results.NotFound();
        ApplyStatus(r, input.Status);
        return Results.Ok(ToDto(r, d, today));
    });
});

api.MapDelete("/resources/{id:guid}", (Guid id, DataStore s) =>
    s.Write<IResult>(d => d.Resources.RemoveAll(r => r.Id == id) > 0 ? Results.NoContent() : Results.NotFound()));

// =====================================================================
// ANALYTICS + EXPORT
// =====================================================================
api.MapGet("/analytics/summary", (AnalyticsService a) => a.BuildSummary());

api.MapGet("/export/csv", (DataStore s) =>
{
    var today = DateOnly.FromDateTime(DateTime.Today);
    var rows = s.Read(d => d.Resources
        .OrderBy(r => d.Courses.FirstOrDefault(c => c.Id == r.CourseId)?.Code)
        .ThenBy(r => r.DueDate ?? DateOnly.MaxValue)
        .Select(r => ToDto(r, d, today)).ToList());

    // Quote every field, and neutralise cells that spreadsheets would treat as formulas.
    static string Csv(string? v)
    {
        v ??= "";
        if (v.Length > 0 && "=+-@\t\r".Contains(v[0])) v = "'" + v;
        return "\"" + v.Replace("\"", "\"\"") + "\"";
    }

    var sb = new StringBuilder();
    sb.AppendLine("Course,Title,Type,Status,Priority,DueDate,Overdue,Tags,Url,Notes");
    foreach (var r in rows)
        sb.AppendLine(string.Join(",", new[]
        {
            Csv(r.CourseCode), Csv(r.Title), Csv(r.Type.ToString()), Csv(r.Status.ToString()),
            Csv(r.Priority.ToString()), Csv(r.DueDate?.ToString("yyyy-MM-dd")), Csv(r.IsOverdue ? "Yes" : "No"),
            Csv(string.Join("; ", r.Tags)), Csv(r.Url), Csv(r.Notes)
        }));

    // UTF-8 with BOM so Excel opens it correctly.
    var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    return Results.File(bytes, "text/csv", $"academic-resources-{today:yyyyMMdd}.csv");
});

app.Run();
