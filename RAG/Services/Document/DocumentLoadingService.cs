using RAG.Models;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace RAG.Services.Document
{
    public sealed class DocumentLoadingService: IDocumentLoadingService
    {
        private const long MaxFileSize = 10 * 1024 * 1024;

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
            ".pdf",
            ".docx",
            ".txt",
            ".html",
            ".htm"
            };

        private readonly IDocumentTextExtractor _extractor;

        private readonly ConcurrentDictionary<Guid, LoadedDocument>
            _documents = new();

        private readonly ConcurrentDictionary<string, Guid>
            _hashIndex = new();

        public DocumentLoadingService(IDocumentTextExtractor extractor)
        {
            _extractor = extractor;
        }

        public async Task<LoadedDocument> LoadAsync(IFormFile file,
            CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                throw new ArgumentException(
                    "Please select a non-empty file.");
            }

            if (file.Length > MaxFileSize)
            {
                throw new ArgumentException(
                    "Maximum allowed file size is 10 MB.");
            }

            var extension = Path.GetExtension(file.FileName);

            if (!AllowedExtensions.Contains(extension))
            {
                throw new ArgumentException(
                    "Supported formats are PDF, DOCX, TXT, and HTML.");
            }

            await using var inputStream = file.OpenReadStream();

            using var memoryStream = new MemoryStream();

            await inputStream.CopyToAsync(
                memoryStream,
                cancellationToken);

            var bytes = memoryStream.ToArray();

            var hash = Convert.ToHexString(
                SHA256.HashData(bytes));

            // Check whether this exact file content was loaded.
            if (_hashIndex.TryGetValue(hash, out var existingId) &&
                _documents.TryGetValue(existingId, out var existing))
            {
                throw new DuplicateDocumentException(
                    existing.Id,
                    existing.OriginalFileName);
            }

            var extraction = await _extractor.ExtractAsync(
                file.FileName,
                bytes,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(extraction.Text))
            {
                throw new ArgumentException(
                    "No readable text was found in the document.");
            }

            var document = new LoadedDocument
            {
                Id = Guid.NewGuid(),
                OriginalFileName = Path.GetFileName(file.FileName),
                FileType = extension.ToLowerInvariant(),
                FileSize = file.Length,
                ContentHash = hash,
                PageCount = extraction.PageCount,
                ExtractedText = extraction.Text,
                UploadedAtUtc = DateTime.UtcNow,
                Status = "Loaded"
            };

            // Reserve the hash so concurrent uploads of identical
            // content do not both pass the duplicate check.
            if (!_hashIndex.TryAdd(hash, document.Id))
            {
                if (_hashIndex.TryGetValue(hash, out existingId) &&
                    _documents.TryGetValue(existingId, out existing))
                {
                    throw new DuplicateDocumentException(
                        existing.Id,
                        existing.OriginalFileName);
                }

                throw new InvalidOperationException(
                    "Could not register the document.");
            }

            _documents[document.Id] = document;

            return document;
        }

        public LoadedDocument? GetById(Guid id)
        {
            return _documents.TryGetValue(id, out var document)
                ? document
                : null;
        }

        public IReadOnlyCollection<LoadedDocument> GetAll()
        {
            return _documents.Values.ToArray();
        }
    }

    public sealed class DuplicateDocumentException : Exception
    {
        public Guid ExistingDocumentId { get; }

        public string ExistingFileName { get; }

        public DuplicateDocumentException(
            Guid existingDocumentId,
            string existingFileName)
            : base("This document has already been loaded.")
        {
            ExistingDocumentId = existingDocumentId;
            ExistingFileName = existingFileName;
        }
    }

}
