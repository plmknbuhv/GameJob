using GameJobWeb.Models;
using GameJobWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GameJobWeb.Pages;

public class IndexModel(
    JobPostingParser parser,
    FitAnalyzer analyzer,
    PdfResumeReader pdfReader,
    HtmlTextExtractor htmlTextExtractor) : PageModel
{
    [BindProperty]
    public IFormFile? JobHtml { get; set; }

    [BindProperty]
    public IFormFile? ResumePdf { get; set; }

    public FitReport? Report { get; private set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
    }

    public async Task OnPostAsync(CancellationToken cancellationToken)
    {
        if (JobHtml is null)
        {
            ErrorMessage = "채용공고 HTML 파일을 업로드하세요.";
            return;
        }

        if (ResumePdf is null)
        {
            ErrorMessage = "자기소개서 PDF 파일을 업로드하세요.";
            return;
        }

        try
        {
            var jobText = await htmlTextExtractor.ExtractFromFileAsync(JobHtml, cancellationToken);
            var resumeText = await pdfReader.ExtractTextAsync(ResumePdf, cancellationToken);
            var job = parser.Parse(jobText);
            Report = analyzer.Analyze(job, resumeText);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
        {
            ErrorMessage = ex.Message;
        }
    }
}
