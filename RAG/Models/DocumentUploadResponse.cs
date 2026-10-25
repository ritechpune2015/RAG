namespace RAG.Models
{
    public sealed class DocumentUploadResponse
    {
        public Guid Id { get; set; }

        public string OriginalFileName { get; set; } = string.Empty;

        public string FileType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public int? PageCount { get; set; }

        public int CharacterCount { get; set; }

        public string Status { get; set; } = string.Empty;

        // Number of chunks successfully indexed in Qdrant.
        public int ChunkCount { get; set; }

        // Additional information about the indexing operation.
        public string Message { get; set; } = string.Empty;

    }

}
