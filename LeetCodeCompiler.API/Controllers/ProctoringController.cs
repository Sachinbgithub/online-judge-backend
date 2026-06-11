using LeetCodeCompiler.API.Models;
using LeetCodeCompiler.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/proctoring")]
    [Authorize(Policy = "AnyAuthenticated")]
    public class ProctoringController : ControllerBase
    {
        private readonly IProctoringService _proctoringService;

        public ProctoringController(IProctoringService proctoringService)
        {
            _proctoringService = proctoringService;
        }

        [HttpPost("events")]
        public async Task<IActionResult> IngestEvents([FromBody] IngestProctoringEventsRequest request)
        {
            try
            {
                var status = await _proctoringService.IngestEventsAsync(request);
                return Ok(status);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("status/{attemptId}")]
        public async Task<IActionResult> GetStatus(int attemptId, [FromQuery] int userId)
        {
            try
            {
                var status = await _proctoringService.GetStatusAsync(attemptId, userId);
                return Ok(status);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("events/{attemptId}")]
        [Authorize(Policy = "TestSetterOnly")]
        public async Task<IActionResult> GetEvents(int attemptId)
        {
            var events = await _proctoringService.GetEventsForAttemptAsync(attemptId);
            return Ok(events);
        }
    }
}
