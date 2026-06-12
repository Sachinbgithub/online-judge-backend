using LeetCodeCompiler.API.Models;
using LeetCodeCompiler.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/question-pools")]
    [Authorize(Policy = "TestSetterOnly")]
    public class QuestionPoolController : ControllerBase
    {
        private readonly IQuestionPoolService _poolService;

        public QuestionPoolController(IQuestionPoolService poolService)
        {
            _poolService = poolService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePool([FromBody] CreateQuestionPoolRequest request)
        {
            var pool = await _poolService.CreatePoolAsync(request);
            return CreatedAtAction(nameof(GetPool), new { id = pool.Id }, pool);
        }

        [HttpGet]
        public async Task<IActionResult> GetPools([FromQuery] bool activeOnly = true)
        {
            var pools = await _poolService.GetPoolsAsync(activeOnly);
            return Ok(pools);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPool(int id)
        {
            var pool = await _poolService.GetPoolAsync(id);
            if (pool == null) return NotFound();
            return Ok(pool);
        }

        [HttpPost("{id}/problems")]
        public async Task<IActionResult> AddProblems(int id, [FromBody] List<int> problemIds)
        {
            try
            {
                var pool = await _poolService.AddProblemsToPoolAsync(id, problemIds);
                return Ok(pool);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePool(int id)
        {
            var deleted = await _poolService.DeletePoolAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
