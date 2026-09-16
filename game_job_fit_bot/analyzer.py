from __future__ import annotations

import re

from .models import FitReport, JobPosting
from .parser import GAME_SKILLS, extract_skills


def analyze_fit(job: JobPosting, profile_text: str) -> FitReport:
    profile_lower = profile_text.lower()
    profile_skills = set(extract_skills(profile_text))

    matched_skills = [skill for skill in job.skills if skill in profile_skills]
    missing_skills = [skill for skill in job.skills if skill not in profile_skills]

    matched_requirements = [
        req for req in job.required if _requirement_matches(req, profile_lower, profile_skills)
    ]
    weak_requirements = [req for req in job.required if req not in matched_requirements]

    score = _score(job, matched_skills, missing_skills, matched_requirements, weak_requirements)
    strengths = _strengths(matched_skills, matched_requirements)
    recommendations = _recommendations(missing_skills, weak_requirements, job.preferred)

    return FitReport(
        score=score,
        level=_level(score),
        matched_skills=matched_skills,
        missing_skills=missing_skills,
        matched_requirements=matched_requirements,
        weak_requirements=weak_requirements,
        strengths=strengths,
        recommendations=recommendations,
        job=job,
    )


def _requirement_matches(requirement: str, profile_lower: str, profile_skills: set[str]) -> bool:
    req_lower = requirement.lower()
    for skill, aliases in GAME_SKILLS.items():
        if any(alias.lower() in req_lower for alias in aliases) and skill in profile_skills:
            return True

    tokens = _meaningful_tokens(req_lower)
    if not tokens:
        return False

    hits = sum(1 for token in tokens if token in profile_lower)
    return hits / len(tokens) >= 0.35


def _meaningful_tokens(text: str) -> list[str]:
    raw_tokens = re.findall(r"[a-zA-Z+#]{2,}|[가-힣]{2,}", text)
    stopwords = {
        "및",
        "또는",
        "관련",
        "경험",
        "이상",
        "가능",
        "보유",
        "업무",
        "개발",
        "대한",
        "이해",
        "분",
        "the",
        "and",
        "with",
    }
    return [token for token in raw_tokens if token not in stopwords]


def _score(
    job: JobPosting,
    matched_skills: list[str],
    missing_skills: list[str],
    matched_requirements: list[str],
    weak_requirements: list[str],
) -> int:
    skill_total = max(len(matched_skills) + len(missing_skills), 1)
    req_total = max(len(matched_requirements) + len(weak_requirements), 1)

    skill_score = len(matched_skills) / skill_total
    req_score = len(matched_requirements) / req_total
    preferred_bonus = 0.08 if job.preferred and any(req in matched_requirements for req in job.preferred) else 0

    return max(0, min(100, round((skill_score * 0.45 + req_score * 0.50 + preferred_bonus) * 100)))


def _level(score: int) -> str:
    if score >= 80:
        return "매우 적합"
    if score >= 65:
        return "적합"
    if score >= 45:
        return "보완 필요"
    return "지원 전 보완 권장"


def _strengths(matched_skills: list[str], matched_requirements: list[str]) -> list[str]:
    strengths: list[str] = []
    if matched_skills:
        strengths.append("공고의 핵심 기술과 포트폴리오 기술 스택이 일부 일치합니다.")
    if matched_requirements:
        strengths.append("필수 요건 중 실제 경험으로 뒷받침할 수 있는 항목이 있습니다.")
    if not strengths:
        strengths.append("현재 텍스트만으로는 강점 매칭 근거가 부족합니다.")
    return strengths


def _recommendations(
    missing_skills: list[str], weak_requirements: list[str], preferred: list[str]
) -> list[str]:
    recommendations: list[str] = []

    for skill in missing_skills[:5]:
        recommendations.append(f"'{skill}' 관련 프로젝트 경험, 코드 링크, 성과 지표를 포트폴리오에 보강하세요.")

    for requirement in weak_requirements[:3]:
        recommendations.append(f"필수 요건 '{requirement}'에 대응하는 경험을 자기소개서에 명시하세요.")

    if preferred:
        recommendations.append("우대사항과 맞닿은 경험은 별도 섹션으로 강조하면 점수 상승 여지가 큽니다.")

    if not recommendations:
        recommendations.append("현재 공고와 잘 맞습니다. 프로젝트 성과를 수치로 더 선명하게 보여주세요.")

    return recommendations
