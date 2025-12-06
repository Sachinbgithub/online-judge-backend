using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Services;
using LeetCodeCompiler.API.Models;
using LeetCodeCompiler.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionResultController : ControllerBase
    {
        private readonly IActivityTrackingService _activityTrackingService;
        private readonly AppDbContext _dbContext;
        private readonly PythonExecutionService _pythonService;
        private readonly JavaScriptExecutionService _javascriptService;
        private readonly JavaExecutionService _javaService;
        private readonly CppExecutionService _cppService;
        private readonly CExecutionService _cService;

        public QuestionResultController(
            IActivityTrackingService activityTrackingService, 
            AppDbContext dbContext,
            PythonExecutionService pythonService,
            JavaScriptExecutionService javascriptService,
            JavaExecutionService javaService,
            CppExecutionService cppService,
            CExecutionService cService)
        {
            _activityTrackingService = activityTrackingService;
            _dbContext = dbContext;
            _pythonService = pythonService;
            _javascriptService = javascriptService;
            _javaService = javaService;
            _cppService = cppService;
            _cService = cService;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitQuestionResult([FromBody] SubmitQuestionResultRequest request)
        {
            try
            {
                Console.WriteLine($"🔄 SubmitQuestionResult called with UserId: {request.UserId}, ProblemId: {request.ProblemId}");
                
                // ✅ STEP 1: Create activity log
                var sessionStartTime = DateTime.UtcNow;
                var activityLog = await _activityTrackingService.LogUserActivityAsync(
                    request.UserId,
                    request.ProblemId,
                    request.AttemptNumber,
                    "submit",
                    0 // Will be updated after processing
                );

                Console.WriteLine($"✅ Activity log created with ID: {activityLog.Id}");

                // ✅ STEP 2: Get test cases for the problem
                var testCases = _dbContext.TestCases.Where(tc => tc.ProblemId == request.ProblemId).ToList();
                
                if (!testCases.Any())
                {
                    Console.WriteLine("❌ No test cases found");
                    return BadRequest(new { error = "No test cases found for this problem." });
                }

                Console.WriteLine($"✅ Found {testCases.Count} test cases");

                // ✅ STEP 3: Execute code against test cases
                var codeExecutionRequest = new CodeExecutionRequest
                {
                    Language = request.LanguageUsed,
                    Code = request.FinalCodeSnapshot,
                    TestCases = testCases
                };

                // Create a temporary CodeExecutionController to execute the code
                var codeExecutionController = new CodeExecutionController(_dbContext, _activityTrackingService, Microsoft.Extensions.Logging.Abstractions.NullLogger<CodeExecutionController>.Instance, _pythonService, _javascriptService, _javaService, _cppService, _cService);
                var executionResult = await codeExecutionController.ExecuteCode(codeExecutionRequest) as OkObjectResult;
                var response = executionResult?.Value as CodeExecutionResponse;

                if (response == null)
                {
                    Console.WriteLine("❌ Code execution failed");
                    return StatusCode(500, new { error = "Failed to execute code" });
                }

                // ✅ STEP 4: Calculate execution time and results
                var endTime = DateTime.UtcNow;
                var timeTaken = (endTime - sessionStartTime).TotalSeconds;

                Console.WriteLine($"✅ Code execution completed in {timeTaken} seconds");

                // ✅ STEP 5: Extract passed and failed test case IDs
                var passedTestCaseIds = response.Results
                    .Where(r => r.Passed)
                    .Select((r, index) => testCases[index].Id.ToString())
                    .ToList();

                var failedTestCaseIds = response.Results
                    .Where(r => !r.Passed)
                    .Select((r, index) => testCases[index].Id.ToString())
                    .ToList();

                Console.WriteLine($"✅ Passed: {passedTestCaseIds.Count}, Failed: {failedTestCaseIds.Count}");

                // ✅ STEP 6: Update activity log with complete metrics including frontend data
                await _activityTrackingService.UpdateActivityMetricsAsync(
                    activityLog.Id,
                    timeTakenSeconds: (int)timeTaken,
                    languageSwitchCount: request.LanguageSwitchCount,
                    eraseCount: request.EraseCount,
                    saveCount: request.SaveCount,
                    runClickCount: request.RunClickCount,
                    submitClickCount: request.SubmitClickCount,
                    loginLogoutCount: request.LoginLogoutCount,
                    isSessionAbandoned: request.IsSessionAbandoned,
                    passedTestCaseIDs: string.Join(",", passedTestCaseIds),
                    failedTestCaseIDs: string.Join(",", failedTestCaseIds),
                    startTime: sessionStartTime,
                    endTime: endTime
                );

                Console.WriteLine($"✅ Activity log updated successfully");

                // ✅ STEP 7: Create or update question result
                var passedCount = response.Results.Count(r => r.Passed);
                var failedCount = response.Results.Count(r => !r.Passed);

                var questionResult = await _activityTrackingService.CreateOrUpdateQuestionResultAsync(
                    request.UserId,
                    request.ProblemId,
                    request.AttemptNumber,
                    request.LanguageUsed,
                    request.FinalCodeSnapshot,
                    response.Results.Count,
                    passedCount,
                    failedCount
                );

                Console.WriteLine($"✅ Question result created/updated with ID: {questionResult.Id}");

                // ✅ STEP 8: Log individual test case results
                for (int i = 0; i < response.Results.Count; i++)
                {
                    var result = response.Results[i];
                    await _activityTrackingService.LogTestCaseResultAsync(
                        questionResult.Id,
                        request.UserId,
                        request.ProblemId,
                        testCases[i].Id,
                        result.Passed,
                        result.Output ?? "",
                        result.Expected ?? "",
                        result.RuntimeMs
                    );
                }

                Console.WriteLine($"✅ All test case results logged");

                // ✅ STEP 9: Return response in the expected format
                var finalResponse = new
                {
                    id = questionResult.Id,
                    userId = questionResult.UserId,
                    problemId = questionResult.ProblemId,
                    attemptNumber = questionResult.AttemptNumber,
                    languageUsed = questionResult.LanguageUsed,
                    finalCodeSnapshot = questionResult.FinalCodeSnapshot,
                    totalTestCases = questionResult.TotalTestCases,
                    passedTestCases = questionResult.PassedTestCases,
                    failedTestCases = questionResult.FailedTestCases,
                    requestedHelp = questionResult.RequestedHelp,
                    createdAt = questionResult.CreatedAt,
                    lastSubmittedAt = questionResult.LastSubmittedAt,
                    // Add activity log ID for reference
                    activityLogId = activityLog.Id,
                    // Add test case results
                    results = response.Results,
                    // Add execution time
                    timeTakenSeconds = timeTaken
                };

                Console.WriteLine($"✅ Returning response with QuestionResultId: {questionResult.Id}");
                return Ok(finalResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SubmitQuestionResult: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to submit question result", details = ex.Message });
            }
        }

        [HttpGet("{userId}/problem/{problemId}")]
        public async Task<IActionResult> GetUserQuestionResultForProblem(int userId, int problemId)
        {
            try
            {
                var questionResults = await _activityTrackingService.GetUserQuestionResultsAsync(userId, problemId);
                
                // Get problem details
                var problem = await _dbContext.Problems.FindAsync(problemId);
                var problemTitle = problem?.Title ?? "Unknown Problem";

                // Format response with calculated counts
                var formattedResults = new List<object>();
                
                foreach (var result in questionResults)
                {
                    // Calculate actual counts for each result
                    var testCaseResults = await _dbContext.CoreTestCaseResults
                        .Where(tcr => tcr.CoreQuestionResultId == result.Id)
                        .ToListAsync();

                    var actualTotal = testCaseResults.Count;
                    var actualPassed = testCaseResults.Count(tcr => tcr.IsPassed);
                    var actualFailed = testCaseResults.Count(tcr => !tcr.IsPassed);
                    var successRate = actualTotal > 0 ? ((double)actualPassed / actualTotal) * 100 : 0;

                    formattedResults.Add(new
                    {
                        id = result.Id,
                        problemTitle = problemTitle,
                        problemId = result.ProblemId,
                        userId = result.UserId,
                        attemptNumber = result.AttemptNumber,
                        totalTestCases = actualTotal,        // Use calculated count
                        passedTestCases = actualPassed,      // Use calculated count
                        failedTestCases = actualFailed,      // Use calculated count
                        languageUsed = result.LanguageUsed,
                        finalCodeSnapshot = result.FinalCodeSnapshot,
                        requestedHelp = result.RequestedHelp,
                        createdAt = result.CreatedAt,
                        lastSubmittedAt = result.LastSubmittedAt,
                        successRate = Math.Round(successRate, 1),
                        timeAgo = GetTimeAgo(result.LastSubmittedAt)
                    });
                }

                return Ok(formattedResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve user question result for problem", details = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllQuestionResults()
        {
            try
            {
                var allResults = await _activityTrackingService.GetAllQuestionResultsAsync();
                
                // Format response with calculated counts
                var formattedResults = new List<object>();
                
                foreach (var result in allResults)
                {
                    // Calculate actual counts for each result
                    var testCaseResults = await _dbContext.CoreTestCaseResults
                        .Where(tcr => tcr.CoreQuestionResultId == result.Id)
                        .ToListAsync();

                    var actualTotal = testCaseResults.Count;
                    var actualPassed = testCaseResults.Count(tcr => tcr.IsPassed);
                    var actualFailed = testCaseResults.Count(tcr => !tcr.IsPassed);

                    formattedResults.Add(new
                    {
                        id = result.Id,
                        problemId = result.ProblemId,
                        userId = result.UserId,
                        attemptNumber = result.AttemptNumber,
                        totalTestCases = actualTotal,        // Use calculated count
                        passedTestCases = actualPassed,      // Use calculated count
                        failedTestCases = actualFailed,      // Use calculated count
                        languageUsed = result.LanguageUsed,
                        finalCodeSnapshot = result.FinalCodeSnapshot,
                        requestedHelp = result.RequestedHelp,
                        createdAt = result.CreatedAt,
                        lastSubmittedAt = result.LastSubmittedAt
                    });
                }

                return Ok(formattedResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve all question results", details = ex.Message });
            }
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserQuestionResults(int userId)
        {
            try
            {
                var userResults = await _activityTrackingService.GetUserQuestionResultsAsync(userId);
                return Ok(userResults);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve user question results", details = ex.Message });
            }
        }

        [HttpGet("{questionResultId}/complete")]
        public async Task<IActionResult> GetCompleteQuestionResult(int questionResultId)
        {
            try
            {
                var result = await _dbContext.CoreQuestionResults
                    .FirstOrDefaultAsync(r => r.Id == questionResultId);
                
                if (result == null)
                    return NotFound(new { error = $"Question result with ID {questionResultId} not found" });

                // Calculate actual counts from test case results
                var testCaseResults = await _dbContext.CoreTestCaseResults
                    .Where(tcr => tcr.CoreQuestionResultId == questionResultId)
                    .ToListAsync();

                var actualTotal = testCaseResults.Count;
                var actualPassed = testCaseResults.Count(tcr => tcr.IsPassed);
                var actualFailed = testCaseResults.Count(tcr => !tcr.IsPassed);
                    
                return Ok(new
                {
                    id = result.Id,
                    problemId = result.ProblemId,
                    userId = result.UserId,
                    attemptNumber = result.AttemptNumber,
                    totalTestCases = actualTotal,        // Use calculated count
                    passedTestCases = actualPassed,      // Use calculated count
                    failedTestCases = actualFailed,      // Use calculated count
                    languageUsed = result.LanguageUsed,
                    finalCodeSnapshot = result.FinalCodeSnapshot,
                    requestedHelp = result.RequestedHelp,
                    createdAt = result.CreatedAt,
                    lastSubmittedAt = result.LastSubmittedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch complete question result", details = ex.Message });
            }
        }

        [HttpGet("{userId}/problem/{problemId}/activity-logs")]
        public async Task<IActionResult> GetUserActivityLogsForProblem(int userId, int problemId)
        {
            try
            {
                var activityLogs = await _activityTrackingService.GetUserActivityLogsAsync(userId, problemId);
                
                if (!activityLogs.Any())
                {
                    return Ok(new { 
                        message = "No activity logs found for this problem",
                        activityLogs = new List<object>()
                    });
                }

                // Format response to highlight the three key fields you requested
                var formattedLogs = activityLogs.Select(log => new
                {
                    id = log.Id,
                    userId = log.UserId,
                    problemId = log.ProblemId,
                    attemptNumber = log.AttemptNumber,
                    testType = log.TestType,
                    
                    // ✅ KEY FIELDS YOU REQUESTED:
                    startTime = log.StartTime,
                    endTime = log.EndTime,
                    languageSwitchCount = log.LanguageSwitchCount,
                    
                    // Additional useful fields
                    timeTakenSeconds = log.TimeTakenSeconds,
                    sessionDuration = FormatDuration(log.TimeTakenSeconds),
                    eraseCount = log.EraseCount,
                    saveCount = log.SaveCount,
                    runClickCount = log.RunClickCount,
                    submitClickCount = log.SubmitClickCount,
                    loginLogoutCount = log.LoginLogoutCount,
                    isSessionAbandoned = log.IsSessionAbandoned,
                    passedTestCaseIDs = log.PassedTestCaseIDs,
                    failedTestCaseIDs = log.FailedTestCaseIDs,
                    createdAt = log.CreatedAt,
                    updatedAt = log.UpdatedAt,
                    timeAgo = GetTimeAgo(log.CreatedAt)
                }).ToList();

                return Ok(new
                {
                    totalLogs = formattedLogs.Count,
                    problemId = problemId,
                    userId = userId,
                    activityLogs = formattedLogs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve user activity logs for problem", details = ex.Message });
            }
        }

        // ✅ NEW: Add the correct endpoint as shown in the image
        [HttpGet("{userId}/problem/{problemId}/activity")]
        public async Task<IActionResult> GetUserActivityForProblem(int userId, int problemId)
        {
            try
            {
                var activityLogs = await _activityTrackingService.GetUserActivityLogsAsync(userId, problemId);
                
                if (!activityLogs.Any())
                {
                    return Ok(new { 
                        message = "No activity logs found for this problem",
                        activityLogs = new List<object>()
                    });
                }

                // Format response with the exact fields you need
                var formattedLogs = activityLogs.Select(log => new
                {
                    id = log.Id,
                    userId = log.UserId,
                    problemId = log.ProblemId,
                    attemptNumber = log.AttemptNumber,
                    testType = log.TestType,
                    
                    // ✅ EXACT FIELDS YOU REQUESTED:
                    startTime = log.StartTime,
                    endTime = log.EndTime,
                    timeTakenSeconds = log.TimeTakenSeconds,
                    sessionDuration = FormatDuration(log.TimeTakenSeconds),
                    languageSwitchCount = log.LanguageSwitchCount,
                    
                    // Additional metrics
                    eraseCount = log.EraseCount,
                    saveCount = log.SaveCount,
                    runClickCount = log.RunClickCount,
                    submitClickCount = log.SubmitClickCount,
                    loginLogoutCount = log.LoginLogoutCount,
                    isSessionAbandoned = log.IsSessionAbandoned,
                    passedTestCaseIDs = log.PassedTestCaseIDs,
                    failedTestCaseIDs = log.FailedTestCaseIDs,
                    createdAt = log.CreatedAt,
                    updatedAt = log.UpdatedAt,
                    timeAgo = GetTimeAgo(log.CreatedAt)
                }).ToList();

                return Ok(new
                {
                    totalLogs = formattedLogs.Count,
                    problemId = problemId,
                    userId = userId,
                    activityLogs = formattedLogs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve user activity for problem", details = ex.Message });
            }
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;
            
            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            else if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes == 1 ? "" : "s")} ago";
            else if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours == 1 ? "" : "s")} ago";
            else
                return $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays == 1 ? "" : "s")} ago";
        }

        private string FormatDuration(int seconds)
        {
            if (seconds < 60)
                return $"{seconds} second{(seconds == 1 ? "" : "s")}";
            else if (seconds < 3600)
            {
                var minutes = seconds / 60;
                var remainingSeconds = seconds % 60;
                return $"{minutes} minute{(minutes == 1 ? "" : "s")} {remainingSeconds} second{(remainingSeconds == 1 ? "" : "s")}";
            }
            else
            {
                var hours = seconds / 3600;
                var remainingMinutes = (seconds % 3600) / 60;
                return $"{hours} hour{(hours == 1 ? "" : "s")} {remainingMinutes} minute{(remainingMinutes == 1 ? "" : "s")}";
            }
        }

        private ICodeExecutionService? GetExecutionService(string language)
        {
            return language.ToLower() switch
            {
                "python" => _pythonService,
                "javascript" or "js" => _javascriptService,
                "java" => _javaService,
                "cpp" or "c++" => _cppService,
                "c" => _cService,
                _ => null
            };
        }

        private string WrapCode(string language, string code, string problemType)
        {
            return language.ToLower() switch
            {
                // "python" => FlexibleWrapperTemplates.WrapPython(code, problemType),
                // "javascript" or "js" => FlexibleWrapperTemplates.WrapJavaScript(code, problemType),
                // "java" => FlexibleWrapperTemplates.WrapJava(code, problemType),
                // "cpp" or "c++" => FlexibleWrapperTemplates.WrapCpp(code, problemType),
                _ => code
            };
        }

        private string GetProblemType(int problemId)
        {
            return problemId switch
            {
                1 => "two_sum",
                6 => "reverse_integer",
                7 => "sum_of_squares",
                8 => "count_vowels_in_string",
                _ => "two_sum" // default fallback
            };
        }
    }

    public class SubmitQuestionResultRequest
    {
        public int UserId { get; set; }
        public int ProblemId { get; set; }
        public int AttemptNumber { get; set; }
        public string LanguageUsed { get; set; } = "";
        public string FinalCodeSnapshot { get; set; } = "";
        public int TotalTestCases { get; set; }
        public int PassedTestCases { get; set; }
        public int FailedTestCases { get; set; }
        public bool RequestedHelp { get; set; } = false;
        
        // ✅ ADD THESE NEW PROPERTIES:
        public int LanguageSwitchCount { get; set; } = 0;
        public int RunClickCount { get; set; } = 0;
        public int SubmitClickCount { get; set; } = 0;
        public int EraseCount { get; set; } = 0;
        public int SaveCount { get; set; } = 0;
        public int LoginLogoutCount { get; set; } = 0;
        public bool IsSessionAbandoned { get; set; } = false;
    }
}