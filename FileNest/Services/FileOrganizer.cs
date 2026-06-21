using System.IO;
using FileNest.Models;

namespace FileNest.Services;

public class OrganizerResult
{
    public int MovedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Logs { get; set; } = new();
}

public class FileOrganizer
{
    private readonly HistoryService _historyService;
    private readonly string _logPath;

    public FileOrganizer(HistoryService historyService)
    {
        _historyService = historyService;
        _logPath = Path.Combine(SettingsService.AppFolder, "log.txt");
    }

    public OrganizerResult Organize(IEnumerable<FileItem> previewItems)
    {
        var result = new OrganizerResult();
        var records = new List<MoveRecord>();

        foreach (var item in previewItems)
        {
            try
            {
                if (!File.Exists(item.SourcePath))
                {
                    result.SkippedCount++;
                    result.Logs.Add($"건너뜀: 원본 없음 - {item.SourcePath}");
                    continue;
                }

                if (IsFileLocked(item.SourcePath))
                {
                    result.SkippedCount++;
                    result.Logs.Add($"건너뜀: 파일 사용 중 - {item.SourcePath}");
                    continue;
                }

                Directory.CreateDirectory(item.DestinationFolder);

                var destinationPath = GetUniquePath(Path.Combine(item.DestinationFolder, item.FileName));

                if (Path.GetFullPath(item.SourcePath).Equals(Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
                {
                    result.SkippedCount++;
                    result.Logs.Add($"건너뜀: 이미 정리된 위치 - {item.SourcePath}");
                    continue;
                }

                File.Move(item.SourcePath, destinationPath);

                records.Add(new MoveRecord
                {
                    OriginalPath = item.SourcePath,
                    NewPath = destinationPath,
                    MovedAt = DateTime.Now
                });

                result.MovedCount++;
                result.Logs.Add($"이동: {item.SourcePath} -> {destinationPath}");
            }
            catch (Exception ex)
            {
                result.SkippedCount++;
                result.Logs.Add($"오류: {item.SourcePath} / {ex.Message}");
            }
        }

        _historyService.AddBatch(records);
        WriteLog(result.Logs);
        return result;
    }

    public OrganizerResult UndoLastBatch()
    {
        var result = new OrganizerResult();
        var batch = _historyService.GetLastBatch();

        if (batch is null || batch.Records.Count == 0)
        {
            result.Logs.Add("되돌릴 이동 기록이 없습니다.");
            return result;
        }

        foreach (var record in batch.Records.AsEnumerable().Reverse())
        {
            try
            {
                if (!File.Exists(record.NewPath))
                {
                    result.SkippedCount++;
                    result.Logs.Add($"되돌리기 건너뜀: 이동된 파일 없음 - {record.NewPath}");
                    continue;
                }

                if (IsFileLocked(record.NewPath))
                {
                    result.SkippedCount++;
                    result.Logs.Add($"되돌리기 건너뜀: 파일 사용 중 - {record.NewPath}");
                    continue;
                }

                var originalFolder = Path.GetDirectoryName(record.OriginalPath);
                if (string.IsNullOrWhiteSpace(originalFolder))
                {
                    result.SkippedCount++;
                    result.Logs.Add($"되돌리기 건너뜀: 원래 폴더 확인 불가 - {record.OriginalPath}");
                    continue;
                }

                Directory.CreateDirectory(originalFolder);
                var restorePath = GetUniquePath(record.OriginalPath);
                File.Move(record.NewPath, restorePath);

                result.MovedCount++;
                result.Logs.Add($"되돌림: {record.NewPath} -> {restorePath}");
            }
            catch (Exception ex)
            {
                result.SkippedCount++;
                result.Logs.Add($"되돌리기 오류: {record.NewPath} / {ex.Message}");
            }
        }

        if (result.MovedCount > 0)
            _historyService.RemoveBatch(batch.BatchId);

        WriteLog(result.Logs);
        return result;
    }

    public static string ResolveDestinationFolder(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').Trim('/');
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return GetDownloadsFolder();

        var root = parts[0].ToLowerInvariant();
        var rest = parts.Skip(1).ToArray();

        string baseFolder = root switch
        {
            "documents" => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "pictures" => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "videos" => Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "music" => Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            "downloads" => GetDownloadsFolder(),
            _ => Path.Combine(GetDownloadsFolder(), "Others")
        };

        return rest.Length == 0 ? baseFolder : Path.Combine(new[] { baseFolder }.Concat(rest).ToArray());
    }

    public static string GetDownloadsFolder()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, "Downloads");
    }

    public static string GetUniquePath(string requestedPath)
    {
        if (!File.Exists(requestedPath) && !Directory.Exists(requestedPath))
            return requestedPath;

        var directory = Path.GetDirectoryName(requestedPath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(requestedPath);
        var extension = Path.GetExtension(requestedPath);
        var index = 1;

        string candidate;
        do
        {
            candidate = Path.Combine(directory, $"{fileNameWithoutExtension} ({index}){extension}");
            index++;
        } while (File.Exists(candidate) || Directory.Exists(candidate));

        return candidate;
    }

    private static bool IsFileLocked(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    private void WriteLog(IEnumerable<string> logs)
    {
        Directory.CreateDirectory(SettingsService.AppFolder);
        var lines = logs.Select(log => $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {log}");
        File.AppendAllLines(_logPath, lines);
    }
}
