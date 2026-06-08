using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Services;
using LeetCodeCompiler.API.Models;
using LeetCodeCompiler.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AnyAuthenticated")]
    public class CodeExecutionController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IActivityTrackingService _activityTrackingService;
        private readonly IJudgeService _judgeService;
        private readonly ILogger<CodeExecutionController> _logger;

        public CodeExecutionController(
            AppDbContext db,
            IActivityTrackingService activityTrackingService,
            IJudgeService judgeService,
            ILogger<CodeExecutionController> logger)
        {
            _db = db;
            _activityTrackingService = activityTrackingService;
            _judgeService = judgeService;
            _logger = logger;
        }

        /// <summary>
        /// Execute code in a specified language against multiple test cases.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ExecuteCode([FromBody] CodeExecutionRequest request)
        {
            try
            {
                var validationResult = ValidateCodeExecutionRequest(request);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Invalid code execution request: {Errors}", string.Join(", ", validationResult.Errors));
                    return BadRequest(new { error = "Invalid request", details = validationResult.Errors });
                }

                _logger.LogInformation("Code execution started for language: {Language}, code length: {CodeLength}",
                    request.Language, request.Code.Length);

                var startTime = DateTime.UtcNow;
                var judgeResult = await _judgeService.EvaluateTestCasesAsync(
                    request.Language, request.Code, request.TestCases);

                var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation("Code execution completed in {ExecutionTime}ms", executionTime);

                return Ok(JudgeResultMapper.ToCodeExecutionResponse(judgeResult, executionTime));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in code execution");
                return StatusCode(500, new { error = "Internal server error during code execution" });
            }
        }

        /// <summary>
        /// Execute code with flexible problem templates - no new files needed!
        /// </summary>
        [HttpPost("flexible")]
        public async Task<IActionResult> ExecuteFlexibleCode([FromBody] FlexibleCodeExecutionRequest request)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Language) ||
                string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.ProblemType))
            {
                return BadRequest(new { error = "Missing or invalid required properties: language, code, or problemType." });
            }

            try
            {
                JudgeResult judgeResult;

                if (request.GetType().GetProperty("CustomInput")?.GetValue(request) is string customInput
                    && !string.IsNullOrWhiteSpace(customInput))
                {
                    var syntheticCase = new List<TestCase>
                    {
                        new TestCase { Input = customInput, ExpectedOutput = "" }
                    };
                    judgeResult = await _judgeService.EvaluateTestCasesAsync(
                        request.Language, request.Code, syntheticCase);
                }
                else if (request.ProblemId.HasValue)
                {
                    judgeResult = await _judgeService.EvaluateAsync(
                        request.ProblemId.Value, request.Language, request.Code);
                }
                else if (request.TestCases != null && request.TestCases.Count > 0)
                {
                    judgeResult = await _judgeService.EvaluateTestCasesAsync(
                        request.Language, request.Code, request.TestCases);
                }
                else
                {
                    return BadRequest(new { error = "No test cases found: provide ProblemId, TestCases, or custom input." });
                }

                return Ok(JudgeResultMapper.ToCodeExecutionResponse(judgeResult, judgeResult.ExecutionTimeMs));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CodeExecutionResponse { Results = new List<TestCaseResult>(), Error = ex.Message });
            }
        }

        /// <summary>
        /// Execute code with activity tracking for user interactions.
        /// </summary>
        [HttpPost("tracked")]
        public async Task<IActionResult> ExecuteCodeWithTracking([FromBody] TrackedCodeExecutionRequest request)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Language) || string.IsNullOrWhiteSpace(request.Code) || request.TestCases == null || request.TestCases.Count == 0)
            {
                return BadRequest(new { error = "Missing or invalid required properties: language, code, or testCases." });
            }

            try
            {
                var startTime = DateTime.UtcNow;
                var activityLog = await _activityTrackingService.LogUserActivityAsync(
                    request.UserId,
                    request.ProblemId,
                    request.AttemptNumber,
                    "run",
                    0);

                var judgeResult = await _judgeService.EvaluateTestCasesAsync(
                    request.Language, request.Code, request.TestCases);

                var endTime = DateTime.UtcNow;
                var timeTaken = (endTime - startTime).TotalSeconds;
                var results = JudgeResultMapper.ToTestCaseResults(judgeResult);

                await _activityTrackingService.UpdateActivityMetricsAsync(
                    activityLog.Id,
                    timeTakenSeconds: (int)timeTaken);

                var passedCount = judgeResult.PassedTestCases;
                var failedCount = judgeResult.FailedTestCases;

                var questionResult = await _activityTrackingService.CreateOrUpdateQuestionResultAsync(
                    request.UserId,
                    request.ProblemId,
                    request.AttemptNumber,
                    request.Language,
                    request.Code,
                    judgeResult.TotalTestCases,
                    passedCount,
                    failedCount);

                for (int i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    await _activityTrackingService.LogTestCaseResultAsync(
                        questionResult.Id,
                        request.UserId,
                        request.ProblemId,
                        i + 1,
                        result.Passed,
                        result.Output,
                        result.Expected,
                        result.RuntimeMs);
                }

                var passedIds = string.Join(",", results.Select((r, i) => new { r.Passed, Index = i + 1 }).Where(x => x.Passed).Select(x => x.Index));
                var failedIds = string.Join(",", results.Select((r, i) => new { r.Passed, Index = i + 1 }).Where(x => !x.Passed).Select(x => x.Index));

                await _activityTrackingService.UpdateActivityMetricsAsync(
                    activityLog.Id,
                    passedTestCaseIDs: passedIds,
                    failedTestCaseIDs: failedIds);

                return Ok(new TrackedCodeExecutionResponse
                {
                    Results = results,
                    ActivityLogId = activityLog.Id,
                    QuestionResultId = questionResult.Id,
                    TimeTakenSeconds = timeTaken
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to execute code with tracking", details = ex.Message });
            }
        }

        /// <summary>
        /// Submit final solution with comprehensive activity tracking.
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitCode([FromBody] SubmitSolutionRequest request)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Language) || string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest(new { error = "Missing or invalid required properties: language or code." });
            }

            try
            {
                var sessionStartTime = DateTime.UtcNow;

                var activityLog = await _activityTrackingService.LogUserActivityAsync(
                    request.UserId,
                    request.ProblemId,
                    request.AttemptNumber,
                    "submit",
                    0);

                var testCases = await _db.TestCases
                    .Where(tc => tc.ProblemId == request.ProblemId)
                    .OrderBy(tc => tc.Id)
                    .ToListAsync();

                if (!testCases.Any())
                    return BadRequest(new { error = "No test cases found for this problem." });

                var judgeResult = await _judgeService.EvaluateAsync(
                    request.ProblemId, request.Language, request.Code);

                var results = JudgeResultMapper.ToTestCaseResults(judgeResult);
                var endTime = DateTime.UtcNow;
                var timeTaken = (endTime - sessionStartTime).TotalSeconds;

                var passedTestCaseIds = judgeResult.TestCaseResults
                    .Where(r => r.IsPassed)
                    .Select(r => r.TestCaseId.ToString())
                    .ToList();

                var failedTestCaseIds = judgeResult.TestCaseResults
                    .Where(r => !r.IsPassed)
                    .Select(r => r.TestCaseId.ToString())
                    .ToList();

                await _activityTrackingService.UpdateActivityMetricsAsync(
                    activityLog.Id,
                    timeTakenSeconds: (int)timeTaken,
                    runClickCount: request.RunClickCount,
                    submitClickCount: request.SubmitClickCount + 1,
                    saveCount: request.SaveCount,
                    languageSwitchCount: request.LanguageSwitchCount,
                    eraseCount: request.EraseCount,
                    loginLogoutCount: request.LoginLogoutCount,
                    isSessionAbandoned: request.IsSessionAbandoned,
                    passedTestCaseIDs: string.Join(",", passedTestCaseIds),
                    failedTestCaseIDs: string.Join(",", failedTestCaseIds),
                    startTime: sessionStartTime,
                    endTime: endTime);

                var questionResult = await _activityTrackingService.CreateOrUpdateQuestionResultAsync(
                    request.UserId,
                    request.ProblemId,
                    request.AttemptNumber,
                    request.Language,
                    request.Code,
                    judgeResult.TotalTestCases,
                    judgeResult.PassedTestCases,
                    judgeResult.FailedTestCases);

                for (int i = 0; i < judgeResult.TestCaseResults.Count; i++)
                {
                    var caseResult = judgeResult.TestCaseResults[i];
                    await _activityTrackingService.LogTestCaseResultAsync(
                        questionResult.Id,
                        request.UserId,
                        request.ProblemId,
                        caseResult.TestCaseId,
                        caseResult.IsPassed,
                        caseResult.ActualOutput ?? "",
                        caseResult.ExpectedOutput,
                        caseResult.ExecutionTimeMs);
                }

                return Ok(new SubmitSolutionResponse
                {
                    Results = results,
                    ActivityLogId = activityLog.Id,
                    QuestionResultId = questionResult.Id,
                    TimeTakenSeconds = timeTaken,
                    TotalTestCases = judgeResult.TotalTestCases,
                    PassedTestCases = judgeResult.PassedTestCases,
                    FailedTestCases = judgeResult.FailedTestCases,
                    Success = judgeResult.IsCorrect
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to submit code", details = ex.Message });
            }
        }

        private static (bool IsValid, List<string> Errors) ValidateCodeExecutionRequest(CodeExecutionRequest request)
        {
            var errors = new List<string>();

            if (request == null)
            {
                errors.Add("Request cannot be null");
                return (false, errors);
            }

            if (string.IsNullOrWhiteSpace(request.Language))
                errors.Add("Language is required");

            if (string.IsNullOrWhiteSpace(request.Code))
                errors.Add("Code is required");
            else if (request.Code.Length > 10000)
                errors.Add("Code exceeds maximum length of 10,000 characters");

            if (request.TestCases == null || !request.TestCases.Any())
                errors.Add("At least one test case is required");
            else if (request.TestCases.Count > 20)
                errors.Add("Maximum 20 test cases allowed per execution");

            return (errors.Count == 0, errors);
        }
    }
}
