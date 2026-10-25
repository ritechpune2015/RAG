using Qdrant.Client;
using Qdrant.Client.Grpc;
using RAG.Models;

namespace RAG.Services.Embeddings
{
    public sealed class QdrantVectorStore : IQudrantVectorStore
    {
        private readonly QdrantClient _client;
        private readonly IEmbeddingService _embeddingService;
        private readonly string _collectionName;

        public QdrantVectorStore(
            QdrantClient client,
            IEmbeddingService embeddingService,
            IConfiguration configuration)
        {
            _client = client;
            _embeddingService = embeddingService;

            _collectionName =
                configuration["Qdrant:CollectionName"]
                ?? "course_documents";
        }

        public async Task StoreChunksAsync(IReadOnlyList<DocumentChunk> chunks,CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(chunks);

            if (chunks.Count == 0)
            {
                return;
            }

            // 1. Generate embeddings for the document chunks.
            var texts = chunks.Select(chunk => chunk.Text).ToList();

            var vectors =await _embeddingService.GenerateBatchAsync(
                    texts, cancellationToken);

            if (vectors.Count != chunks.Count)
            {
                throw new InvalidOperationException(
                    "The number of embeddings does not match the number of chunks.");
            }

            // 2. Create Qdrant points.
            var points = new List<PointStruct>(chunks.Count);

            for (int i = 0; i < chunks.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var chunk = chunks[i];
                var vector = vectors[i];

                if (vector.Length == 0)
                {
                    throw new InvalidOperationException(
                        "An embedding vector cannot be empty.");
                }

                points.Add(new PointStruct
                {
                    Id = chunk.ChunkId,
                    Vectors = vector,

                    Payload =
                    {
                    ["documentId"] = chunk.SourceDocumentId.ToString(),
                    ["fileName"] = chunk.SourceFileName,
                    ["chunkIndex"] = chunk.ChunkIndex,
                    ["text"] = chunk.Text
                    }
                });
            }

            // 3. Store points in Qdrant.
            await _client.UpsertAsync(_collectionName,points,cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(string query, int topK, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException(
                    "Query cannot be empty.",
                    nameof(query));
            }

            if (topK is < 1 or > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(topK),
                    "topK must be between 1 and 100.");
            }

            // 1. Generate the query embedding.
            var queryVector =
                await _embeddingService.GenerateAsync(
                    query, cancellationToken);

            // 2. Search Qdrant.
            var points = await _client.SearchAsync(
                _collectionName,
                queryVector,
                limit: (ulong)topK,
                cancellationToken: cancellationToken);

            // 3. Convert Qdrant results into application models.
            var results = new List<RetrievedChunk>();

            foreach (var point in points)
            {
                var payload = point.Payload;

                string GetString(string key)
                {
                    return payload.TryGetValue(key, out var value)
                        ? value.StringValue
                        : string.Empty;
                }

                int GetInt(string key)
                {
                    return payload.TryGetValue(key, out var value)
                        ? (int)value.IntegerValue
                        : 0;
                }

                if (!Guid.TryParse(GetString("documentId"), out var documentId))
                {
                    continue;
                }

                if (!Guid.TryParse(point.Id.Uuid, out var chunkId))
                {
                    // This example assumes UUID point IDs.
                    continue;
                }

                results.Add(new RetrievedChunk
                {
                    ChunkId = chunkId,
                    DocumentId = documentId,
                    FileName = GetString("fileName"),
                    ChunkIndex = GetInt("chunkIndex"),
                    Text = GetString("text"),
                    Score = point.Score
                });
            }

            return results;
        }

        public async Task DeleteDocumentAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            var filter = new Filter
            {
                Must =
    {
        new Qdrant.Client.Grpc.Condition
        {
            Field = new FieldCondition
            {
                Key = "documentId",
                Match = new Qdrant.Client.Grpc.Match
                {
                    Keyword = documentId.ToString()
                }
            }
        }
    }

            };

            await _client.DeleteAsync(
                _collectionName,
                filter,
                cancellationToken: cancellationToken);
        }
    }

}
