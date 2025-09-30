using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeetCodeCompiler.API.Services;
using System.Linq;
using LeetCodeCompiler.API.Models;
using LeetCodeCompiler.API.Data;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // 🔧 DEVELOPMENT MODE: Rate limiting disabled for faster development
    // [EnableRateLimiting("ApiLimiter")]
    public class CodeExecutionController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IActivityTrackingService _activityTrackingService;
        private readonly ILogger<CodeExecutionController> _logger;
        private readonly PythonExecutionService _pythonService;
        private readonly JavaScriptExecutionService _javascriptService;
        private readonly JavaExecutionService _javaService;
        private readonly CppExecutionService _cppService;

        // 🔧 DEVELOPMENT MODE: Security patterns disabled for faster development
        // private static readonly string[] DangerousPatterns = {
        //     // All security patterns disabled in development
        // };

        public CodeExecutionController(
            AppDbContext db, 
            IActivityTrackingService activityTrackingService,
            ILogger<CodeExecutionController> logger,
            PythonExecutionService pythonService,
            JavaScriptExecutionService javascriptService,
            JavaExecutionService javaService,
            CppExecutionService cppService)
        {
            _db = db;
            _activityTrackingService = activityTrackingService;
            _logger = logger;
            _pythonService = pythonService;
            _javascriptService = javascriptService;
            _javaService = javaService;
            _cppService = cppService;
        }

        /// <summary>
        /// Execute code in a specified language against multiple test cases.
        /// </summary>
        /// <param name="request">The code, language, and test cases.</param>
        /// <returns>Results for each test case.</returns>
        [HttpPost]
        // 🔧 DEVELOPMENT MODE: Rate limiting disabled for faster development
        // [EnableRateLimiting("CodeExecution")]
        public async Task<IActionResult> ExecuteCode([FromBody] CodeExecutionRequest request)
        {
            try
            {
                // Input validation
                var validationResult = ValidateCodeExecutionRequest(request);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Invalid code execution request: {Errors}", string.Join(", ", validationResult.Errors));
                    return BadRequest(new { error = "Invalid request", details = validationResult.Errors });
            }

                // 🔧 DEVELOPMENT MODE: Skip security checks for faster development
                // var securityCheck = CheckCodeSecurity(request.Code, request.Language);
                // if (!securityCheck.IsSafe)
                // {
                //     _logger.LogWarning("Potentially malicious code detected: {Reason}", securityCheck.Reason);
                //     return BadRequest(new { error = "Code contains potentially dangerous operations", reason = securityCheck.Reason });
                // }

                // Log the execution attempt
                _logger.LogInformation("Code execution started for language: {Language}, code length: {CodeLength}", 
                    request.Language, request.Code.Length);

                ICodeExecutionService? service = GetExecutionService(request.Language);
                if (service == null)
                {
                    return BadRequest(new { error = $"Unsupported language: {request.Language}" });
            }

            var results = new List<TestCaseResult>();
                var startTime = DateTime.UtcNow;

                foreach (var testCase in request.TestCases)
                {
                    try
                    {
                        var result = await service.ExecuteAsync(request.Code, testCase.Input);
                        results.Add(new TestCaseResult
                        {
                            Input = testCase.Input,
                            Output = result.Output,
                            Expected = testCase.ExpectedOutput,
                            Passed = result.Output.Trim() == testCase.ExpectedOutput.Trim(),
                            Stdout = result.Stdout,
                            Stderr = result.Stderr,
                            RuntimeMs = result.RuntimeMs,
                            MemoryMb = result.MemoryMb,
                            Error = result.Error
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing test case: {Input}", testCase.Input);
                    results.Add(new TestCaseResult
                    {
                        Input = testCase.Input,
                        Output = "",
                        Expected = testCase.ExpectedOutput,
                        Passed = false,
                            Error = "Execution failed: " + ex.Message
                    });
                }
            }

                var executionTime = DateTime.UtcNow - startTime;
                _logger.LogInformation("Code execution completed in {ExecutionTime}ms", executionTime.TotalMilliseconds);

                return Ok(new CodeExecutionResponse
                {
                    Results = results,
                    ExecutionTime = executionTime.TotalMilliseconds
                });
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
        /// <param name="request">The code, language, test cases, and problem type.</param>
        /// <returns>Results for each test case.</returns>
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
                ICodeExecutionService? service = null;
                switch (request.Language.ToLower())
                {
                    case "python":
                        service = _pythonService;
                        break;
                    case "javascript":
                    case "js":
                        service = _javascriptService;
                        break;
                    case "java":
                        service = _javaService;
                        break;
                    case "cpp":
                    case "c++":
                        service = _cppService;
                        break;
                    default:
                        break;
                }

                var results = new List<TestCaseResult>();
                List<TestCase> testCasesToRun = new();
                // If customInput is provided, use it as a single test case (for ad-hoc runs)
                if (request.GetType().GetProperty("CustomInput") != null && request.GetType().GetProperty("CustomInput")?.GetValue(request) is string customInput && !string.IsNullOrWhiteSpace(customInput))
                {
                    testCasesToRun.Add(new TestCase { Input = customInput, ExpectedOutput = "" });
                }
                else if (request.ProblemId.HasValue)
                {
                    // Fetch test cases from DB
                    testCasesToRun = _db.TestCases.Where(tc => tc.ProblemId == request.ProblemId.Value).ToList();
                }
                else
                {
                    return BadRequest(new { error = "No test cases found: provide ProblemId or custom input." });
                }

                if (service == null)
                {
                    foreach (var testCase in testCasesToRun)
                    {
                        results.Add(new TestCaseResult
                        {
                            Input = testCase.Input,
                            Output = "",
                            Expected = testCase.ExpectedOutput,
                            Passed = false,
                            Stdout = "",
                            Stderr = "",
                            RuntimeMs = 0,
                            MemoryMb = 0,
                            Error = "Language not implemented or supported."
                        });
                    }
                }
                else
                {
                    foreach (var testCase in testCasesToRun)
                    {
                        try
                        {
                            var execResult = await service.ExecuteAsync(request.Code, testCase.Input);
                            results.Add(new TestCaseResult
                            {
                                Input = testCase.Input,
                                Output = execResult.Output,
                                Expected = testCase.ExpectedOutput ?? "",
                                Passed = string.IsNullOrEmpty(execResult.Error) && string.IsNullOrEmpty(execResult.Stderr) && execResult.Output.Trim() == (testCase.ExpectedOutput?.Trim() ?? ""),
                                Stdout = execResult.Stdout,
                                Stderr = execResult.Stderr,
                                RuntimeMs = execResult.RuntimeMs,
                                MemoryMb = execResult.MemoryMb,
                                Error = execResult.Error
                            });
                        }
                        catch (Exception ex)
                        {
                            results.Add(new TestCaseResult
                            {
                                Input = testCase.Input,
                                Output = "",
                                Expected = testCase.ExpectedOutput ?? "",
                                Passed = false,
                                Stdout = "",
                                Stderr = "",
                                RuntimeMs = 0,
                                MemoryMb = 0,
                                Error = $"Exception: {ex.Message}"
                            });
                        }
                    }
                }
                return Ok(new CodeExecutionResponse { Results = results });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new CodeExecutionResponse { Results = new List<TestCaseResult>(), Error = ex.Message });
            }
        }

        /// <summary>
        /// Execute code with activity tracking for user interactions.
        /// </summary>
        /// <param name="request">The tracked code execution request with user info.</param>
        /// <returns>Results for each test case with activity tracking.</returns>
        [HttpPost("tracked")]
        public async Task<IActionResult> ExecuteCodeWithTracking([FromBody] TrackedCodeExecutionRequest request)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Language) || string.IsNullOrWhiteSpace(request.Code) || request.TestCases == null || request.TestCases.Count == 0)
            {
                return BadRequest(new { error = "Missing or invalid required properties: language, code, or testCases." });
            }

            try
            {
                // Log user activity
                var startTime = DateTime.UtcNow;
                var activityLog = await _activityTrackingService.LogUserActivityAsync(
                    request.UserId,
                    request.ProblemId,
                    request.AttemptNumber,
                    "run",
                    0 // Will be updated after execution
                );

                // Execute code (reuse existing logic)
                var originalRequest = new CodeExecutionRequest
                {
                    Language = request.Language,
                    Code = request.Code,
                    TestCases = request.TestCases
                };

                var executionResult = await ExecuteCode(originalRequest) as OkObjectResult;
                var response = executionResult?.Value as CodeExecutionResponse;

                if (response == null)
                {
                    return StatusCode(500, new { error = "Failed to execute code" });
                }

                // Calculate execution time
                var endTime = DateTime.UtcNow;
                var timeTaken = (endTime - startTime).TotalSeconds;

                // Update activity log with execution time
                await _activityTrackingService.UpdateActivityMetricsAsync(
                    activityLog.Id,
                    timeTakenSeconds: (int)timeTaken
                );

                // Create or update question result
                var passedCount = response.Results.Count(r => r.Passed);
                var failedCount = response.Results.Count(r => !r.Passed);

                var questionResult = await _activityTrackingService.CreateOrUpdateQuestionResultAsync(
                    request.UserId,
                    request.ProblemId,
                    request.AttemptNumber,
                    request.Language,
                    request.Code,
                    response.Results.Count,
                    passedCount,
                    failedCount
                );

                // Log individual test case results
                for (int i = 0; i < response.Results.Count; i++)
                {
                    var result = response.Results[i];
                    await _activityTrackingService.LogTestCaseResultAsync(
                        questionResult.Id,
                        request.UserId,
                        request.ProblemId,
                        i + 1, // Test case ID
                        result.Passed,
                        result.Output,
                        result.Expected,
                        result.RuntimeMs
                    );
                }

                // Update activity log with test case results
                var passedIds = string.Join(",", response.Results.Select((r, i) => new { r.Passed, Index = i + 1 }).Where(x => x.Passed).Select(x => x.Index));
                var failedIds = string.Join(",", response.Results.Select((r, i) => new { r.Passed, Index = i + 1 }).Where(x => !x.Passed).Select(x => x.Index));

                await _activityTrackingService.UpdateActivityMetricsAsync(
                    activityLog.Id,
                    passedTestCaseIDs: passedIds,
                    failedTestCaseIDs: failedIds
                );

                return Ok(new TrackedCodeExecutionResponse
                {
                    Results = response.Results,
                    ActivityLogId = activityLog.Id,
                    QuestionResultId = questionResult.Id,
                    TimeTakenSeconds = timeTaken
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to execute code with tracking", details = ex.Message });
            }
        }

        /// <summary>
        /// Submit final solution with comprehensive activity tracking.
        /// </summary>
        /// <param name="request">The final submission request.</param>
        /// <returns>Submission results with activity tracking.</returns>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitCode([FromBody] SubmitSolutionRequest request)
        {
            Console.WriteLine($"🔄 SubmitCode called with UserId: {request.UserId}, ProblemId: {request.ProblemId}");

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Language) || string.IsNullOrWhiteSpace(request.Code))
            {
                Console.WriteLine("❌ Invalid request data");
                return BadRequest(new { error = "Missing or invalid required properties: language or code." });
            }

            try
            {
                // ✅ STEP 1: Create initial activity log with start time
                var sessionStartTime = DateTime.UtcNow;
                Console.WriteLine($"🔄 Creating activity log at {sessionStartTime}");

                var activityLog = await _activityTrackingService.LogUserActivityAsync(
                    request.UserId,
                    request.ProblemId,
                    request.AttemptNumber,
                    "submit",
                    0 // Will be updated after execution
                );

                Console.WriteLine($"✅ Activity log created with ID: {activityLog.Id}");

                // ✅ STEP 2: Execute against all test cases for the problem
                var testCases = _db.TestCases.Where(tc => tc.ProblemId == request.ProblemId).ToList();
                Console.WriteLine($"✅ Found {testCases.Count} test cases for problem {request.ProblemId}");

                if (!testCases.Any())
                {
                    Console.WriteLine("❌ No test cases found");
                    return BadRequest(new { error = "No test cases found for this problem." });
                }

                var originalRequest = new CodeExecutionRequest
                {
                    Language = request.Language,
                    Code = request.Code,
                    TestCases = testCases
                };

                var executionResult = await ExecuteCode(originalRequest) as OkObjectResult;
                var response = executionResult?.Value as CodeExecutionResponse;

                if (response == null)
                {
                    Console.WriteLine("❌ Code execution failed");
                    return StatusCode(500, new { error = "Failed to execute code" });
                }

                // ✅ STEP 3: Calculate execution time and prepare test case results
                var endTime = DateTime.UtcNow;
                var timeTaken = (endTime - sessionStartTime).TotalSeconds;

                Console.WriteLine($"✅ Code execution completed in {timeTaken} seconds");

                // ✅ STEP 4: Extract passed and failed test case IDs
                var passedTestCaseIds = response.Results
                    .Where(r => r.Passed)
                    .Select((r, index) => testCases[index].Id.ToString())
                    .ToList();

                var failedTestCaseIds = response.Results
                    .Where(r => !r.Passed)
                    .Select((r, index) => testCases[index].Id.ToString())
                    .ToList();

                Console.WriteLine($"✅ Passed: {passedTestCaseIds.Count}, Failed: {failedTestCaseIds.Count}");

                // ✅ STEP 5: Update activity log with complete metrics
                Console.WriteLine($"🔄 Updating activity log {activityLog.Id} with metrics");

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
                    endTime: endTime
                );

                Console.WriteLine($"✅ Activity log updated successfully");

                // ✅ STEP 6: Create or update question result
                var passedCount = response.Results.Count(r => r.Passed);
                var failedCount = response.Results.Count(r => !r.Passed);

                var questionResult = await _activityTrackingService.CreateOrUpdateQuestionResultAsync(
                    request.UserId,
                    request.ProblemId,
                    request.AttemptNumber,
                    request.Language,
                    request.Code,
                    response.Results.Count,
                    passedCount,
                    failedCount
                );

                Console.WriteLine($"✅ Question result created/updated with ID: {questionResult.Id}");

                // ✅ STEP 7: Log individual test case results
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

                // ✅ STEP 8: Return comprehensive response
                var finalResponse = new SubmitSolutionResponse
                {
                    Results = response.Results,
                    ActivityLogId = activityLog.Id,
                    QuestionResultId = questionResult.Id,
                    TimeTakenSeconds = timeTaken,
                    TotalTestCases = response.Results.Count,
                    PassedTestCases = passedCount,
                    FailedTestCases = failedCount,
                    Success = failedCount == 0
                };

                Console.WriteLine($"✅ Returning response with ActivityLogId: {activityLog.Id}");
                return Ok(finalResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SubmitCode: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to submit code", details = ex.Message });
            }
        }

        private ICodeExecutionService? GetExecutionService(string language)
        {
            return language.ToLower() switch
            {
                "python" => _pythonService,
                "javascript" or "js" => _javascriptService,
                "java" => _javaService,
                "cpp" => _cppService,
                _ => null
            };
        }

        private (bool IsValid, List<string> Errors) ValidateCodeExecutionRequest(CodeExecutionRequest request)
        {
            var errors = new List<string>();

            if (request == null)
            {
                errors.Add("Request cannot be null");
                return (false, errors);
            }

            if (string.IsNullOrWhiteSpace(request.Language))
            {
                errors.Add("Language is required");
            }

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                errors.Add("Code is required");
            }
            else if (request.Code.Length > 10000) // Limit code size
            {
                errors.Add("Code exceeds maximum length of 10,000 characters");
            }

            if (request.TestCases == null || !request.TestCases.Any())
            {
                errors.Add("At least one test case is required");
            }
            else if (request.TestCases.Count > 20) // Limit number of test cases
            {
                errors.Add("Maximum 20 test cases allowed per execution");
            }

            return (errors.Count == 0, errors);
        }

        // 🔧 DEVELOPMENT MODE: Security checks disabled for faster development
        // private (bool IsSafe, string Reason) CheckCodeSecurity(string code, string language)
        // {
        //     // Security checks removed for development
        //     return (true, "");
        // }

        // private bool IsAllowedImport(string code)
        // {
        //     // All imports allowed in development
        //     return true;
        // }
    }
} 