namespace RAG.Models
{
    public sealed class LoadedDocument
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string OriginalFileName { get; set; } = string.Empty;

        public string FileType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public string ContentHash { get; set; } = string.Empty;

        public int? PageCount { get; set; }

        public string ExtractedText { get; set; } = string.Empty;

        public DateTime UploadedAtUtc { get; set; } =
            DateTime.UtcNow;

        public string Status { get; set; } = "Loaded";
    }

}
