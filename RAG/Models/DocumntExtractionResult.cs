namespace RAG.Models
{
    public sealed class DocumentExtractionResult
    {
        public string Text { get; set; } = string.Empty;

        public int? PageCount { get; set; }
    }

}
