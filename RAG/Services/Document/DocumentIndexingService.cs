using RAG.Services.Chunking;
using RAG.Services.Embeddings;

namespace RAG.Services.Document
{
    public sealed class DocumentIndexingService
    {
        private readonly ITextChunker _textChunker;
        private readonly IQudrantVectorStore _vectorStore;

        public DocumentIndexingService(ITextChunker textChunker, IQudrantVectorStore vectorStore)
        {
            _textChunker = textChunker;
            _vectorStore = vectorStore;
        }

        public async Task<int> IndexDocumentAsync(
            string extractedText,
            string fileName,
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            // Step 1: Validate the extracted text.
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                throw new InvalidOperationException(
                    "The document contains no extractable text.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Step 2: Split the extracted text into chunks.
            var chunks = _textChunker.Recursive(
                extractedText,
                documentId,
                fileName,
                maxChunkCharacters: 1200,
                overlap: 200);

            if (chunks.Count == 0)
            {
                throw new InvalidOperationException(
                    "No chunks were created from the document.");
            }

            // Step 3: Generate embeddings and store chunks in Qdrant.
            await _vectorStore.StoreChunksAsync(
                chunks,
                cancellationToken);

            return chunks.Count;
        }
    }
}
