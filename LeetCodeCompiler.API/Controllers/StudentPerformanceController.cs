using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Services;
using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AnyAuthenticated")]
    public class StudentPerformanceController : ControllerBase
    {
        private readonly IPerformanceService _performanceService;

        public StudentPerformanceController(IPerformanceService performanceService)
        {
            _performanceService = performanceService;
        }

        [HttpGet("{userId}/overview")]
        public async Task<IActionResult> GetOverview(long userId)
        {
            var result = await _performanceService.GetStudentOverviewAsync(userId);
            return Ok(result);
        }

        [HttpGet("{userId}/coding-tests")]
        public async Task<IActionResult> GetCodingTestHistory(long userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _performanceService.GetCodingTestHistoryAsync(userId, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{userId}/practice-tests")]
        public async Task<IActionResult> GetPracticeTestHistory(long userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _performanceService.GetPracticeTestHistoryAsync(userId, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{userId}/free-practice")]
        public async Task<IActionResult> GetFreePracticeSummary(long userId)
        {
            var result = await _performanceService.GetFreePracticeSummaryAsync(userId);
            return Ok(result);
        }
    }
}
