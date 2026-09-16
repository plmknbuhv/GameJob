from __future__ import annotations

from dataclasses import asdict, dataclass, field


@dataclass(frozen=True)
class JobPosting:
    title: str
    required: list[str] = field(default_factory=list)
    preferred: list[str] = field(default_factory=list)
    skills: list[str] = field(default_factory=list)
    experience: list[str] = field(default_factory=list)
    raw_text: str = ""

    def to_dict(self) -> dict:
        return asdict(self)


@dataclass(frozen=True)
class FitReport:
    score: int
    level: str
    matched_skills: list[str]
    missing_skills: list[str]
    matched_requirements: list[str]
    weak_requirements: list[str]
    strengths: list[str]
    recommendations: list[str]
    job: JobPosting

    def to_dict(self) -> dict:
        data = asdict(self)
        data["job"] = self.job.to_dict()
        return data
