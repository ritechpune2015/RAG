using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RAG.Models;
using RAG.Services.Document;

namespace RAG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class DocumentsController : ControllerBase
    {
        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
            ".pdf",
            ".docx",
            ".txt"
            };

        private readonly IDocumentLoadingService _documentLoadingService;
        private readonly DocumentIndexingService _indexingService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocumentsController> _logger;


        public DocumentsController(
    IDocumentLoadingService documentLoadingService,
    DocumentIndexingService indexingService,
    IConfiguration configuration,
    ILogger<DocumentsController> logger)
        {
            _documentLoadingService = documentLoadingService;
            _indexingService = indexingService;
            _configuration = configuration;
            _logger = logger;
        }


        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(11 * 1024 * 1024)]
        public async Task<ActionResult<DocumentUploadResponse>> Upload(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Please select a non-empty file."
                });
            }

            var maxFileSize = _configuration.GetValue<long>(
                "DocumentStorage:MaxFileSizeBytes",
                10 * 1024 * 1024);

            if (file.Length > maxFileSize)
            {
                return BadRequest(new
                {
                    message = $"The file cannot exceed {maxFileSize} bytes."
                });
            }

            var originalFileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalFileName);

            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(new
                {
                    message = "Only PDF, DOCX and TXT files are supported."
                });
            }

            var documentId = Guid.NewGuid();

            var uploadDirectory = _configuration[
                "DocumentStorage:UploadDirectory"] ?? "uploads";

            var absoluteUploadDirectory = Path.GetFullPath(
                Path.Combine(
                    _configuration["DocumentStorage:RootPath"]
                        ?? AppContext.BaseDirectory,
                    uploadDirectory));

            Directory.CreateDirectory(absoluteUploadDirectory);

            // Never use the supplied file name as the storage path.
            var storedFileName = $"{documentId:N}{extension}";

            var storedFilePath = Path.Combine(
                absoluteUploadDirectory,
                storedFileName);

            try
            {
                // Step 1: Save the original file.
                await using (var outputStream = new FileStream(
                    storedFilePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true))
                {
                    await file.CopyToAsync(
                        outputStream, cancellationToken);
                }

                // Step 2: Extract, chunk, embed and store in Qdrant.

                var document = await _documentLoadingService.LoadAsync(
           file,
           cancellationToken);

                // Step 3: Chunk the extracted text and index it in Qdrant.
                var chunkCount = await _indexingService.IndexDocumentAsync(
                    document.ExtractedText,
                    document.OriginalFileName,
                    document.Id,
                    cancellationToken);

                // Step 3: Return the indexing result.
                return Ok(new DocumentUploadResponse
                {
                    Id = documentId,
                    OriginalFileName = originalFileName,
                    FileType = extension,
                    FileSize = file.Length,
                    PageCount = null, // Populate this when page-count extraction is implemented.
                    CharacterCount = 0, // Populate this with the extracted text length.
                    Status = "Indexed",
                    ChunkCount = chunkCount,
                    Message = "Document uploaded and indexed successfully."
                });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // A production implementation should record the indexing
                // status and reconcile any partial Qdrant writes.
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Document indexing failed for document {DocumentId}",
                    documentId);

                // Best-effort cleanup of this upload's Qdrant points.
                try
                {
                    // If the vector store was injected here, we could
                    // delete all points for documentId.
                    // The complete cleanup implementation is discussed below.
                }
                catch (Exception cleanupException)
                {
                    _logger.LogError(
                        cleanupException,
                        "Cleanup failed for document {DocumentId}",
                        documentId);
                }

                // Best-effort cleanup of the original file.
                try
                {
                    if (System.IO.File.Exists(storedFilePath))
                    {
                        System.IO.File.Delete(storedFilePath);
                    }
                }
                catch (Exception cleanupException)
                {
                    _logger.LogError(
                        cleanupException,
                        "Could not remove failed upload {DocumentId}",
                        documentId);
                }

                return Problem(
                    title: "Document indexing failed",
                    detail: "The file could not be indexed. Check the application logs.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
