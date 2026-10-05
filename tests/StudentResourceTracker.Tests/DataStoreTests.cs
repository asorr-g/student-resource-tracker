using StudentResourceTracker.Models;
using StudentResourceTracker.Services;

namespace StudentResourceTracker.Tests;

public sealed class DataStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "srt-tests-" + Guid.NewGuid().ToString("N"));
    private string DataPath => Path.Combine(_dir, "tracker.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Creates_seed_data_when_file_is_missing()
    {
        var store = new DataStore(DataPath);
        Assert.True(File.Exists(DataPath));
        Assert.NotEmpty(store.Read(d => d.Courses));
    }

    [Fact]
    public void Writes_persist_across_instances()
    {
        new DataStore(DataPath).Write(d => { d.Courses.Add(new Course { Code = "NEW 1", Title = "New" }); return 0; });
        var reloaded = new DataStore(DataPath);
        Assert.Contains(reloaded.Read(d => d.Courses), c => c.Code == "NEW 1");
    }

    [Fact]
    public void Failed_write_rolls_back_memory_and_is_not_saved_by_later_writes()
    {
        var store = new DataStore(DataPath);
        var original = store.Read(d => d.Resources[0].Title);

        Assert.Throws<InvalidOperationException>(() => store.Write<int>(d =>
        {
            d.Resources[0].Title = "PARTIAL";
            throw new InvalidOperationException();
        }));

        Assert.Equal(original, store.Read(d => d.Resources[0].Title));
        store.Write(d => { d.Courses.Add(new Course { Code = "X 1", Title = "X" }); return 0; });
        Assert.DoesNotContain("PARTIAL", File.ReadAllText(DataPath));
    }

    [Fact]
    public void Write_that_changes_nothing_does_not_touch_the_file()
    {
        var store = new DataStore(DataPath);
        var before = File.GetLastWriteTimeUtc(DataPath);
        File.SetLastWriteTimeUtc(DataPath, before.AddHours(-1));
        var stamped = File.GetLastWriteTimeUtc(DataPath);

        store.Write(d => d.Resources.RemoveAll(r => r.Id == Guid.Empty));

        Assert.Equal(stamped, File.GetLastWriteTimeUtc(DataPath));
    }

    [Fact]
    public void Corrupt_file_is_backed_up_and_an_empty_tracker_is_started()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(DataPath, "{ this is not json");

        var store = new DataStore(DataPath);

        Assert.Empty(store.Read(d => d.Courses));
        var backup = Assert.Single(Directory.GetFiles(_dir, "tracker.corrupt-*.json"));
        Assert.Equal("{ this is not json", File.ReadAllText(backup));
    }
}
