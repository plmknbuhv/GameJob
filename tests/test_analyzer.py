import unittest

from game_job_fit_bot.analyzer import analyze_fit
from game_job_fit_bot.parser import parse_job_posting


class AnalyzerTest(unittest.TestCase):
    def test_analyze_fit_reports_matches_and_gaps(self):
        job = parse_job_posting(
            """
            게임 서버 개발자
            필수 요건
            - Python 서버 개발 경험
            - Unreal C++ 개발 경험
            """
        )
        report = analyze_fit(job, "Python으로 REST API 서버를 개발했습니다.")

        self.assertIn("python", report.matched_skills)
        self.assertIn("unreal", report.missing_skills)
        self.assertGreater(report.score, 0)


if __name__ == "__main__":
    unittest.main()
