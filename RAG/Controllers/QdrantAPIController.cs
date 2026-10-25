using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RAG.Services.Qdrantsvr;

namespace RAG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QdrantAPIController : ControllerBase
    {
        private readonly IQdrantService _service;

        public QdrantAPIController(IQdrantService service)
        {
            _service = service;
        }

        [HttpPost("create-collection/{name}")]

        public async Task<IActionResult> CreateCollection(string name)

        {

            await _service.CreateCollection(name);
            return Ok("Collection Created");
        }

    }
}
