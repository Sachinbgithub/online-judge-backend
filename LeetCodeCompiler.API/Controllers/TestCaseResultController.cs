using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Services;
using LeetCodeCompiler.API.Models;
using LeetCodeCompiler.API.Data;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestCaseResultController : ControllerBase
    {
        private readonly IActivityTrackingService _activityTrackingService;
        private readonly AppDbContext _dbContext;

        public TestCaseResultController(IActivityTrackingService activityTrackingService, AppDbContext dbContext)
        {
            _activityTrackingService = activityTrackingService;
            _dbContext = dbContext;
        }

        [HttpGet("{coreQuestionResultId}")]
        public async Task<IActionResult> GetTestCaseResults(int coreQuestionResultId)
        {
            try
            {
                var testCaseResults = await _activityTrackingService.GetTestCaseResultsAsync(coreQuestionResultId);
                
                // Format response for user-friendly display
                var formattedResults = new List<object>();
                
                foreach (var testCase in testCaseResults)
                {
                    var input = await GetTestCaseInput(testCase.TestCaseId, testCase.ProblemId);
                    
                    formattedResults.Add(new
                    {
                        testCaseNumber = testCase.TestCaseId,
                        isPassed = testCase.IsPassed,
                        status = testCase.IsPassed ? "PASSED" : "FAILED",
                        userOutput = testCase.UserOutput,
                        expectedOutput = testCase.ExpectedOutput,
                        executionTime = testCase.ExecutionTime,
                        createdAt = testCase.CreatedAt,
                        input = input // Now gets real input from database
                    });
                }

                return Ok(formattedResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve test case results", details = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTestCaseResults()
        {
            try
            {
                var allResults = await _activityTrackingService.GetAllTestCaseResultsAsync();
                return Ok(allResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve all test case results", details = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserTestCaseResults(int userId)
        {
            try
            {
                var userResults = await _activityTrackingService.GetUserTestCaseResultsAsync(userId);
                return Ok(userResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve user test case results", details = ex.Message });
            }
        }

        [HttpGet("user/{userId}/problem/{problemId}")]
        public async Task<IActionResult> GetUserTestCaseResultsForProblem(int userId, int problemId)
        {
            try
            {
                var results = await _activityTrackingService.GetUserTestCaseResultsForProblemAsync(userId, problemId);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve user test case results for problem", details = ex.Message });
            }
        }

        [HttpGet("problem/{problemId}/definitions")]
        public async Task<IActionResult> GetTestCaseDefinitions(int problemId)
        {
            try
            {
                var testCases = await _dbContext.TestCases
                    .Where(tc => tc.ProblemId == problemId)
                    .OrderBy(tc => tc.Id)
                    .ToListAsync();
                
                var formattedTestCases = testCases.Select(tc => new
                {
                    testCaseId = tc.Id,
                    input = tc.Input,
                    expectedOutput = tc.ExpectedOutput
                }).ToList();

                return Ok(formattedTestCases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve test case definitions", details = ex.Message });
            }
        }

        [HttpPost("log")]
        public async Task<IActionResult> LogTestCaseResult([FromBody] LogTestCaseResultRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var testCaseResult = await _activityTrackingService.LogTestCaseResultAsync(
                    request.CoreQuestionResultId,
                    request.UserId,
                    request.ProblemId,
                    request.TestCaseId,
                    request.IsPassed,
                    request.UserOutput,
                    request.ExpectedOutput,
                    request.ExecutionTime
                );

                return CreatedAtAction(nameof(GetTestCaseResults), new { coreQuestionResultId = request.CoreQuestionResultId }, testCaseResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to log test case result", details = ex.Message });
            }
        }

        private async Task<string> GetTestCaseInput(int testCaseId, int problemId)
        {
            try
            {
                var testCase = await _dbContext.TestCases
                    .FirstOrDefaultAsync(tc => tc.Id == testCaseId && tc.ProblemId == problemId);
                
                return testCase?.Input ?? "Input not found";
            }
            catch
            {
                return "Input not available";
            }
        }
    }

    public class LogTestCaseResultRequest
    {
        public int CoreQuestionResultId { get; set; }
        public int UserId { get; set; }
        public int ProblemId { get; set; }
        public int TestCaseId { get; set; }
        public bool IsPassed { get; set; }
        public string UserOutput { get; set; } = "";
        public string ExpectedOutput { get; set; } = "";
        public double ExecutionTime { get; set; }
    }
}