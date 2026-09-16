# GameJob

게임업계 채용공고 HTML 파일과 자기소개서 PDF 파일을 비교해 적합도, 강점, 부족한 역량, 보완 액션을 분석하는 로컬 웹 프로그램입니다.

## 현재 버전

- ASP.NET Core Razor Pages 웹앱
- 로컬호스트 실행
- 채용공고 HTML 파일 업로드
- 자기소개서 PDF 업로드
- 공고 요건 파싱, 기술 키워드 추출, 적합도 점수 산출
- 매칭된 기술, 부족한 기술, 강점, 보완 제안 표시

## 실행 방법

Visual Studio에서 실행하려면 `GameJob.slnx`를 열고 `GameJobWeb` 프로젝트를 시작하세요.

터미널에서 실행하려면:

```powershell
cd C:\Users\plmkn\Desktop\School\Server3
dotnet run --project GameJobWeb\GameJobWeb.csproj
```

브라우저에서 아래 주소로 접속한 뒤, 채용공고 HTML 파일과 자기소개서 PDF 파일을 업로드합니다.

```text
http://localhost:5287
```

HTTPS 프로필로 실행하면 Visual Studio 설정에 따라 아래 주소도 사용할 수 있습니다.

```text
https://localhost:7165
```

## 프로젝트 구조

```text
GameJob.slnx
GameJobWeb/
  GameJobWeb.csproj
  Program.cs
  Models/
    FitReport.cs
  Services/
    JobPostingParser.cs
    PdfResumeReader.cs
    FitAnalyzer.cs
    HtmlTextExtractor.cs
  Pages/
    Index.cshtml
    Index.cshtml.cs
  wwwroot/
    css/site.css
```

## 주요 서비스

- `HtmlTextExtractor`: 업로드한 채용공고 HTML 파일을 텍스트로 변환
- `JobPostingParser`: 필수 요건, 우대사항, 기술 키워드 파싱
- `PdfResumeReader`: 업로드된 자기소개서 PDF에서 텍스트 추출
- `FitAnalyzer`: 채용공고와 자기소개서를 비교해 적합도 리포트 생성

## 참고

PDF가 스캔 이미지로만 구성되어 있으면 텍스트 추출이 되지 않을 수 있습니다. 이 경우 OCR 처리된 PDF를 사용해야 합니다.

게임잡 공고 페이지는 브라우저에서 연 뒤 `Ctrl+S`로 HTML 파일로 저장해서 업로드하세요.

기존 Python CLI 파일은 이전 버전 기록으로 남겨두었습니다. 현재 요청 기능은 `GameJobWeb`에서 동작합니다.
