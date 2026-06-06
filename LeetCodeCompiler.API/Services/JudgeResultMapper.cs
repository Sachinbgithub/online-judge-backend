using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    /// <summary>
    /// Maps <see cref="JudgeResult"/> to API response DTOs used by CodeExecution and QuestionResult endpoints.
    /// </summary>
    public static class JudgeResultMapper
    {
        public static List<TestCaseResult> ToTestCaseResults(JudgeResult judgeResult)
        {
            return judgeResult.TestCaseResults.Select(r => new TestCaseResult
            {
                Input = r.Input,
                Output = r.ActualOutput ?? "",
                Expected = r.ExpectedOutput,
                Passed = r.IsPassed,
                Stdout = r.Stdout,
                Stderr = r.Stderr,
                RuntimeMs = r.ExecutionTimeMs,
                MemoryMb = r.MemoryUsedKB / 1024.0,
                Error = r.ErrorMessage
            }).ToList();
        }

        public static CodeExecutionResponse ToCodeExecutionResponse(JudgeResult judgeResult, double wallClockMs)
        {
            return new CodeExecutionResponse
            {
                Results = ToTestCaseResults(judgeResult),
                ExecutionTime = wallClockMs
            };
        }
    }
}
