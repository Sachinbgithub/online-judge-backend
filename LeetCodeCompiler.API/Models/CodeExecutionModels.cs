using System.Collections.Generic;

namespace LeetCodeCompiler.API.Models
{
    public class CodeExecutionRequest
    {
        public string Language { get; set; } = "";
        public string Code { get; set; } = "";
        public List<TestCase> TestCases { get; set; } = new List<TestCase>();
    }

    public class FlexibleCodeExecutionRequest
    {
        public string Language { get; set; } = "";
        public string Code { get; set; } = "";
        public List<TestCase> TestCases { get; set; } = new();
        public string ProblemType { get; set; } = "";
        public int? ProblemId { get; set; }
    }

    public class ProblemConfig
    {
        public string FunctionName { get; set; } = "";
        public string[] ParameterNames { get; set; } = new string[0];
        public string[] ParameterTypes { get; set; } = new string[0];
        public string ReturnType { get; set; } = "";
        public string InputFormat { get; set; } = "";
        public string OutputFormat { get; set; } = "";
    }

    public class TestCaseResult
    {
        public string Input { get; set; } = "";
        public string Output { get; set; } = "";
        public string Expected { get; set; } = "";
        public bool Passed { get; set; }
        public string Stdout { get; set; } = "";
        public string Stderr { get; set; } = "";
        public double RuntimeMs { get; set; }
        public double MemoryMb { get; set; }
        public string? Error { get; set; }
    }

    public class CodeExecutionResponse
    {
        public List<TestCaseResult> Results { get; set; } = new();
        public string? Error { get; set; }
        public double ExecutionTime { get; set; } // Add this property
    }

    // New models for activity tracking
    public class TrackedCodeExecutionRequest
    {
        public int UserId { get; set; }
        public int ProblemId { get; set; }
        public int AttemptNumber { get; set; }
        public string Language { get; set; } = "";
        public string Code { get; set; } = "";
        public List<TestCase> TestCases { get; set; } = new List<TestCase>();
    }

    public class TrackedCodeExecutionResponse
    {
        public List<TestCaseResult> Results { get; set; } = new();
        public int ActivityLogId { get; set; }
        public int QuestionResultId { get; set; }
        public double TimeTakenSeconds { get; set; }
        public string? Error { get; set; }
    }

    public class SubmitSolutionRequest
    {
        public int UserId { get; set; }
        public int ProblemId { get; set; }
        public int AttemptNumber { get; set; }
        public string Language { get; set; } = "";
        public string Code { get; set; } = "";
        public int RunClickCount { get; set; }
        public int SubmitClickCount { get; set; }
        public int SaveCount { get; set; }
        public int LanguageSwitchCount { get; set; }
        public int EraseCount { get; set; }
        public int LoginLogoutCount { get; set; }
        public bool IsSessionAbandoned { get; set; }
    }

    public class SubmitSolutionResponse
    {
        public List<TestCaseResult> Results { get; set; } = new();
        public int ActivityLogId { get; set; }
        public int QuestionResultId { get; set; }
        public double TimeTakenSeconds { get; set; }
        public int TotalTestCases { get; set; }
        public int PassedTestCases { get; set; }
        public int FailedTestCases { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
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