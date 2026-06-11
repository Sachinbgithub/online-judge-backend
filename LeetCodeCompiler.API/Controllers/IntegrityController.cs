using LeetCodeCompiler.API.Models;
using LeetCodeCompiler.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/integrity")]
    [Authorize(Policy = "AnyAuthenticated")]
    public class IntegrityController : ControllerBase
    {
        private readonly IIntegrityAnalysisService _integrityService;
        private readonly IPlagiarismService _plagiarismService;

        public IntegrityController(
            IIntegrityAnalysisService integrityService,
            IPlagiarismService plagiarismService)
        {
            _integrityService = integrityService;
            _plagiarismService = plagiarismService;
        }

        [HttpPost("activity-snapshot")]
        public async Task<IActionResult> SaveActivitySnapshot([FromBody] CodeActivitySnapshotRequest request)
        {
            try
            {
                await _integrityService.SaveActivitySnapshotAsync(request);
                return Ok(new { success = true });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("flags/{attemptId}")]
        public async Task<IActionResult> GetFlags(int attemptId, [FromQuery] int userId, [FromQuery] bool isTestSetter = false)
        {
            try
            {
                var flags = await _integrityService.GetFlagsForAttemptAsync(attemptId, userId, isTestSetter);
                return Ok(flags);
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

        [HttpGet("review")]
        [Authorize(Policy = "TestSetterOnly")]
        public async Task<IActionResult> GetReviewSummary([FromQuery] int codingTestId)
        {
            var summary = await _integrityService.GetReviewSummaryAsync(codingTestId);
            return Ok(summary);
        }

        [HttpPatch("flags/{flagId}/review")]
        [Authorize(Policy = "TestSetterOnly")]
        public async Task<IActionResult> ReviewFlag(long flagId, [FromBody] ReviewIntegrityFlagRequest request, [FromQuery] int reviewedBy)
        {
            try
            {
                var flag = await _integrityService.ReviewFlagAsync(flagId, request, reviewedBy);
                if (flag == null) return NotFound();
                return Ok(flag);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("/api/plagiarism/report/{submissionId}")]
        [Authorize(Policy = "TestSetterOnly")]
        public async Task<IActionResult> GetPlagiarismReport(long submissionId)
        {
            var report = await _plagiarismService.GetReportAsync(submissionId);
            if (report == null) return NotFound(new { error = "Report not found or still pending" });
            return Ok(report);
        }
    }
}
