using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using HtmlAgilityPack;
using RAG.Models;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace RAG.Services.Document
{
    public sealed class DocumentTextExtractor : IDocumentTextExtractor
    {
            public Task<DocumentExtractionResult> ExtractAsync(
                string fileName,
                byte[] fileBytes,
                CancellationToken cancellationToken = default)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
                ArgumentNullException.ThrowIfNull(fileBytes);

                if (fileBytes.Length == 0)
                {
                    throw new ArgumentException(
                        "File content cannot be empty.",
                        nameof(fileBytes));
                }

                cancellationToken.ThrowIfCancellationRequested();

                var extension = System.IO.Path.GetExtension(fileName)
                    .ToLowerInvariant();

                DocumentExtractionResult result = extension switch
                {
                    ".pdf" => ExtractPdf(fileBytes),
                    ".docx" => ExtractDocx(fileBytes),
                    ".txt" => ExtractTxt(fileBytes),
                    ".html" or ".htm" => ExtractHtml(fileBytes),

                    _ => throw new NotSupportedException(
                        $"File type '{extension}' is not supported.")
                };

                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(result.Text))
                {
                    throw new InvalidOperationException(
                        "No readable text was found in the document.");
                }

                return Task.FromResult(result);
            }

            private static DocumentExtractionResult ExtractPdf(
                byte[] fileBytes)
            {
                using var stream = new MemoryStream(fileBytes);
                using var pdf = PdfDocument.Open(stream);

                var builder = new StringBuilder();

                foreach (var page in pdf.GetPages())
                {
                    builder.AppendLine(page.Text);
                }

                return new DocumentExtractionResult
                {
                    Text = builder.ToString(),
                    PageCount = pdf.NumberOfPages
                };
            }

            private static DocumentExtractionResult ExtractDocx(
                byte[] fileBytes)
            {
                using var stream = new MemoryStream(fileBytes);

                using var document =
                    WordprocessingDocument.Open(stream, false);

                var body = document.MainDocumentPart?
                    .Document?
                    .Body;

                return new DocumentExtractionResult
                {
                    Text = body?.InnerText ?? string.Empty,
                    PageCount = null
                };
            }

            private static DocumentExtractionResult ExtractTxt(
                byte[] fileBytes)
            {
                return new DocumentExtractionResult
                {
                    Text = Encoding.UTF8.GetString(fileBytes),
                    PageCount = null
                };
            }

            private static DocumentExtractionResult ExtractHtml(
                byte[] fileBytes)
            {
                var html = Encoding.UTF8.GetString(fileBytes);

                // Basic HTML-to-text conversion for demonstration.
                html = Regex.Replace(
                    html,
                    @"<(script|style)\b[^>]*>.*?</\1>",
                    " ",
                    RegexOptions.IgnoreCase |
                    RegexOptions.Singleline);

                html = Regex.Replace(
                    html,
                    @"<\s*br\s*/?\s*>|</\s*(p|div|li|h[1-6]|tr)\s*>",
                    "\n",
                    RegexOptions.IgnoreCase);

                html = Regex.Replace(html, @"<[^>]+>", " ");

                html = System.Net.WebUtility.HtmlDecode(html);

                html = Regex.Replace(html, @"[ \t]+", " ");
                html = Regex.Replace(html, @"\n\s*\n+", "\n\n");

                return new DocumentExtractionResult
                {
                    Text = html.Trim(),
                    PageCount = null
                };
            }
        }
    }
