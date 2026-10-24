using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using HtmlAgilityPack;
using RAG.Models;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace RAG.Services.Document
{
    public sealed class DocumentTextExtractor: IDocumentTextExtractor
    {
        public Task<DocumentExtractionResult> ExtractAsync(string fileName,
            byte[] fileContent, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();

            DocumentExtractionResult result = extension switch
            {
                ".pdf" => ExtractPdf(fileContent),

                ".docx" => ExtractDocx(fileContent),

                ".txt" => ExtractTxt(fileContent),

                ".html" or ".htm" => ExtractHtml(fileContent),

                _ => throw new ArgumentException(
                    "Unsupported document format.")
            };

            result.Text = CleanText(result.Text);

            return Task.FromResult(result);
        }

        private static DocumentExtractionResult ExtractPdf(byte[] fileContent)
        {
            using var stream = new MemoryStream(fileContent);

            using var pdf = PdfDocument.Open(stream);

            var text = new StringBuilder();

            foreach (var page in pdf.GetPages())
            {
                text.AppendLine(page.Text);
            }

            return new DocumentExtractionResult
            {
                Text = text.ToString(),
                PageCount = pdf.NumberOfPages
            };
        }

        private static DocumentExtractionResult ExtractDocx(byte[] fileContent)
        {
            using var stream = new MemoryStream(fileContent);

            using var document =
                WordprocessingDocument.Open(stream, false);

            var body = document.MainDocumentPart?.Document.Body;

            if (body is null)
            {
                return new DocumentExtractionResult();
            }

            var text = new StringBuilder();

            foreach (var element in body.Elements())
            {
                if (element is Paragraph paragraph)
                {
                    text.AppendLine(paragraph.InnerText);
                }
                else if (element is Table table)
                {
                    foreach (var row in table.Elements<TableRow>())
                    {
                        var cells = row.Elements<TableCell>()
                            .Select(cell => cell.InnerText);

                        text.AppendLine(
                            string.Join(" | ", cells));
                    }
                }
            }

            return new DocumentExtractionResult
            {
                Text = text.ToString()
            };
        }

        private static DocumentExtractionResult ExtractTxt(byte[] fileContent)
        {
            var text = Encoding.UTF8.GetString(fileContent);

            return new DocumentExtractionResult
            {
                Text = text
            };
        }

        private static DocumentExtractionResult ExtractHtml(byte[] fileContent)
        {
            var html = Encoding.UTF8.GetString(fileContent);

            var htmlDocument = new HtmlDocument();

            htmlDocument.LoadHtml(html);

            // Remove elements that usually do not contain
            // useful document content.
            var unwantedNodes = htmlDocument.DocumentNode.SelectNodes(
                "//script|//style|//noscript");

            if (unwantedNodes is not null)
            {
                foreach (var node in unwantedNodes)
                {
                    node.Remove();
                }
            }

            var text = HtmlEntity.DeEntitize(
                htmlDocument.DocumentNode.InnerText);

            return new DocumentExtractionResult
            {
                Text = text
            };
        }

        private static string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            // Normalize line endings.
            text = text.Replace("\r\n", "\n")
                       .Replace("\r", "\n");

            // Replace tabs and repeated spaces.
            text = Regex.Replace(text, @"[ \t]+", " ");

            // Remove excessive blank lines.
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            return text.Trim();
        }
    }

}
