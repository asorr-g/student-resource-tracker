using System.Text.Json;
using System.Text.Json.Serialization;
using StudentResourceTracker.Models;

namespace StudentResourceTracker.Services;

/// <summary>
/// Thread-safe JSON-file persistence. All access goes through Read/Write so the
/// lock and the save-to-disk step can never be forgotten.
/// A Write is all-or-nothing: if the mutation throws or the save fails, the in-memory
/// data is restored to the last saved state.
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
    private string _savedJson;

    public DataStore(string path, ILogger<DataStore>? logger = null)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            try
            {
                _data = JsonSerializer.Deserialize<DataFile>(json, JsonOptions) ?? new DataFile();
                _savedJson = Serialize(_data);
                return;
            }
            catch (JsonException ex)
            {
                // Keep the unreadable file so nothing is lost, then start with an empty tracker.
                var backup = Path.ChangeExtension(path, $".corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                File.Move(path, backup);
                logger?.LogError(ex, "Data file {Path} could not be read. It was moved to {Backup} and an empty tracker was started.", path, backup);
                _data = new DataFile();
            }
        }
        else
        {
            _data = SeedData.Create();
        }

        _savedJson = Serialize(_data);
        WriteFile(_savedJson);
    }

    public T Read<T>(Func<DataFile, T> query)
    {
        lock (_lock) return query(_data);
    }

    public T Write<T>(Func<DataFile, T> mutation)
    {
        lock (_lock)
        {
            try
            {
                var result = mutation(_data);
                var json = Serialize(_data);
                if (json != _savedJson)
                {
                    WriteFile(json);
                    _savedJson = json;
                }
                return result;
            }
            catch
            {
                _data = JsonSerializer.Deserialize<DataFile>(_savedJson, JsonOptions)!;
                throw;
            }
        }
    }

    private static string Serialize(DataFile data) => JsonSerializer.Serialize(data, JsonOptions);

    // Write to a temp file then swap, so a crash mid-write cannot corrupt the data file.
    private void WriteFile(string json)
    {
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, _path, overwrite: true);
    }
}
