using RAG.Models;

namespace RAG.Services.Chunking
{
    public interface ITextChunker
    {
        List<DocumentChunk> FixedSize(
      string text,
      Guid sourceDocumentId,
      string sourceFileName,
      int chunkSize,
      int overlap);

        List<DocumentChunk> SentenceBased(
            string text,
            Guid sourceDocumentId,
            string sourceFileName,
            int maxChunkCharacters);

        List<DocumentChunk> ParagraphBased(
            string text,
            Guid sourceDocumentId,
            string sourceFileName,
            int maxChunkCharacters);

        List<DocumentChunk> Recursive(
            string text,
            Guid sourceDocumentId,
            string sourceFileName,
            int maxChunkCharacters,
            int overlap = 0);

        List<DocumentChunk> ChunkPages(
            IEnumerable<DocumentPage> pages,
            int maxChunkCharacters);


    }
}
