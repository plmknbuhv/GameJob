using System.Text.RegularExpressions;
using GameJobWeb.Models;

namespace GameJobWeb.Services;

public sealed class FitAnalyzer(JobPostingParser parser)
{
    public FitReport Analyze(JobPosting job, string resumeText)
    {
        var profileLower = resumeText.ToLowerInvariant();
        var profileSkills = parser.ExtractSkills(resumeText).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var matchedSkills = job.Skills.Where(profileSkills.Contains).ToList();
        var missingSkills = job.Skills.Where(skill => !profileSkills.Contains(skill)).ToList();
        var matchedRequirements = job.Required
            .Where(requirement => RequirementMatches(requirement, profileLower, profileSkills))
            .ToList();
        var weakRequirements = job.Required
            .Where(requirement => !matchedRequirements.Contains(requirement))
            .ToList();

        var score = Score(job, matchedSkills, missingSkills, matchedRequirements, weakRequirements);

        return new FitReport(
            score,
            Level(score),
            matchedSkills,
            missingSkills,
            matchedRequirements,
            weakRequirements,
            Strengths(matchedSkills, matchedRequirements),
            Recommendations(missingSkills, weakRequirements, job.Preferred),
            ImprovementItems(missingSkills, weakRequirements, job.Preferred),
            job);
    }

    private bool RequirementMatches(string requirement, string profileLower, IReadOnlySet<string> profileSkills)
    {
        var reqLower = requirement.ToLowerInvariant();
        foreach (var skill in parser.SkillAliases)
        {
            if (skill.Value.Any(alias => reqLower.Contains(alias.ToLowerInvariant(), StringComparison.Ordinal))
                && profileSkills.Contains(skill.Key))
            {
                return true;
            }
        }

        var tokens = MeaningfulTokens(reqLower);
        if (tokens.Count == 0)
        {
            return false;
        }

        var hits = tokens.Count(token => profileLower.Contains(token, StringComparison.Ordinal));
        return hits / (double)tokens.Count >= 0.35;
    }

    private static int Score(
        JobPosting job,
        IReadOnlyCollection<string> matchedSkills,
        IReadOnlyCollection<string> missingSkills,
        IReadOnlyCollection<string> matchedRequirements,
        IReadOnlyCollection<string> weakRequirements)
    {
        var skillTotal = Math.Max(matchedSkills.Count + missingSkills.Count, 1);
        var requirementTotal = Math.Max(matchedRequirements.Count + weakRequirements.Count, 1);
        var skillScore = matchedSkills.Count / (double)skillTotal;
        var requirementScore = matchedRequirements.Count / (double)requirementTotal;
        var preferredBonus = job.Preferred.Count > 0 && job.Preferred.Any(matchedRequirements.Contains) ? 0.08 : 0;

        return Math.Clamp((int)Math.Round((skillScore * 0.45 + requirementScore * 0.50 + preferredBonus) * 100), 0, 100);
    }

    private static string Level(int score)
    {
        return score switch
        {
            >= 80 => "매우 적합",
            >= 65 => "적합",
            >= 45 => "보완 필요",
            _ => "지원 전 보완 권장"
        };
    }

    private static List<string> Strengths(IReadOnlyCollection<string> matchedSkills, IReadOnlyCollection<string> matchedRequirements)
    {
        var strengths = new List<string>();
        if (matchedSkills.Count > 0)
        {
            strengths.Add("공고의 핵심 기술과 자기소개서의 경험 키워드가 일부 일치합니다.");
        }

        if (matchedRequirements.Count > 0)
        {
            strengths.Add("필수 요건 중 실제 경험으로 뒷받침할 수 있는 항목이 있습니다.");
        }

        if (strengths.Count == 0)
        {
            strengths.Add("현재 PDF 내용만으로는 강점 매칭 근거가 부족합니다.");
        }

        return strengths;
    }

    private static List<string> Recommendations(
        IReadOnlyList<string> missingSkills,
        IReadOnlyList<string> weakRequirements,
        IReadOnlyCollection<string> preferred)
    {
        var recommendations = new List<string>();
        recommendations.AddRange(missingSkills.Take(5).Select(skill => $"'{skill}' 관련 프로젝트 경험, 코드 링크, 성과 지표를 자기소개서나 포트폴리오에 보강하세요."));
        recommendations.AddRange(weakRequirements.Take(3).Select(requirement => $"핵심 역량 요건 '{requirement}'에 대응하는 프로젝트 경험과 역할을 자기소개서에 명시하세요."));

        if (preferred.Count > 0)
        {
            recommendations.Add("우대사항과 맞닿은 경험은 별도 문단으로 강조하면 적합도 상승 여지가 큽니다.");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("현재 공고와 잘 맞습니다. 프로젝트 성과를 수치로 더 선명하게 보여주세요.");
        }

        return recommendations;
    }

