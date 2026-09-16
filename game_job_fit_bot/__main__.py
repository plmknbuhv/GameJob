from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from .analyzer import analyze_fit
from .fetcher import fetch_url_text
from .parser import parse_job_posting
from .pdf_reader import PdfReadError, extract_pdf_text


def main() -> None:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")

    args = _parse_args()
    if not any([args.job, args.job_url, args.profile, args.resume_pdf]):
        args = _interactive_args(args)

    try:
        job_text = fetch_url_text(args.job_url) if args.job_url else Path(args.job).read_text(encoding="utf-8")
        profile_text = (
            extract_pdf_text(args.resume_pdf)
            if args.resume_pdf
            else Path(args.profile).read_text(encoding="utf-8")
        )
    except PdfReadError as exc:
        print(f"오류: {exc}", file=sys.stderr)
        raise SystemExit(1) from exc

    job = parse_job_posting(job_text)
    report = analyze_fit(job, profile_text)

    if args.json:
        Path(args.json).write_text(
            json.dumps(report.to_dict(), ensure_ascii=False, indent=2),
            encoding="utf-8",
        )

    print(_format_report(report.to_dict()))


def _parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="GameJob - 게임업계 채용공고/자기소개서 PDF 적합도 분석")
    source = parser.add_mutually_exclusive_group()
    source.add_argument("--job", help="채용공고 텍스트 파일 경로")
    source.add_argument("--job-url", help="채용공고 URL")
    profile = parser.add_mutually_exclusive_group()
    profile.add_argument("--profile", help="포트폴리오/자기소개서 텍스트 파일 경로")
    profile.add_argument("--resume-pdf", help="자기소개서 PDF 파일 경로")
    parser.add_argument("--json", help="JSON 분석 결과 저장 경로")
    args = parser.parse_args()
    if any([args.job, args.job_url, args.profile, args.resume_pdf]):
        if not (args.job or args.job_url):
            parser.error("--job 또는 --job-url 중 하나를 입력하세요.")
        if not (args.profile or args.resume_pdf):
            parser.error("--profile 또는 --resume-pdf 중 하나를 입력하세요.")
    return args


def _interactive_args(args: argparse.Namespace) -> argparse.Namespace:
    print("GameJob 분석을 시작합니다.")
    args.job_url = input("채용공고 링크를 입력하세요: ").strip()
    args.resume_pdf = input("자기소개서 PDF 파일 경로를 입력하세요: ").strip().strip('"')
    if not args.job_url or not args.resume_pdf:
        print("오류: 채용공고 링크와 자기소개서 PDF 경로가 모두 필요합니다.", file=sys.stderr)
        raise SystemExit(1)
    return args


def _format_report(data: dict) -> str:
    job = data["job"]
    lines = [
        f"직무: {job['title']}",
        f"적합도: {data['score']}점 / {data['level']}",
        "",
        "매칭된 기술:",
        _items(data["matched_skills"]),
        "",
        "부족한 기술:",
        _items(data["missing_skills"]),
        "",
        "강점:",
        _items(data["strengths"]),
        "",
        "보완 제안:",
        _items(data["recommendations"]),
    ]
    return "\n".join(lines)


def _items(items: list[str]) -> str:
    if not items:
        return "- 없음"
    return "\n".join(f"- {item}" for item in items)


if __name__ == "__main__":
    main()
