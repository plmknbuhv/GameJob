from __future__ import annotations

import re

from .models import JobPosting


GAME_SKILLS = {
    "unity": ["unity", "유니티", "c#"],
    "unreal": ["unreal", "언리얼", "ue4", "ue5", "c++"],
    "csharp": ["c#", "c sharp", "씨샵"],
    "cplusplus": ["c++", "cpp"],
    "python": ["python", "파이썬"],
    "java": ["java", "자바"],
    "javascript": ["javascript", "typescript", "react", "node"],
    "server": ["server", "backend", "서버", "백엔드", "api"],
    "network": ["network", "socket", "tcp", "udp", "네트워크", "소켓"],
    "database": ["mysql", "postgresql", "redis", "mongodb", "db", "database", "데이터베이스"],
    "liveops": ["liveops", "라이브", "운영", "지표", "로그", "analytics"],
    "game_design": ["기획", "밸런스", "레벨 디자인", "시스템 디자인"],
    "art": ["3d", "maya", "blender", "모델링", "애니메이션", "원화"],
    "qa": ["qa", "테스트", "버그", "검증"],
}

SECTION_PATTERNS = {
    "required": re.compile(r"(필수|자격|지원\s*자격|requirements?)", re.IGNORECASE),
    "preferred": re.compile(r"(우대|preferred|plus|nice\s*to\s*have)", re.IGNORECASE),
    "experience": re.compile(r"(경력|experience|년\s*이상)", re.IGNORECASE),
}


def parse_job_posting(text: str) -> JobPosting:
    normalized = _normalize_text(text)
    lines = [line.strip(" -ㆍ*•\t") for line in normalized.splitlines() if line.strip()]

    title = _guess_title(lines)
    sections = _collect_sections(lines)
    skills = extract_skills(normalized)

    required = sections["required"] or _keyword_lines(lines, SECTION_PATTERNS["required"])
    preferred = sections["preferred"] or _keyword_lines(lines, SECTION_PATTERNS["preferred"])
    experience = sections["experience"] or _keyword_lines(lines, SECTION_PATTERNS["experience"])

    return JobPosting(
        title=title,
        required=_dedupe(required),
        preferred=_dedupe(preferred),
        skills=skills,
        experience=_dedupe(experience),
        raw_text=text.strip(),
    )


def extract_skills(text: str) -> list[str]:
    lower = text.lower()
    found: list[str] = []
    for skill, aliases in GAME_SKILLS.items():
        if any(alias.lower() in lower for alias in aliases):
            found.append(skill)
    return found


def _normalize_text(text: str) -> str:
    text = text.replace("\r\n", "\n").replace("\r", "\n")
    text = re.sub(r"\n{3,}", "\n\n", text)
    return text.strip()


def _guess_title(lines: list[str]) -> str:
    for line in lines[:8]:
        if len(line) <= 80 and not line.endswith(":") and not SECTION_PATTERNS["required"].search(line):
            return line
    return "게임 채용공고"


def _collect_sections(lines: list[str]) -> dict[str, list[str]]:
    sections = {"required": [], "preferred": [], "experience": []}
    current: str | None = None

    for line in lines:
        matched_section = _section_for_line(line)
        if matched_section and len(line) <= 40:
            current = matched_section
            continue
        if current:
            if _section_for_line(line) and len(line) <= 40:
                current = _section_for_line(line)
                continue
            sections[current].append(line)

    return sections


def _section_for_line(line: str) -> str | None:
    for section, pattern in SECTION_PATTERNS.items():
        if pattern.search(line):
            return section
    return None


def _keyword_lines(lines: list[str], pattern: re.Pattern[str]) -> list[str]:
    return [line for line in lines if pattern.search(line)]


def _dedupe(items: list[str]) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for item in items:
        key = item.lower()
        if key not in seen:
            seen.add(key)
            result.append(item)
    return result
