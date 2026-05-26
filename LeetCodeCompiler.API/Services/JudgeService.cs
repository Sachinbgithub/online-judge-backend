using LeetCodeCompiler.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeetCodeCompiler.API.Services
{
    /// <summary>
    /// Loads official test cases from the database and executes code using the same
    /// language services as <see cref="Controllers.CodeExecutionController"/>.
    /// </summary>
    public class JudgeService : IJudgeService
    {
        private readonly AppDbContext _context;
        private readonly PythonExecutionService _pythonService;
        private readonly JavaScriptExecutionService _javascriptService;
        private readonly JavaExecutionService _javaService;
        private readonly CppExecutionService _cppService;
        private readonly CExecutionService _cService;
        private readonly ILogger<JudgeService> _logger;

        public JudgeService(
            AppDbContext context,
            PythonExecutionService pythonService,
            JavaScriptExecutionService javascriptService,
            JavaExecutionService javaService,
            CppExecutionService cppService,
            CExecutionService cService,
            ILogger<JudgeService> logger)
        {
            _context = context;
            _pythonService = pythonService;
            _javascriptService = javascriptService;
            _javaService = javaService;
            _cppService = cppService;
            _cService = cService;
            _logger = logger;
        }

        public async Task<JudgeResult> EvaluateAsync(int problemId, string language, string code)
        {
            var result = new JudgeResult();
            var lang = (language ?? "").Trim();
            if (string.IsNullOrEmpty(lang))
                throw new ArgumentException("Language is required for evaluation.", nameof(language));

            var service = GetExecutionService(lang);
            if (service == null)
                throw new ArgumentException($"Unsupported language for evaluation: {language}", nameof(language));

            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code is required for evaluation.", nameof(code));

            var testCases = await _context.TestCases
                .AsNoTracking()
                .Where(tc => tc.ProblemId == problemId)
                .OrderBy(tc => tc.Id)
                .ToListAsync();

            result.TotalTestCases = testCases.Count;
            if (testCases.Count == 0)
            {
                result.PassedTestCases = 0;
                result.FailedTestCases = 0;
                result.IsCorrect = false;
                return result;
            }

            var order = 0;
            foreach (var tc in testCases)
            {
                order++;
                var expected = tc.ExpectedOutput ?? "";
                var input = tc.Input ?? "";

                JudgeTestCaseResult row;
                try
                {
                    var exec = await service.ExecuteAsync(code, input);
                    var actual = exec.Output ?? "";
                    var passed = string.IsNullOrEmpty(exec.Error) && actual.Trim() == expected.Trim();

                    row = new JudgeTestCaseResult
                    {
                        TestCaseId = tc.Id,
                        TestCaseOrder = order,
                        Input = input,
                        ExpectedOutput = expected,
                        ActualOutput = actual,
                        IsPassed = passed,
                        ExecutionTimeMs = (int)Math.Round(exec.RuntimeMs),
                        MemoryUsedKB = (int)Math.Round(exec.MemoryMb * 1024.0),
                        ErrorMessage = string.IsNullOrEmpty(exec.Error) ? null : exec.Error,
                        ErrorType = ClassifyError(passed, exec)
                    };

                    result.ExecutionTimeMs += row.ExecutionTimeMs;
                    result.MemoryUsedKB += row.MemoryUsedKB;
                    if (passed)
                        result.PassedTestCases++;
                    else
                        result.FailedTestCases++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Judge execution failed for ProblemId={ProblemId}, TestCaseId={TestCaseId}", problemId, tc.Id);
                    row = new JudgeTestCaseResult
                    {
                        TestCaseId = tc.Id,
                        TestCaseOrder = order,
                        Input = input,
                        ExpectedOutput = expected,
                        ActualOutput = null,
                        IsPassed = false,
                        ExecutionTimeMs = 0,
                        MemoryUsedKB = 0,
                        ErrorMessage = "Execution failed: " + ex.Message,
                        ErrorType = "RuntimeError"
                    };
                    result.FailedTestCases++;
                }

                result.TestCaseResults.Add(row);
            }

            result.IsCorrect = result.TotalTestCases > 0 && result.PassedTestCases == result.TotalTestCases;
            return result;
        }

        private static string? ClassifyError(bool passed, ExecutionResult exec)
        {
            if (passed)
                return null;
            if (!string.IsNullOrEmpty(exec.Error))
                return exec.Error.Contains("compile", StringComparison.OrdinalIgnoreCase) ? "CompilationError" : "RuntimeError";
            if (!string.IsNullOrEmpty(exec.Stderr))
                return "RuntimeError";
            return "WrongAnswer";
        }

        private ICodeExecutionService? GetExecutionService(string language)
        {
            return language.ToLowerInvariant() switch
            {
                "python" => _pythonService,
                "javascript" or "js" => _javascriptService,
                "java" => _javaService,
                "cpp" or "c++" => _cppService,
                "c" => _cService,
                _ => null
            };
        }
    }
}
