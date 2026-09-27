using System.Text.Json;
using System.Text.Json.Serialization;
using StudentResourceTracker.Models;

namespace StudentResourceTracker.Services;

/// <summary>
/// Thread-safe JSON-file persistence. All access goes through Read/Write so the
/// lock and the save-to-disk step can never be forgotten.
/// </summary>
public class DataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;
    private readonly object _lock = new();
    private DataFile _data;

    public DataStore(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        if (File.Exists(path))
        {
            _data = JsonSerializer.Deserialize<DataFile>(File.ReadAllText(path), JsonOptions) ?? new DataFile();
        }
        else
        {
            _data = SeedData.Create();
            Save();
        }
    }

    public T Read<T>(Func<DataFile, T> query)
    {
        lock (_lock) return query(_data);
    }

    public T Write<T>(Func<DataFile, T> mutation)
    {
        lock (_lock)
        {
            var result = mutation(_data);
            Save();
            return result;
        }
    }

    // Write to a temp file then swap, so a crash mid-write cannot corrupt the data file.
    private void Save()
    {
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(_data, JsonOptions));
        File.Move(tmp, _path, overwrite: true);
    }
}