    private static List<ImprovementItem> ImprovementItems(
        IReadOnlyList<string> missingSkills,
        IReadOnlyList<string> weakRequirements,
        IReadOnlyCollection<string> preferred)
    {
        var items = new List<ImprovementItem>();

        items.AddRange(missingSkills.Take(6).Select((skill, index) =>
        {
            var guide = SkillGuide(skill);
            return new ImprovementItem(
                guide.Title,
                index < 2 ? "높음" : "중간",
                guide.Reason,
                guide.PortfolioAction,
                guide.ResumeAction,
                guide.EvidenceExample);
        }));

        items.AddRange(weakRequirements.Take(5).Select((requirement, index) =>
        {
            var normalized = requirement.Trim();
            return new ImprovementItem(
                $"요건 보강: {Shorten(normalized, 52)}",
                index < 2 ? "높음" : "중간",
                "공고의 핵심 요건인데 자기소개서에서 직접 대응되는 경험 근거가 약하게 감지되었습니다.",
                $"'{normalized}'와 연결되는 프로젝트를 1개 골라 본인의 역할, 사용 기술, 문제 상황, 해결 결과를 정리하세요.",
                "자기소개서에는 '제가 담당한 부분', '기술 선택 이유', '결과 수치 또는 개선 효과'가 한 문단 안에 보이게 작성하세요.",
                "예: 매칭 서버의 응답 지연을 로그 분석으로 확인하고 캐싱을 적용해 평균 응답 시간을 35% 줄였습니다.");
        }));

        if (preferred.Count > 0)
        {
            items.Add(new ImprovementItem(
                "우대사항 연결 강화",
                "중간",
                "우대사항은 필수 요건보다 점수 비중은 낮지만, 비슷한 지원자 사이에서 차이를 만드는 근거가 됩니다.",
                "우대사항과 겹치는 경험을 포트폴리오 첫 화면이나 프로젝트 요약에 별도 태그로 표시하세요.",
                "자기소개서 말미에 '공고의 우대사항과 연결되는 경험'을 2-3문장으로 압축해 넣으세요.",
                "예: 라이브 운영 중 로그를 분석해 이탈 구간을 찾고 튜토리얼 보상을 조정한 경험이 있습니다."));
        }

        if (items.Count == 0)
        {
            items.Add(new ImprovementItem(
                "성과 근거 선명화",
                "중간",
                "핵심 기술은 잘 맞지만 결과가 수치와 역할로 표현될수록 설득력이 올라갑니다.",
                "대표 프로젝트마다 기간, 인원, 담당 영역, 사용 기술, 결과 지표를 표 형태로 정리하세요.",
                "자기소개서에는 추상적인 표현보다 '문제-행동-결과' 순서의 짧은 사례를 넣으세요.",
                "예: 4인 팀 프로젝트에서 서버 API 12개를 설계했고, 테스트 자동화로 회귀 버그를 조기에 발견했습니다."));
        }

        return items;
    }

