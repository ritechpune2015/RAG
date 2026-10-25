namespace RAG.Models
{
    public class RetrievedChunk
    {
        public Guid ChunkId { get; set; }
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public string Text { get; set; } = string.Empty;
        public double Score { get; set; }

    }
}
