using System.Text;
using UglyToad.PdfPig;

namespace GameJobWeb.Services;

public sealed class PdfResumeReader
{
    public async Task<string> ExtractTextAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("PDF 파일이 비어 있습니다.");
        }

        if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("자기소개서는 PDF 파일만 업로드할 수 있습니다.");
        }

        await using var uploadStream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await uploadStream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        using var document = PdfDocument.Open(memory);
        var builder = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            builder.AppendLine(page.Text);
            builder.AppendLine();
        }

        var text = builder.ToString().Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("PDF에서 텍스트를 추출하지 못했습니다. 스캔 이미지 PDF라면 OCR 처리가 먼저 필요합니다.");
        }

        return text;
    }
}
