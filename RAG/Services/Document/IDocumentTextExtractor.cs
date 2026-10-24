using RAG.Models;

namespace RAG.Services.Document
{
    public interface IDocumentTextExtractor
    {
        Task<DocumentExtractionResult> ExtractAsync(string fileName,
            byte[] fileContent,
            CancellationToken cancellationToken);
    }

}
