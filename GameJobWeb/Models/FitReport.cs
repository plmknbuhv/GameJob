namespace GameJobWeb.Models;

public sealed record JobPosting(
    string Title,
    IReadOnlyList<string> Required,
    IReadOnlyList<string> Preferred,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Experience,
    string RawText);

public sealed record FitReport(
    int Score,
    string Level,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills,
    IReadOnlyList<string> MatchedRequirements,
    IReadOnlyList<string> WeakRequirements,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Recommendations,
    IReadOnlyList<ImprovementItem> ImprovementItems,
    JobPosting Job);

public sealed record ImprovementItem(
    string Title,
    string Priority,
    string Reason,
    string PortfolioAction,
    string ResumeAction,
    string EvidenceExample);
