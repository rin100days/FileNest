using System.IO;
using System.Text.Json;
using FileNest.Models;

namespace FileNest.Services;

public class AppSettings
{
    public List<string> TargetFolders { get; set; } = new();
    public List<Rule> Rules { get; set; } = new();
}

public class SettingsService
{
    public static string AppFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FileNest");

    public string SettingsPath => Path.Combine(AppFolder, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AppSettings Load()
    {
        Directory.CreateDirectory(AppFolder);

        if (!File.Exists(SettingsPath))
        {
            var defaults = CreateDefaultSettings();
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? CreateDefaultSettings();

            if (settings.TargetFolders.Count == 0)
                settings.TargetFolders = CreateDefaultTargetFolders();

            if (settings.Rules.Count == 0)
                settings.Rules = CreateDefaultRules();

            settings.TargetFolders = settings.TargetFolders
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return settings;
        }
        catch
        {
            var defaults = CreateDefaultSettings();
            Save(defaults);
            return defaults;
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(AppFolder);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    public static AppSettings CreateDefaultSettings()
    {
        return new AppSettings
        {
            TargetFolders = CreateDefaultTargetFolders(),
            Rules = CreateDefaultRules()
        };
    }

    public static List<string> CreateDefaultTargetFolders()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var downloads = Path.Combine(userProfile, "Downloads");
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        return new[] { downloads, desktop, documents }
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<Rule> CreateDefaultRules()
    {
        return new List<Rule>
        {
            new()
            {
                Name = "TRPG 문서",
                MatchType = RuleMatchType.Keyword,
                Keywords = new List<string> { "coc", "크툴루", "trpg", "시나리오", "캐릭터시트" },
                DestinationRelativePath = "Documents/TRPG",
                Description = "파일명에 TRPG 관련 키워드가 들어가면 TRPG 폴더로 이동"
            },
            new()
            {
                Name = "학교 자료",
                MatchType = RuleMatchType.Keyword,
                Keywords = new List<string> { "과제", "수행", "학교", "영어", "국어" },
                DestinationRelativePath = "Documents/School",
                Description = "파일명에 학교/과제 관련 키워드가 들어가면 School 폴더로 이동"
            },
            new()
            {
                Name = "커미션/레퍼런스",
                MatchType = RuleMatchType.Keyword,
                Keywords = new List<string> { "커미션", "reference", "ref", "자료" },
                DestinationRelativePath = "Pictures/Reference",
                Description = "파일명에 커미션/자료 키워드가 들어가면 Reference 폴더로 이동"
            },
            new()
            {
                Name = "드림/Yume",
                MatchType = RuleMatchType.Keyword,
                Keywords = new List<string> { "카노", "라이카", "yume", "드림" },
                DestinationRelativePath = "Documents/Yume",
                Description = "파일명에 드림 관련 키워드가 들어가면 Yume 폴더로 이동"
            },
            new()
            {
                Name = "이미지",
                MatchType = RuleMatchType.Extension,
                Extensions = new List<string> { "jpg", "jpeg", "png", "gif", "webp", "heic" },
                DestinationRelativePath = "Pictures/Images",
                Description = "이미지 확장자 파일"
            },
            new()
            {
                Name = "문서",
                MatchType = RuleMatchType.Extension,
                Extensions = new List<string> { "pdf", "docx", "hwp", "txt", "md", "pptx", "xlsx" },
                DestinationRelativePath = "Documents/Files",
                Description = "문서 확장자 파일"
            },
            new()
            {
                Name = "영상",
                MatchType = RuleMatchType.Extension,
                Extensions = new List<string> { "mp4", "mov", "mkv", "avi", "webm" },
                DestinationRelativePath = "Videos/Files",
                Description = "영상 확장자 파일"
            },
            new()
            {
                Name = "음악",
                MatchType = RuleMatchType.Extension,
                Extensions = new List<string> { "mp3", "wav", "flac", "m4a" },
                DestinationRelativePath = "Music/Files",
                Description = "음악 확장자 파일"
            },
            new()
            {
                Name = "압축파일",
                MatchType = RuleMatchType.Extension,
                Extensions = new List<string> { "zip", "rar", "7z" },
                DestinationRelativePath = "Downloads/Archives",
                Description = "압축 파일"
            },
            new()
            {
                Name = "설치파일",
                MatchType = RuleMatchType.Extension,
                Extensions = new List<string> { "exe", "msi" },
                DestinationRelativePath = "Downloads/Installers",
                Description = "설치 파일"
            },
            new()
            {
                Name = "코드",
                MatchType = RuleMatchType.Extension,
                Extensions = new List<string> { "html", "css", "js", "ts", "py", "json", "cs" },
                DestinationRelativePath = "Documents/Code",
                Description = "코드 파일"
            },
            new()
            {
                Name = "기타",
                MatchType = RuleMatchType.Fallback,
                DestinationRelativePath = "Downloads/Others",
                Description = "위 규칙에 해당하지 않는 파일"
            }
        };
    }
}
