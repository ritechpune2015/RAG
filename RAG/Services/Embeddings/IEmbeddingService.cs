namespace RAG.Services.Embeddings
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<float[]>> GenerateBatchAsync(
            IReadOnlyList<string> texts,
            CancellationToken cancellationToken = default);

    }
}
