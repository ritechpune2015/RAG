using RAG.Models;

namespace RAG.Services.Document
{
    public interface IDocumentLoadingService
    {
        Task<LoadedDocument> LoadAsync(IFormFile file,
CancellationToken cancellationToken);

        LoadedDocument? GetById(Guid id);

        IReadOnlyCollection<LoadedDocument> GetAll();


    }
}
