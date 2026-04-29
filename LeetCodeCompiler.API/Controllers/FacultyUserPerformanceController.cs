using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Services;
using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "TestSetterOnly")]
    public class FacultyUserPerformanceController : ControllerBase
    {
        private readonly IPerformanceService _performanceService;

        public FacultyUserPerformanceController(IPerformanceService performanceService)
        {
            _performanceService = performanceService;
        }

        [HttpGet("{userId}/overview")]
        public async Task<IActionResult> GetUserOverview(long userId)
        {
            var result = await _performanceService.GetUserFullPerformanceAsync(userId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("{userId}/coding-tests")]
        public async Task<IActionResult> GetUserCodingTests(long userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _performanceService.GetUserCodingTestHistoryAsync(userId, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{userId}/practice-tests")]
        public async Task<IActionResult> GetUserPracticeTests(long userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _performanceService.GetUserPracticeTestHistoryAsync(userId, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{userId}/practice-tests/{practiceTestId}")]
        public async Task<IActionResult> GetUserPracticeTestDetail(long userId, int practiceTestId, [FromQuery] int? attemptNumber = null)
        {
            var result = await _performanceService.GetUserPracticeTestDetailAsync(userId, practiceTestId, attemptNumber);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("{userId}/free-practice")]
        public async Task<IActionResult> GetUserFreePractice(long userId)
        {
            var result = await _performanceService.GetUserFreePracticeSummaryAsync(userId);
            return Ok(result);
        }
    }
}
