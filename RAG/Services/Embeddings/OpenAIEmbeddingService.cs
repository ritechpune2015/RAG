using OpenAI.Embeddings;

namespace RAG.Services.Embeddings
{
    public class OpenAIEmbeddingService : IEmbeddingService
    {
        private readonly EmbeddingClient _client;

        public OpenAIEmbeddingService(EmbeddingClient client)
        {
            _client = client;
        }

        public async Task<float[]> GenerateAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException(
                    "Text cannot be empty.",
                    nameof(text));
            }

            var embedding = await _client.GenerateEmbeddingAsync(
                text,
                cancellationToken: cancellationToken);

            return embedding.Value.ToFloats().ToArray();
        }

        public async Task<IReadOnlyList<float[]>> GenerateBatchAsync(
            IReadOnlyList<string> texts,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(texts);

            if (texts.Count == 0)
            {
                return Array.Empty<float[]>();
            }

            if (texts.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException(
                    "Every text must contain non-whitespace content.",
                    nameof(texts));
            }

            var result = await _client.GenerateEmbeddingsAsync(
                texts,
                cancellationToken: cancellationToken);

            return result.Value
                .OrderBy(item => item.Index)
                .Select(item => item.ToFloats().ToArray())
                .ToArray();
        }

    }
}
