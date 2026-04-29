using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Services;
using LeetCodeCompiler.API.Models;
using LeetCodeCompiler.API.Data;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AnyAuthenticated")]
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
        public async Task<IActionResult> GetTestCaseResults(int coreQuestionResultId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _dbContext.CoreTestCaseResults
                    .Where(result => result.CoreQuestionResultId == coreQuestionResultId);
                
                var totalCount = await query.CountAsync();

                var testCaseResults = await query
                    .OrderBy(result => result.TestCaseId)
                    .ThenByDescending(result => result.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                
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

                return Ok(new PagedResult<object>
                {
                    Items = formattedResults,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve test case results", details = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTestCaseResults([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _dbContext.CoreTestCaseResults;
                var totalCount = await query.CountAsync();

                var allResults = await query
                    .OrderByDescending(result => result.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                    
                return Ok(new PagedResult<CoreTestCaseResult>
                {
                    Items = allResults,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve all test case results", details = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserTestCaseResults(int userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _dbContext.CoreTestCaseResults.Where(result => result.UserId == userId);
                var totalCount = await query.CountAsync();

                var userResults = await query
                    .OrderByDescending(result => result.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new PagedResult<CoreTestCaseResult>
                {
                    Items = userResults,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve user test case results", details = ex.Message });
            }
        }

        [HttpGet("user/{userId}/problem/{problemId}")]
        public async Task<IActionResult> GetUserTestCaseResultsForProblem(int userId, int problemId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _dbContext.CoreTestCaseResults.Where(result => result.UserId == userId && result.ProblemId == problemId);
                var totalCount = await query.CountAsync();

                var results = await query
                    .OrderByDescending(result => result.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                    
                return Ok(new PagedResult<CoreTestCaseResult>
                {
                    Items = results,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve user test case results for problem", details = ex.Message });
            }
        }

        [HttpGet("problem/{problemId}/definitions")]
        public async Task<IActionResult> GetTestCaseDefinitions(int problemId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _dbContext.TestCases
                    .Where(tc => tc.ProblemId == problemId);
                
                var totalCount = await query.CountAsync();
 
                var testCases = await query
                    .OrderBy(tc => tc.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                
                var formattedTestCases = testCases.Select(tc => new
                {
                    testCaseId = tc.Id,
                    input = tc.Input,
                    expectedOutput = tc.ExpectedOutput
                }).Cast<object>().ToList();
 
                return Ok(new PagedResult<object>
                {
                    Items = formattedTestCases,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
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