    private static SkillImprovementGuide SkillGuide(string skill)
    {
        return skill switch
        {
            "unity" => new SkillImprovementGuide(
                "Unity 경험 보강",
                "게임 클라이언트 직무에서는 엔진 이해, 씬 구성, 프리팹, UI, 애니메이션, 빌드 경험이 중요한 판단 근거입니다.",
                "작은 플레이어블 데모를 만들고 씬 구조, 입력 처리, UI 흐름, 빌드 파일 또는 영상 링크를 함께 제출하세요.",
                "자기소개서에는 Unity에서 어떤 기능을 직접 구현했고 어떤 문제를 해결했는지 구체적으로 적으세요.",
                "예: Unity로 인벤토리 UI와 아이템 장착 로직을 구현하고 ScriptableObject로 데이터 관리를 분리했습니다."),
            "unreal" => new SkillImprovementGuide(
                "Unreal 경험 보강",
                "언리얼 공고는 C++/블루프린트, 액터 컴포넌트, 레벨 구성, 최적화 경험을 중요하게 봅니다.",
                "Actor, Component, Blueprint 연동 사례가 보이는 샘플 프로젝트와 플레이 영상을 준비하세요.",
                "자기소개서에는 C++와 블루프린트를 어떤 기준으로 나눠 사용했는지 설명하세요.",
                "예: 캐릭터 이동 로직은 C++로 구현하고 상호작용 연출은 Blueprint로 분리해 수정 비용을 줄였습니다."),
            "server" => new SkillImprovementGuide(
                "게임 서버 경험 보강",
                "서버 직무는 API 설계, 세션/매칭/인증, 장애 대응, 운영 관점의 안정성을 중요하게 봅니다.",
                "로그인, 매칭, 랭킹, 인벤토리 중 하나를 골라 API 명세와 DB 구조, 예외 처리를 문서화하세요.",
                "자기소개서에는 트래픽, 동시성, 데이터 정합성 문제를 어떻게 다뤘는지 적으세요.",
                "예: 매칭 요청 중복 처리를 위해 Redis 락을 적용하고 실패 케이스를 재시도 큐로 분리했습니다."),
            "network" => new SkillImprovementGuide(
                "네트워크 프로그래밍 보강",
                "실시간 게임이나 서버 공고에서는 TCP/UDP, 패킷, 지연, 재전송, 동기화 이해가 강한 신호가 됩니다.",
                "간단한 채팅/룸/실시간 위치 동기화 예제를 만들고 패킷 구조와 지연 대응 방식을 설명하세요.",
                "자기소개서에는 네트워크 지연, 순서 보장, 패킷 손실 같은 문제를 어떤 방식으로 고려했는지 넣으세요.",
                "예: UDP 위치 동기화에서 스냅샷 보간을 적용해 움직임 끊김을 완화했습니다."),
            "database" => new SkillImprovementGuide(
                "데이터베이스 경험 보강",
                "게임 서비스는 유저 데이터, 아이템, 재화, 로그가 많아 스키마 설계와 조회 성능 경험을 중요하게 봅니다.",
                "유저/아이템/매치 기록 테이블을 설계하고 인덱스, 트랜잭션, 조회 쿼리 예시를 포트폴리오에 넣으세요.",
                "자기소개서에는 데이터 구조를 왜 그렇게 설계했는지와 성능 개선 근거를 적으세요.",
                "예: 랭킹 조회 쿼리에 복합 인덱스를 적용해 테스트 데이터 10만 건 기준 조회 시간을 줄였습니다."),
            "liveops" => new SkillImprovementGuide(
                "라이브 운영 경험 보강",
                "라이브 게임 회사는 출시 후 지표 분석, 이벤트 운영, 로그 확인, 문제 대응 경험을 높게 봅니다.",
                "가상의 이벤트 운영안, 핵심 지표 대시보드, 로그 분석 예시를 만들어 운영 관점을 보여주세요.",
                "자기소개서에는 유저 행동 데이터를 보고 어떤 개선 결정을 했는지 사례 중심으로 쓰세요.",
                "예: 튜토리얼 이탈 구간을 로그로 확인하고 보상 지급 시점을 조정하는 개선안을 제안했습니다."),
            "qa" => new SkillImprovementGuide(
                "QA 역량 보강",
                "QA 직무는 버그 재현력, 테스트 케이스 설계, 리스크 판단, 협업 커뮤니케이션을 중요하게 봅니다.",
                "테스트 케이스 표, 버그 리포트 샘플, 재현 절차와 기대 결과가 포함된 문서를 준비하세요.",
                "자기소개서에는 발견한 문제를 개발팀이 바로 처리할 수 있게 정리한 경험을 적으세요.",
                "예: 특정 해상도에서 UI가 겹치는 문제를 재현 조건, 스크린샷, 기대 결과와 함께 보고했습니다."),
            "game_design" => new SkillImprovementGuide(
                "게임 기획 역량 보강",
                "기획 직무는 시스템 구조화, 밸런스 근거, 유저 경험 설계, 문서화 능력을 중요하게 봅니다.",
                "시스템 기획서, 밸런스 테이블, 유저 플로우, 개선안 전후 비교를 포트폴리오에 추가하세요.",
                "자기소개서에는 재미를 어떻게 정의했고 어떤 근거로 수치를 조정했는지 설명하세요.",
                "예: 초반 전투 이탈을 줄이기 위해 몬스터 체력 곡선을 조정하고 클리어 시간을 비교했습니다."),
            "art" => new SkillImprovementGuide(
                "아트 역량 보강",
                "아트 직무는 결과물뿐 아니라 제작 의도, 스타일 적합성, 파이프라인 이해를 함께 봅니다.",
                "완성 이미지 외에 러프, 중간 과정, 레퍼런스, 폴리곤/텍스처 정보 또는 엔진 적용 화면을 넣으세요.",
                "자기소개서에는 작업 의도와 피드백 반영 과정을 구체적으로 적으세요.",
                "예: 모바일 환경을 고려해 텍스처 해상도를 조정하고 실루엣이 잘 읽히도록 형태를 단순화했습니다."),
            _ => new SkillImprovementGuide(
                $"{skill} 경험 보강",
                "공고에서 반복적으로 등장하는 기술은 기업이 실제 업무 투입 가능성을 판단하는 핵심 단서입니다.",
                $"'{skill}'를 사용한 작은 기능을 직접 만들고 코드, 화면, 문제 해결 과정을 함께 정리하세요.",
                $"자기소개서에는 '{skill}'를 단순히 안다고 쓰기보다 어떤 상황에서 사용했고 어떤 결과를 만들었는지 적으세요.",
                $"예: {skill}를 활용해 기능을 구현했고, 문제 원인을 찾아 구조를 개선했습니다.")
        };
    }

    private static string Shorten(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength - 1), "...");
    }

    private static List<string> MeaningfulTokens(string text)
    {
        var stopwords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "및", "또는", "관련", "경험", "이상", "가능", "보유", "업무", "개발", "대한", "이해", "분",
            "the", "and", "with"
        };

        return Regex.Matches(text, "[a-zA-Z+#]{2,}|[가-힣]{2,}")
            .Select(match => match.Value)
            .Where(token => !stopwords.Contains(token))
            .ToList();
    }
}

internal sealed record SkillImprovementGuide(
    string Title,
    string Reason,
    string PortfolioAction,
    string ResumeAction,
    string EvidenceExample);
