using System.Net;
using System.Text.RegularExpressions;

namespace GameJobWeb.Services;

public sealed class HtmlTextExtractor
{
    public string ToText(string html)
    {
        var withoutScripts = Regex.Replace(html, "<(script|style|noscript)[\\s\\S]*?</\\1>", " ", RegexOptions.IgnoreCase);
        var withBreaks = Regex.Replace(withoutScripts, "</?(p|br|li|div|section|h[1-6]|tr)[^>]*>", "\n", RegexOptions.IgnoreCase);
        var withoutTags = Regex.Replace(withBreaks, "<[^>]+>", " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        var compactLines = decoded
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => Regex.Replace(line, "\\s{2,}", " "))
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join('\n', compactLines);
    }

    public async Task<string> ExtractFromFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("채용공고 HTML 파일이 비어 있습니다.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".htm", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("채용공고 파일은 .html, .htm, .txt 형식만 업로드할 수 있습니다.");
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync(cancellationToken);
        return extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) ? content : ToText(content);
    }
}
