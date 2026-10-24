namespace RAG.Models
{
    public sealed class ChunkingRequest
    {
        public Guid SourceDocumentId { get; set; }

        public string SourceFileName { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public int ChunkSize { get; set; } = 500;

        public int Overlap { get; set; } = 50;
    }

}
