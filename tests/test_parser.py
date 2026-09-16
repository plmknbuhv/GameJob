import unittest

from game_job_fit_bot.parser import parse_job_posting


class ParserTest(unittest.TestCase):
    def test_parse_job_posting_extracts_sections_and_skills(self):
        text = """
        게임 서버 개발자

        필수 요건
        - Python 서버 개발 경험
        - TCP 네트워크 지식

        우대사항
        - Redis 경험
        """

        job = parse_job_posting(text)

        self.assertEqual(job.title, "게임 서버 개발자")
        self.assertIn("Python 서버 개발 경험", job.required)
        self.assertIn("Redis 경험", job.preferred)
        self.assertIn("python", job.skills)
        self.assertIn("network", job.skills)


if __name__ == "__main__":
    unittest.main()
