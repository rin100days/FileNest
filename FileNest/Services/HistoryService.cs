using System.IO;
using System.Text.Json;

namespace FileNest.Services;

public class MoveRecord
{
    public string OriginalPath { get; set; } = string.Empty;
    public string NewPath { get; set; } = string.Empty;
    public DateTime MovedAt { get; set; } = DateTime.Now;
}

public class MoveBatch
{
    public string BatchId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<MoveRecord> Records { get; set; } = new();
}

public class HistoryData
{
    public List<MoveBatch> Batches { get; set; } = new();
}

public class HistoryService
{
    public string HistoryPath => Path.Combine(SettingsService.AppFolder, "history.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public HistoryData Load()
    {
        Directory.CreateDirectory(SettingsService.AppFolder);

        if (!File.Exists(HistoryPath))
            return new HistoryData();

        try
        {
            var json = File.ReadAllText(HistoryPath);
            return JsonSerializer.Deserialize<HistoryData>(json, JsonOptions) ?? new HistoryData();
        }
        catch
        {
            return new HistoryData();
        }
    }

    public void Save(HistoryData data)
    {
        Directory.CreateDirectory(SettingsService.AppFolder);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(HistoryPath, json);
    }

    public void AddBatch(List<MoveRecord> records)
    {
        if (records.Count == 0)
            return;

        var data = Load();
        data.Batches.Add(new MoveBatch
        {
            CreatedAt = DateTime.Now,
            Records = records
        });
        Save(data);
    }

    public MoveBatch? GetLastBatch()
    {
        var data = Load();
        return data.Batches.LastOrDefault();
    }

    public void RemoveBatch(string batchId)
    {
        var data = Load();
        data.Batches.RemoveAll(batch => batch.BatchId == batchId);
        Save(data);
    }
}
