using System.Text.RegularExpressions;
using GameJobWeb.Models;

namespace GameJobWeb.Services;

public sealed class JobPostingParser
{
    private static readonly Dictionary<string, string[]> GameSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        ["unity"] = ["unity", "유니티", "c#"],
        ["unreal"] = ["unreal", "언리얼", "ue4", "ue5", "c++"],
        ["csharp"] = ["c#", "c sharp", "씨샵"],
        ["cplusplus"] = ["c++", "cpp"],
        ["python"] = ["python", "파이썬"],
        ["java"] = ["java", "자바"],
        ["javascript"] = ["javascript", "typescript", "react", "node"],
        ["server"] = ["server", "backend", "서버", "백엔드", "api"],
        ["network"] = ["network", "socket", "tcp", "udp", "네트워크", "소켓"],
        ["database"] = ["mysql", "postgresql", "redis", "mongodb", "db", "database", "데이터베이스"],
        ["liveops"] = ["liveops", "라이브", "운영", "지표", "로그", "analytics"],
        ["game_design"] = ["기획", "밸런스", "레벨 디자인", "시스템 디자인"],
        ["art"] = ["3d", "maya", "blender", "모델링", "애니메이션", "원화"],
        ["qa"] = ["qa", "테스트", "버그", "검증"]
    };

    private static readonly Regex RequiredPattern = new("(필수\\s*요건|자격\\s*요건|지원\\s*자격|필수\\s*사항|requirements?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PreferredPattern = new("(우대\\s*사항|우대\\s*요건|preferred|plus|nice\\s*to\\s*have)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ExperiencePattern = new("(경력|experience|년\\s*이상)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DutyPattern = new("(담당\\s*업무|주요\\s*업무|업무\\s*내용|responsibilities?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex StopSectionPattern = new(
        "(근무\\s*조건|근무\\s*지역|근무지|인근\\s*지하철|회사\\s*위치|복리\\s*후생|복지|전형\\s*절차|채용\\s*절차|접수\\s*기간|제출\\s*서류|고용\\s*형태|급여|담당자|문의|주소|지도|찾아오시는)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NonCompetencyLinePattern = new(
        "(근무\\s*지역|근무지|인근\\s*지하철|회사\\s*위치|주소|\\d+\\s*호선|\\d+\\s*번\\s*출구|\\d+\\s*m\\s*이내|성수역|강남역|역삼역|판교역|지하철|버스|지도|복리\\s*후생|복지|식대|연차|휴가|보험|퇴직금|제출\\s*서류|접수\\s*기간|마감일|담당자|문의)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CompetencySignalPattern = new(
        "(개발|구현|설계|운영|분석|최적화|테스트|검증|협업|커뮤니케이션|경험|경력|능력|역량|이해|지식|숙련|사용|가능|엔진|프로그래밍|서버|클라이언트|네트워크|데이터|DB|API|Unity|유니티|Unreal|언리얼|C#|C\\+\\+|Python|Java|QA|기획|아트|모델링|라이브|툴)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public JobPosting Parse(string text)
    {
        var normalized = Normalize(text);
        var lines = normalized
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Trim(' ', '-', 'ㆍ', '*', '•', '\t'))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var sections = CollectSections(lines);

        return new JobPosting(
            GuessTitle(lines),
            Dedupe(sections["required"].Count > 0 ? sections["required"] : KeywordLines(lines, RequiredPattern)),
            Dedupe(sections["preferred"].Count > 0 ? sections["preferred"] : KeywordLines(lines, PreferredPattern)),
            ExtractSkills(normalized),
            Dedupe(sections["experience"].Count > 0 ? sections["experience"] : KeywordLines(lines, ExperiencePattern)),
            text.Trim());
    }

    public IReadOnlyList<string> ExtractSkills(string text)
    {
        var lower = text.ToLowerInvariant();
        return GameSkills
            .Where(skill => skill.Value.Any(alias => lower.Contains(alias.ToLowerInvariant(), StringComparison.Ordinal)))
            .Select(skill => skill.Key)
            .ToList();
    }

    public IReadOnlyDictionary<string, string[]> SkillAliases => GameSkills;

    private static string Normalize(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        return Regex.Replace(normalized, "\n{3,}", "\n\n").Trim();
    }

    private static string GuessTitle(IReadOnlyList<string> lines)
    {
        foreach (var line in lines.Take(8))
        {
            if (line.Length <= 80 && !line.EndsWith(':') && !RequiredPattern.IsMatch(line))
            {
                return line;
            }
        }

        return "게임 채용공고";
    }

    private static Dictionary<string, List<string>> CollectSections(IReadOnlyList<string> lines)
    {
        var sections = new Dictionary<string, List<string>>
        {
            ["required"] = [],
            ["preferred"] = [],
            ["experience"] = []
        };

        string? current = null;
        foreach (var line in lines)
        {
            if (IsStopSection(line))
            {
                current = null;
                continue;
            }

            var matched = SectionForLine(line);
            if (matched is not null && line.Length <= 40)
            {
                current = matched;
                continue;
            }

            if (current is not null && IsCompetencyRequirement(line))
            {
                sections[current].Add(line);
            }
        }

        return sections;
    }

    private static string? SectionForLine(string line)
    {
        if (RequiredPattern.IsMatch(line))
        {
            return "required";
        }

        if (PreferredPattern.IsMatch(line))
        {
            return "preferred";
        }

        if (ExperiencePattern.IsMatch(line))
        {
            return "experience";
        }

        return DutyPattern.IsMatch(line) ? "required" : null;
    }

    private static List<string> KeywordLines(IEnumerable<string> lines, Regex pattern)
    {
        return lines.Where(line => pattern.IsMatch(line) && IsCompetencyRequirement(line)).ToList();
    }

    private static List<string> Dedupe(IEnumerable<string> items)
    {
        return items
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsStopSection(string line)
    {
        return line.Length <= 60 && StopSectionPattern.IsMatch(line);
    }

    private static bool IsCompetencyRequirement(string line)
    {
        if (line.Length < 3 || NonCompetencyLinePattern.IsMatch(line))
        {
            return false;
        }

        return CompetencySignalPattern.IsMatch(line);
    }
}
