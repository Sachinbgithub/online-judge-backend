using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Services;
using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        public async Task<IActionResult> GetCodingTestHistory(long userId)
        {
            var result = await _performanceService.GetCodingTestHistoryAsync(userId);
            return Ok(result);
        }

        [HttpGet("{userId}/practice-tests")]
        public async Task<IActionResult> GetPracticeTestHistory(long userId)
        {
            var result = await _performanceService.GetPracticeTestHistoryAsync(userId);
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
