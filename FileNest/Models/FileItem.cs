namespace FileNest.Models;

public class FileItem
{
    public string FileName { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string CurrentLocation { get; set; } = string.Empty;
    public string DestinationFolder { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
