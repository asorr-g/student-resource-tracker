using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StudentResourceTracker.Tests;

public sealed class ApiTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "srt-tests-" + Guid.NewGuid().ToString("N"));
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiTests()
    {
        var dataPath = Path.Combine(_dir, "tracker.json");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseSetting("DataFile", dataPath));
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private async Task<JsonElement> GetJson(string path) =>
        JsonDocument.Parse(await _client.GetStringAsync(path)).RootElement;

    private async Task<(string CourseId, JsonElement Resource)> FirstResource()
    {
        var resources = await GetJson("/api/resources");
        var r = resources[0];
        return (r.GetProperty("courseId").GetString()!, r);
    }

    [Fact]
    public async Task Seed_data_is_served()
    {
        Assert.Equal(3, (await GetJson("/api/courses")).GetArrayLength());
        Assert.Equal(8, (await GetJson("/api/resources")).GetArrayLength());
    }

    [Fact]
    public async Task Numeric_enum_values_are_rejected()
    {
        var (courseId, _) = await FirstResource();
        var res = await _client.PostAsync("/api/resources",
            Json($$"""{"courseId":"{{courseId}}","title":"x","type":99,"status":7,"priority":42}"""));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal(8, (await GetJson("/api/resources")).GetArrayLength());
    }

    [Fact]
    public async Task Null_tags_are_ignored_instead_of_crashing()
    {
        var (courseId, r) = await FirstResource();
        var id = r.GetProperty("id").GetString();
        var res = await _client.PutAsync($"/api/resources/{id}",
            Json($$"""{"courseId":"{{courseId}}","title":"Updated","type":"Video","status":"NotStarted","priority":"Low","tags":[null," Exam "]}"""));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Updated", body.GetProperty("title").GetString());
        Assert.Equal("exam", body.GetProperty("tags")[0].GetString());
    }

    [Fact]
    public async Task Invalid_course_on_update_leaves_resource_unchanged()
    {
        var (_, r) = await FirstResource();
        var id = r.GetProperty("id").GetString();
        var res = await _client.PutAsync($"/api/resources/{id}",
            Json($$"""{"courseId":"{{Guid.NewGuid()}}","title":"Changed","type":"Video","status":"NotStarted","priority":"Low"}"""));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var after = (await GetJson("/api/resources")).EnumerateArray().Single(x => x.GetProperty("id").GetString() == id);
        Assert.Equal(r.GetProperty("title").GetString(), after.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Long_lecturer_is_rejected()
    {
        var res = await _client.PostAsJsonAsync("/api/courses", new { code = "L 1", title = "t", lecturer = new string('a', 500) });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Duplicate_course_code_conflicts()
    {
        var res = await _client.PostAsJsonAsync("/api/courses", new { code = "cs 301", title = "Duplicate" });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Status_patch_sets_and_clears_completed_at()
    {
        var (_, r) = await FirstResource();
        var id = r.GetProperty("id").GetString();

        var done = await (await _client.PatchAsJsonAsync($"/api/resources/{id}/status", new { status = "Completed" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(JsonValueKind.Null, done.GetProperty("completedAt").ValueKind);

        var reopened = await (await _client.PatchAsJsonAsync($"/api/resources/{id}/status", new { status = "InProgress" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, reopened.GetProperty("completedAt").ValueKind);
    }

    [Fact]
    public async Task Deleting_a_course_deletes_its_resources()
    {
        var courses = await GetJson("/api/courses");
        var course = courses[0];
        var id = course.GetProperty("id").GetString();

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/courses/{id}")).StatusCode);
        var remaining = await GetJson("/api/resources");
        Assert.DoesNotContain(remaining.EnumerateArray(), r => r.GetProperty("courseId").GetString() == id);
        Assert.Equal(8 - course.GetProperty("resourceCount").GetInt32(), remaining.GetArrayLength());
    }

    [Fact]
    public async Task Search_and_overdue_filters_work()
    {
        Assert.Equal(1, (await GetJson("/api/resources?q=UML")).GetArrayLength());
        var overdue = await GetJson("/api/resources?overdue=true");
        Assert.All(overdue.EnumerateArray(), r => Assert.True(r.GetProperty("isOverdue").GetBoolean()));
        Assert.Equal(1, overdue.GetArrayLength());
    }

    [Fact]
    public async Task Summary_separates_overdue_from_upcoming()
    {
        var s = await GetJson("/api/analytics/summary");
        Assert.Equal(1, s.GetProperty("overdue").GetInt32());
        Assert.Equal(1, s.GetProperty("overdueItems").GetArrayLength());
        Assert.All(s.GetProperty("upcoming").EnumerateArray(), u => Assert.True(u.GetProperty("daysLeft").GetInt32() >= 0));
    }

    [Fact]
    public async Task Csv_export_escapes_formulas()
    {
        var (courseId, _) = await FirstResource();
        await _client.PostAsJsonAsync("/api/resources", new
        {
            courseId, title = "=HYPERLINK(\"http://evil\")", type = "Link", status = "NotStarted", priority = "Low"
        });

        var res = await _client.GetAsync("/api/export/csv");
        Assert.Equal("text/csv", res.Content.Headers.ContentType?.MediaType);
        var csv = await res.Content.ReadAsStringAsync();
        Assert.Contains("\"'=HYPERLINK(\"\"http://evil\"\")\"", csv);
    }

    [Fact]
    public async Task Unknown_resource_returns_404()
    {
        var res = await _client.DeleteAsync($"/api/resources/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
