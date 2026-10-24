namespace RAG.Models
{
    public sealed class DocumentPage
    {
        public Guid SourceDocumentId { get; set; }

        public string SourceFileName { get; set; } = string.Empty;

        public int PageNumber { get; set; }

        public string Text { get; set; } = string.Empty;
    }

}
