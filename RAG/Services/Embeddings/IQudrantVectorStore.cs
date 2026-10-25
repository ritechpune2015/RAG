using RAG.Models;

namespace RAG.Services.Embeddings
{
    public interface IQudrantVectorStore
    {
        Task StoreChunksAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RetrievedChunk>> SearchAsync(string query,int topK, CancellationToken cancellationToken = default);

        Task DeleteDocumentAsync(Guid documentId,CancellationToken cancellationToken = default);

    }
}
