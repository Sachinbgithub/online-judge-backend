namespace LeetCodeCompiler.API.Services
{
    /// <summary>
    /// Server-side evaluation of code against <see cref="Models.TestCase"/> rows in the database.
    /// Used for authoritative scoring on submit paths (coding tests, practice tests).
    /// </summary>
    public interface IJudgeService
    {
        /// <summary>
        /// Runs <paramref name="code"/> for <paramref name="problemId"/> in <paramref name="language"/>
        /// against all DB test cases. Does not persist anything.
        /// </summary>
        Task<JudgeResult> EvaluateAsync(int problemId, string language, string code);
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
        public string? ErrorMessage { get; set; }
        public string? ErrorType { get; set; }
    }
}
