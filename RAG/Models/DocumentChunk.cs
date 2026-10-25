namespace RAG.Models
{
    public sealed class DocumentChunk
    {
        public Guid ChunkId { get; set; } = Guid.NewGuid();
        public Guid SourceDocumentId { get; set; }
        public string SourceFileName { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public string Text { get; set; } = string.Empty;
        public int CharacterCount => Text.Length;
        public int? PageNumber { get; set; }
        public string? Heading { get; set; }
    }

}
