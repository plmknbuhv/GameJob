from __future__ import annotations

from pathlib import Path


class PdfReadError(RuntimeError):
    """Raised when a PDF cannot be read as text."""


def extract_pdf_text(path: str) -> str:
    pdf_path = Path(path)
    if not pdf_path.exists():
        raise PdfReadError(f"PDF 파일을 찾을 수 없습니다: {pdf_path}")
    if pdf_path.suffix.lower() != ".pdf":
        raise PdfReadError(f"PDF 파일만 입력할 수 있습니다: {pdf_path}")

    reader_class = _load_pdf_reader()
    try:
        reader = reader_class(str(pdf_path))
        pages = getattr(reader, "pages", [])
        text_parts = []
        for page in pages:
            text = page.extract_text() or ""
            if text.strip():
                text_parts.append(text.strip())
    except Exception as exc:
        raise PdfReadError(f"PDF 텍스트를 읽는 중 오류가 발생했습니다: {exc}") from exc

    text = "\n\n".join(text_parts).strip()
    if not text:
        raise PdfReadError(
            "PDF에서 텍스트를 추출하지 못했습니다. 스캔 이미지 PDF라면 OCR 처리가 먼저 필요합니다."
        )
    return text


def _load_pdf_reader():
    try:
        from pypdf import PdfReader

        return PdfReader
    except ImportError:
        pass

    try:
        from PyPDF2 import PdfReader

        return PdfReader
    except ImportError as exc:
        raise PdfReadError(
            "PDF 분석에는 pypdf가 필요합니다. 다음 명령으로 설치하세요: python -m pip install -r requirements.txt"
        ) from exc
