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
        private readonly IDocumentLoadingService _loadingService;

        public DocumentsController(IDocumentLoadingService loadingService)
        {
            _loadingService = loadingService;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<ActionResult<DocumentUploadResponse>> Upload(
            IFormFile file,CancellationToken cancellationToken)
        {
            try
            {
                var document = await _loadingService.LoadAsync(
                    file,
                    cancellationToken);

                var response = new DocumentUploadResponse
                {
                    Id = document.Id,
                    OriginalFileName = document.OriginalFileName,
                    FileType = document.FileType,
                    FileSize = document.FileSize,
                    PageCount = document.PageCount,
                    CharacterCount = document.ExtractedText.Length,
                    Status = document.Status
                };

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = document.Id },
                    response);
            }
            catch (DuplicateDocumentException ex)
            {
                return Conflict(new
                {
                    message = ex.Message,
                    existingDocumentId = ex.ExistingDocumentId,
                    existingFileName = ex.ExistingFileName
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // In production, log the exception details
                // through ILogger without exposing internal
                // implementation details to the client.
                return Problem(
                    title: "Document loading failed.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{id:guid}")]
        public ActionResult<DocumentUploadResponse> GetById(
            Guid id)
        {
            var document = _loadingService.GetById(id);

            if (document is null)
            {
                return NotFound();
            }

            return Ok(new DocumentUploadResponse
            {
                Id = document.Id,
                OriginalFileName = document.OriginalFileName,
                FileType = document.FileType,
                FileSize = document.FileSize,
                PageCount = document.PageCount,
                CharacterCount = document.ExtractedText.Length,
                Status = document.Status
            });
        }

        [HttpGet("{id:guid}/text")]
        public ActionResult<string> GetText(Guid id)
        {
            var document = _loadingService.GetById(id);

            if (document is null)
            {
                return NotFound();
            }

            return Ok(new
            {
                document.Id,
                document.OriginalFileName,
                document.ExtractedText
            });
        }

        [HttpGet]
        public ActionResult<IReadOnlyCollection<DocumentUploadResponse>>
            GetAll()
        {
            var documents = _loadingService.GetAll()
                .Select(document => new DocumentUploadResponse
                {
                    Id = document.Id,
                    OriginalFileName = document.OriginalFileName,
                    FileType = document.FileType,
                    FileSize = document.FileSize,
                    PageCount = document.PageCount,
                    CharacterCount = document.ExtractedText.Length,
                    Status = document.Status
                })
                .ToArray();

            return Ok(documents);
        }
    }

}
