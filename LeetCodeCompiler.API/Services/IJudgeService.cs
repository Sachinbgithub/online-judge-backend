using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    /// <summary>
    /// Server-side evaluation of code against <see cref="TestCase"/> rows.
    /// Used for authoritative scoring and consistent Run/Submit pass-fail rules.
    /// </summary>
    public interface IJudgeService
    {
        /// <summary>
        /// Runs <paramref name="code"/> for <paramref name="problemId"/> in <paramref name="language"/>
        /// against all DB test cases (ordered by Id). Does not persist anything.
        /// </summary>
        Task<JudgeResult> EvaluateAsync(int problemId, string language, string code);

        /// <summary>
        /// Runs <paramref name="code"/> against an explicit list of test cases (e.g. client-provided Run cases).
        /// Does not persist anything.
        /// </summary>
        Task<JudgeResult> EvaluateTestCasesAsync(string language, string code, IReadOnlyList<TestCase> testCases);
    }

    public class JudgeResult
    {
        public int TotalTestCases { get; set; }
        public int PassedTestCases { get; set; }
        public int FailedTestCases { get; set; }
        public bool IsCorrect { get; set; }
        /// <summary>Sum of per-case execution time (ms).</summary>
        public int ExecutionTimeMs { get; set; }
        /// <summary>Sum of per-case memory (KB), derived from runtime MB where available.</summary>
        public int MemoryUsedKB { get; set; }
        public List<JudgeTestCaseResult> TestCaseResults { get; set; } = new();
    }

    public class JudgeTestCaseResult
    {
        public int TestCaseId { get; set; }
        public int TestCaseOrder { get; set; }
        public string Input { get; set; } = "";
        public string ExpectedOutput { get; set; } = "";
        public string? ActualOutput { get; set; }
        public bool IsPassed { get; set; }
        public int ExecutionTimeMs { get; set; }
        public int MemoryUsedKB { get; set; }
        public string Stdout { get; set; } = "";
        public string Stderr { get; set; } = "";
        public string? ErrorMessage { get; set; }
        public string? ErrorType { get; set; }
    }
}
