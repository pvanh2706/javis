using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlAgilityPack;
using System.Text;

namespace JavisApi.Services;

/// <summary>
/// Extracts plain text from PDF, Word (.docx), HTML, and URLs.
/// </summary>
public class KbService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public KbService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public string ExtractPdf(Stream stream)
    {
        using var pdf = PdfDocument.Open(stream);
        var sb = new StringBuilder();

        foreach (var page in pdf.GetPages())
            sb.AppendLine(page.Text);

        return sb.ToString().Trim();
    }

    public string ExtractDocx(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return "";

        var sb = new StringBuilder();
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            sb.AppendLine(paragraph.InnerText);
        }

        return sb.ToString().Trim();
    }

    public string ExtractHtml(string html)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var nodesToRemove = htmlDoc.DocumentNode.SelectNodes("//script|//style");
        if (nodesToRemove is not null)
        {
            foreach (var node in nodesToRemove)
                node.Remove();
        }

        var text = htmlDoc.DocumentNode.InnerText;
        // Normalize whitespace
        return System.Text.RegularExpressions.Regex
            .Replace(text, @"\s{2,}", "\n")
            .Trim();
    }

    public async Task<string> ExtractUrlAsync(string url)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "JavisBot/1.0");

        var html = await client.GetStringAsync(url);
        return ExtractHtml(html);
    }

    public async Task<string> ExtractFromFileAsync(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => ExtractPdf(stream),
            ".docx" => ExtractDocx(stream),
            ".html" or ".htm" => ExtractHtml(await new StreamReader(stream).ReadToEndAsync()),
            ".txt" or ".md" => await new StreamReader(stream).ReadToEndAsync(),
            _ => ""
        };
    }
}
