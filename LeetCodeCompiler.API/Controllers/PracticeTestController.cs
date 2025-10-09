using LeetCodeCompiler.API.Models;
using LeetCodeCompiler.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PracticeTestController : ControllerBase
    {
        private readonly IPracticeTestService _practiceTestService;

        public PracticeTestController(IPracticeTestService practiceTestService)
        {
            _practiceTestService = practiceTestService;
        }

        /// <summary>
        /// Create a new practice test
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreatePracticeTest([FromBody] CreatePracticeTestRequest request)
        {
            try
            {
                var result = await _practiceTestService.CreatePracticeTestAsync(request);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Start a practice test for a user
        /// </summary>
        [HttpGet("start")]
        public async Task<IActionResult> StartPracticeTest([FromQuery] int practiceTestId, [FromQuery] int userId)
        {
            try
            {
                var request = new StartPracticeTestRequest
                {
                    PracticeTestId = practiceTestId,
                    UserId = userId
                };

                var result = await _practiceTestService.StartPracticeTestAsync(request);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Submit practice test results
        /// </summary>
        [HttpPost("submit-result")]
        public async Task<IActionResult> SubmitPracticeTestResult([FromBody] SubmitPracticeTestResultRequest request)
        {
            try
            {
                var result = await _practiceTestService.SubmitPracticeTestResultAsync(request);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get practice test results
        /// </summary>
        [HttpGet("result")]
        public async Task<IActionResult> GetPracticeTestResult([FromQuery] int practiceTestId, [FromQuery] int userId, [FromQuery] int? attemptNumber = null)
        {
            try
            {
                var request = new GetPracticeTestResultRequest
                {
                    PracticeTestId = practiceTestId,
                    UserId = userId,
                    AttemptNumber = attemptNumber
                };

                var result = await _practiceTestService.GetPracticeTestResultAsync(request);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Validate practice test and attempt before submitting
        /// </summary>
        [HttpGet("validate")]
        public async Task<IActionResult> ValidatePracticeTest([FromQuery] int practiceTestId, [FromQuery] int userId, [FromQuery] int attemptNumber)
        {
            try
            {
                var result = await _practiceTestService.ValidatePracticeTestAsync(practiceTestId, userId, attemptNumber);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }
    }
}
