using Microsoft.AspNetCore.Mvc;
using RAG.Models;
using RAG.Services.Chunking;

namespace RAG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class ChunkingController : ControllerBase
    {
        private readonly ITextChunker _chunker;

        public ChunkingController(ITextChunker chunker)
        {
            _chunker = chunker;
        }

        [HttpPost("{strategy}")]
        public ActionResult<List<DocumentChunk>> CreateChunks(string strategy, [FromBody] ChunkingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest(new
                {
                    message = "Text is required."
                });
            }

            if (request.ChunkSize <= 0 ||
                request.ChunkSize > 10000)
            {
                return BadRequest(new
                {
                    message = "ChunkSize must be between 1 and 10000."
                });
            }

            if (request.Overlap < 0 ||
                request.Overlap >= request.ChunkSize)
            {
                return BadRequest(new
                {
                    message = "Overlap must be non-negative and smaller than ChunkSize."
                });
            }

            //var strategy = Request.Query["strategy"]
            //    .ToString()
            //    .ToLowerInvariant();

            try
            {
                List<DocumentChunk> chunks = strategy switch
                {
                    "" or "fixed" => _chunker.FixedSize(
                        request.Text,
                        request.SourceDocumentId,
                        request.SourceFileName,
                        request.ChunkSize,
                        request.Overlap),

                    "sentence" => _chunker.SentenceBased(
                        request.Text,
                        request.SourceDocumentId,
                        request.SourceFileName,
                        request.ChunkSize),

                    "paragraph" => _chunker.ParagraphBased(
                        request.Text,
                        request.SourceDocumentId,
                        request.SourceFileName,
                        request.ChunkSize),

                    "recursive" => _chunker.Recursive(
                        request.Text,
                        request.SourceDocumentId,
                        request.SourceFileName,
                        request.ChunkSize,
                        request.Overlap),

                    _ => throw new ArgumentException(
                        "Supported strategies: fixed, sentence, paragraph, recursive.")
                };

                return Ok(chunks);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }

}
