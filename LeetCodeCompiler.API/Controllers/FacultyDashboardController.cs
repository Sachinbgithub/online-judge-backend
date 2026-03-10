using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Services;
using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacultyDashboardController : ControllerBase
    {
        private readonly IPerformanceService _performanceService;

        public FacultyDashboardController(IPerformanceService performanceService)
        {
            _performanceService = performanceService;
        }

        [HttpGet("{codingTestId}/students")]
        public async Task<IActionResult> GetStudentsForTest(int codingTestId)
        {
            var result = await _performanceService.GetStudentsForTestAsync(codingTestId);
            return Ok(result);
        }

        [HttpGet("{codingTestId}/leaderboard")]
        public async Task<IActionResult> GetLeaderboard(int codingTestId)
        {
            var result = await _performanceService.GetLeaderboardAsync(codingTestId);
            return Ok(result);
        }

        [HttpGet("{practiceTestId}/practice-students")]
        public async Task<IActionResult> GetPracticeStudents(int practiceTestId)
        {
            var result = await _performanceService.GetPracticeStudentsAsync(practiceTestId);
            return Ok(result);
        }

        [HttpGet("{codingTestId}/problem-analysis")]
        public async Task<IActionResult> GetProblemAnalysis(int codingTestId)
        {
            var result = await _performanceService.GetProblemAnalysisAsync(codingTestId);
            return Ok(result);
        }
    }
}
