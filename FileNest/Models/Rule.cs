namespace FileNest.Models;

public enum RuleMatchType
{
    Keyword,
    Extension,
    Fallback
}

public class Rule
{
    public string Name { get; set; } = string.Empty;
    public RuleMatchType MatchType { get; set; }
    public List<string> Extensions { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
    public string DestinationRelativePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;

    public string DisplayCondition
    {
        get
        {
            return MatchType switch
            {
                RuleMatchType.Keyword => string.Join(", ", Keywords),
                RuleMatchType.Extension => string.Join(", ", Extensions.Select(e => e.StartsWith('.') ? e : "." + e)),
                _ => "매칭되는 규칙 없음"
            };
        }
    }
}
