using System.IO;
using FileNest.Models;

namespace FileNest.Services;

public class FileScanner
{
    public List<FileItem> Scan(IEnumerable<string> folders, IEnumerable<Rule> rules)
    {
        var result = new List<FileItem>();
        var enabledRules = rules.Where(rule => rule.Enabled).ToList();
        var reservedDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in folders.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(folder))
                continue;

            if (!Directory.Exists(folder))
                continue;

            if (!IsSafeTargetFolder(folder))
                continue;

            foreach (var filePath in EnumerateFilesSafely(folder))
            {
                try
                {
                    var match = MatchRule(filePath, enabledRules);
                    if (match is null)
                        continue;

                    var destinationFolder = FileOrganizer.ResolveDestinationFolder(match.Rule.DestinationRelativePath);
                    var currentFolder = Path.GetDirectoryName(filePath) ?? string.Empty;

                    if (IsInsideDirectory(filePath, destinationFolder))
                        continue;

                    var previewDestination = GetPreviewUniquePath(
                        Path.Combine(destinationFolder, Path.GetFileName(filePath)),
                        reservedDestinations);

                    reservedDestinations.Add(previewDestination);

                    result.Add(new FileItem
                    {
                        FileName = Path.GetFileName(filePath),
                        SourcePath = filePath,
                        CurrentLocation = currentFolder,
                        DestinationFolder = destinationFolder,
                        DestinationPath = previewDestination,
                        Reason = match.Reason,
                        Category = match.Rule.Name
                    });
                }
                catch
                {
                    // 미리보기 스캔 중 접근 불가 파일은 조용히 건너뜁니다.
                }
            }
        }

        return result
            .OrderBy(item => item.Category)
            .ThenBy(item => item.FileName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static bool IsSafeTargetFolder(string folderPath)
    {
        try
        {
            var fullPath = NormalizeDirectory(folderPath);

            var windows = NormalizeDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            var programFiles = NormalizeDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            var programFilesX86 = NormalizeDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
            var appData = NormalizeDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            var localAppData = NormalizeDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            var commonAppData = NormalizeDirectory(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));

            var blockedFolders = new[]
            {
                windows,
                programFiles,
                programFilesX86,
                appData,
                localAppData,
                commonAppData
            }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var blocked in blockedFolders)
            {
                if (fullPath.Equals(blocked, StringComparison.OrdinalIgnoreCase) ||
                    fullPath.StartsWith(blocked + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            var root = Path.GetPathRoot(fullPath);
            if (!string.IsNullOrWhiteSpace(root) && fullPath.Equals(NormalizeDirectory(root), StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> EnumerateFilesSafely(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            if (!IsSafeTargetFolder(current))
                continue;

            IEnumerable<string> files = Array.Empty<string>();
            IEnumerable<string> directories = Array.Empty<string>();

            try
            {
                files = Directory.EnumerateFiles(current);
            }
            catch
            {
                // 접근할 수 없는 폴더는 건너뜁니다.
            }

            foreach (var file in files)
                yield return file;

            try
            {
                directories = Directory.EnumerateDirectories(current)
                    .Where(directory => !IsReparsePoint(directory));
            }
            catch
            {
                // 접근할 수 없는 폴더는 건너뜁니다.
            }

            foreach (var directory in directories)
                pending.Push(directory);
        }
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            return attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            return true;
        }
    }

    private static RuleMatch? MatchRule(string filePath, List<Rule> rules)
    {
        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();

        foreach (var rule in rules.Where(rule => rule.MatchType == RuleMatchType.Keyword))
        {
            var keyword = rule.Keywords.FirstOrDefault(k =>
                !string.IsNullOrWhiteSpace(k) &&
                fileName.Contains(k, StringComparison.CurrentCultureIgnoreCase));

            if (keyword is not null)
            {
                return new RuleMatch(
                    rule,
                    $"파일명 키워드 '{keyword}' → {rule.Name}");
            }
        }

        foreach (var rule in rules.Where(rule => rule.MatchType == RuleMatchType.Extension))
        {
            if (rule.Extensions.Any(ext => ext.TrimStart('.').Equals(extension, StringComparison.OrdinalIgnoreCase)))
            {
                return new RuleMatch(
                    rule,
                    $"확장자 .{extension} → {rule.Name}");
            }
        }

        var fallback = rules.FirstOrDefault(rule => rule.MatchType == RuleMatchType.Fallback);
        return fallback is null
            ? null
            : new RuleMatch(fallback, "일치하는 규칙 없음 → 기타");
    }

    private static string GetPreviewUniquePath(string requestedPath, HashSet<string> reserved)
    {
        var directory = Path.GetDirectoryName(requestedPath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(requestedPath);
        var extension = Path.GetExtension(requestedPath);
        var candidate = requestedPath;
        var index = 1;

        while (File.Exists(candidate) || Directory.Exists(candidate) || reserved.Contains(candidate))
        {
            candidate = Path.Combine(directory, $"{fileNameWithoutExtension} ({index}){extension}");
            index++;
        }

        return candidate;
    }

    private static bool IsInsideDirectory(string path, string directory)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var fullDirectory = NormalizeDirectory(directory) + Path.DirectorySeparatorChar;
            return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeDirectory(string path)
    {
        return Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private sealed record RuleMatch(Rule Rule, string Reason);
}
