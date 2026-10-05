using StudentResourceTracker.Models;
using StudentResourceTracker.Services;

namespace StudentResourceTracker.Tests;

public class ValidatorTests
{
    private static ResourceInput Resource(
        string? title = "Read chapter 1", string? url = null, string? notes = null,
        ResourceType type = ResourceType.Textbook, ResourceStatus status = ResourceStatus.NotStarted,
        Priority priority = Priority.Medium) =>
        new(Guid.NewGuid(), title, type, url, notes, status, priority, null, null);

    [Fact]
    public void Valid_resource_has_no_errors() => Assert.Empty(Validators.Resource(Resource()));

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Resource_title_is_required(string? title) =>
        Assert.Contains("title", Validators.Resource(Resource(title: title)).Keys);

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com/file")]
    [InlineData("not a url")]
    public void Resource_url_must_be_http_or_https(string url) =>
        Assert.Contains("url", Validators.Resource(Resource(url: url)).Keys);

    [Fact]
    public void Notes_length_is_measured_after_trimming() =>
        Assert.Empty(Validators.Resource(Resource(notes: new string('a', 2000) + "   ")));

    [Fact]
    public void Undefined_enum_values_are_rejected()
    {
        var errors = Validators.Resource(Resource(type: (ResourceType)99, status: (ResourceStatus)7, priority: (Priority)42));
        Assert.Contains("type", errors.Keys);
        Assert.Contains("status", errors.Keys);
        Assert.Contains("priority", errors.Keys);
        Assert.Contains("status", Validators.Status(new StatusInput((ResourceStatus)7)).Keys);
    }

    [Fact]
    public void Course_lecturer_and_semester_lengths_are_limited()
    {
        var errors = Validators.Course(new CourseInput("CS 1", "Intro", new string('a', 81), new string('b', 41)));
        Assert.Contains("lecturer", errors.Keys);
        Assert.Contains("semester", errors.Keys);
    }

    [Fact]
    public void CleanTags_skips_nulls_blanks_and_duplicates_and_normalises_case()
    {
        var tags = Validators.CleanTags(new[] { null, " Exam ", "exam", "", "  ", new string('x', 31), "SQL" });
        Assert.Equal(new[] { "exam", "sql" }, tags);
    }

    [Fact]
    public void CleanTags_keeps_at_most_ten() =>
        Assert.Equal(10, Validators.CleanTags(Enumerable.Range(0, 15).Select(i => $"t{i}")).Count);
}
