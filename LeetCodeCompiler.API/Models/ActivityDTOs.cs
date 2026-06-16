using System.ComponentModel.DataAnnotations;

namespace LeetCodeCompiler.API.Models
{
    public class LogUserActivityRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int ProblemId { get; set; }

        public int AttemptNumber { get; set; }

        public string TestType { get; set; } = "session";

        public int TimeTakenSeconds { get; set; }

        public int? CodingTestId { get; set; }
        public int? CodingTestAttemptId { get; set; }
        public int? CodingTestQuestionAttemptId { get; set; }

        public string Source { get; set; } = "practice";
    }

    public class UpdateActivityMetricsRequest
    {
        public int? TimeTakenSeconds { get; set; }
        public int? LanguageSwitchCount { get; set; }
        public int? EraseCount { get; set; }
        public int? SaveCount { get; set; }
        public int? RunClickCount { get; set; }
        public int? SubmitClickCount { get; set; }
        public int? LoginLogoutCount { get; set; }
        public bool? IsSessionAbandoned { get; set; }
        public string? PassedTestCaseIDs { get; set; }
        public string? FailedTestCaseIDs { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class AssessmentActivityMetrics
    {
        public int? TimeTakenSeconds { get; set; }
        public int? LanguageSwitchCount { get; set; }
        public int? EraseCount { get; set; }
        public int? SaveCount { get; set; }
        public int? RunClickCount { get; set; }
        public int? SubmitClickCount { get; set; }
        public int? LoginLogoutCount { get; set; }
        public bool? IsSessionAbandoned { get; set; }
        public string? PassedTestCaseIDs { get; set; }
        public string? FailedTestCaseIDs { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class UserActivityLogResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProblemId { get; set; }
        public int AttemptNumber { get; set; }
        public string TestType { get; set; } = "";
        public int TimeTakenSeconds { get; set; }
        public int LanguageSwitchCount { get; set; }
        public int EraseCount { get; set; }
        public int SaveCount { get; set; }
        public int RunClickCount { get; set; }
        public int SubmitClickCount { get; set; }
        public int LoginLogoutCount { get; set; }
        public bool IsSessionAbandoned { get; set; }
        public string PassedTestCaseIDs { get; set; } = "";
        public string FailedTestCaseIDs { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int? CodingTestId { get; set; }
        public int? CodingTestAttemptId { get; set; }
        public int? CodingTestQuestionAttemptId { get; set; }
        public long? SubmissionId { get; set; }
        public string? SessionStatus { get; set; }
        public string Source { get; set; } = "practice";
    }

    public class ProblemActivitySessionResponse
    {
        public int ProblemId { get; set; }
        public List<UserActivityLogResponse> ActivityLogs { get; set; } = new();
        public List<CodingTestSubmissionSummaryResponse> Submissions { get; set; } = new();
    }

    public class AttemptActivityReviewResponse
    {
        public int CodingTestAttemptId { get; set; }
        public int CodingTestId { get; set; }
        public int UserId { get; set; }
        public string IntegrityStatus { get; set; } = "Normal";
        public string AttemptStatus { get; set; } = "";
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public decimal TotalScore { get; set; }
        public decimal MaxScore { get; set; }
        public double Percentage { get; set; }
        public ProctoringStatusResponse ProctoringSummary { get; set; } = new();
        public List<ProctoringEventDto> ProctoringEvents { get; set; } = new();
        public List<IntegrityFlagResponse> IntegrityFlags { get; set; } = new();
        public List<CodeActivitySnapshotResponse> CodeActivitySnapshots { get; set; } = new();
        public List<ProblemActivitySessionResponse> ProblemSessions { get; set; } = new();
        public AttemptAggregatedMetricsResponse AggregatedMetrics { get; set; } = new();
    }

    public class AttemptAggregatedMetricsResponse
    {
        public int TotalRunClicks { get; set; }
        public int TotalSubmitClicks { get; set; }
        public int TotalEraseCount { get; set; }
        public int TotalSaveCount { get; set; }
        public int TotalLanguageSwitches { get; set; }
        public int TotalLoginLogoutCount { get; set; }
        public int TotalTimeSpentSeconds { get; set; }
    }

    public class CodeActivitySnapshotResponse
    {
        public long Id { get; set; }
        public int CodingTestAttemptId { get; set; }
        public int? ProblemId { get; set; }
        public DateTime Timestamp { get; set; }
        public int CodeLength { get; set; }
        public int DeltaChars { get; set; }
        public string Source { get; set; } = "";
        public int? PasteLength { get; set; }
        public string? CodeHash { get; set; }
    }
}
