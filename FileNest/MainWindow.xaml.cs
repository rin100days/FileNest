using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using FileNest.Models;
using FileNest.Services;
using WinForms = System.Windows.Forms;

namespace FileNest;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly SettingsService _settingsService = new();
    private readonly HistoryService _historyService = new();
    private readonly FileScanner _fileScanner = new();
    private readonly FileOrganizer _fileOrganizer;

    private bool _previewCreated;
    private string? _selectedTargetFolder;
    private string _statusMessage = "먼저 [미리보기 새로고침]을 눌러 이동 예정 파일을 확인하세요.";

    public ObservableCollection<string> TargetFolders { get; } = new();
    public ObservableCollection<FileItem> PreviewItems { get; } = new();
    public ObservableCollection<Rule> Rules { get; } = new();

    public string? SelectedTargetFolder
    {
        get => _selectedTargetFolder;
        set
        {
            _selectedTargetFolder = value;
            OnPropertyChanged(nameof(SelectedTargetFolder));
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        InitializeComponent();
        _fileOrganizer = new FileOrganizer(_historyService);
        DataContext = this;
        LoadSettingsIntoUi();
    }

    private void LoadSettingsIntoUi()
    {
        var settings = _settingsService.Load();

        TargetFolders.Clear();
        foreach (var folder in settings.TargetFolders)
            TargetFolders.Add(folder);

        Rules.Clear();
        foreach (var rule in settings.Rules)
            Rules.Add(rule);
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new WinForms.FolderBrowserDialog
        {
            Description = "FileNest가 정리할 폴더를 선택하세요.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() != WinForms.DialogResult.OK)
            return;

        var selectedPath = dialog.SelectedPath;

        if (!FileScanner.IsSafeTargetFolder(selectedPath))
        {
            MessageBox.Show(
                "Windows, Program Files, AppData 같은 시스템 폴더는 정리 대상으로 추가할 수 없습니다.",
                "안전장치",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (TargetFolders.Any(path => path.Equals(selectedPath, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = "이미 추가된 폴더입니다.";
            return;
        }

        TargetFolders.Add(selectedPath);
        _previewCreated = false;
        StatusMessage = "폴더를 추가했습니다. 실제 이동 전 [미리보기 새로고침]을 눌러 확인하세요.";
    }

    private void RemoveFolder_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedTargetFolder is null)
        {
            StatusMessage = "제거할 폴더를 왼쪽 목록에서 선택하세요.";
            return;
        }

        TargetFolders.Remove(SelectedTargetFolder);
        SelectedTargetFolder = null;
        _previewCreated = false;
        StatusMessage = "선택한 폴더를 목록에서 제거했습니다. [설정 저장]을 누르면 settings.json에 반영됩니다.";
    }

    private async void RefreshPreview_Click(object sender, RoutedEventArgs e)
    {
        StatusMessage = "미리보기를 만드는 중입니다...";
        PreviewItems.Clear();

        var folders = TargetFolders.ToList();
        var rules = Rules.ToList();

        var preview = await Task.Run(() => _fileScanner.Scan(folders, rules));

        foreach (var item in preview)
            PreviewItems.Add(item);

        _previewCreated = true;
        StatusMessage = $"미리보기 완료: {PreviewItems.Count}개 파일이 정리 대상으로 잡혔습니다. 실제 이동은 [정리 실행]을 눌러야만 진행됩니다.";
    }

    private async void Organize_Click(object sender, RoutedEventArgs e)
    {
        if (!_previewCreated)
        {
            MessageBox.Show(
                "이동 전 미리보기가 필수입니다. 먼저 [미리보기 새로고침]을 눌러주세요.",
                "미리보기 필요",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (PreviewItems.Count == 0)
        {
            StatusMessage = "정리할 파일이 없습니다.";
            return;
        }

        var confirm = MessageBox.Show(
            $"미리보기의 {PreviewItems.Count}개 파일을 실제로 이동할까요?\n이동 기록은 history.json에 저장됩니다.",
            "정리 실행 확인",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        StatusMessage = "파일을 이동하는 중입니다...";
        var items = PreviewItems.ToList();
        var result = await Task.Run(() => _fileOrganizer.Organize(items));

        PreviewItems.Clear();
        _previewCreated = false;

        StatusMessage = BuildResultMessage("정리 완료", result);
    }

    private async void Undo_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "마지막 정리 기록을 기준으로 파일을 원래 위치로 되돌릴까요?",
            "마지막 정리 되돌리기",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        StatusMessage = "마지막 정리를 되돌리는 중입니다...";
        var result = await Task.Run(() => _fileOrganizer.UndoLastBatch());

        PreviewItems.Clear();
        _previewCreated = false;

        StatusMessage = BuildResultMessage("되돌리기 완료", result);
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        var settings = new AppSettings
        {
            TargetFolders = TargetFolders.ToList(),
            Rules = Rules.ToList()
        };

        _settingsService.Save(settings);
        StatusMessage = $"설정을 저장했습니다: {_settingsService.SettingsPath}";
    }

    private static string BuildResultMessage(string title, OrganizerResult result)
    {
        var recentLogs = result.Logs.TakeLast(5);
        var logText = string.Join(Environment.NewLine, recentLogs);

        if (string.IsNullOrWhiteSpace(logText))
            logText = "기록된 상세 로그가 없습니다.";

        return $"{title}: 이동 {result.MovedCount}개 / 건너뜀 {result.SkippedCount}개" +
               Environment.NewLine +
               "최근 로그:" +
               Environment.NewLine +
               logText;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
