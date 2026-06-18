using System.ComponentModel.DataAnnotations;

namespace LeetCodeCompiler.API.Models
{
    public class ProctoringEventDto
    {
        [Required]
        public string EventType { get; set; } = "";

        [Required]
        public DateTime OccurredAt { get; set; }

        public int ClientSequence { get; set; }

        public string? PayloadJson { get; set; }
    }

    public class IngestProctoringEventsRequest
    {
        [Required]
        public int CodingTestAttemptId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public List<ProctoringEventDto> Events { get; set; } = new();
    }

    public class ProctoringStatusResponse
    {
        public int CodingTestAttemptId { get; set; }
        public string IntegrityStatus { get; set; } = "Normal";
        public int BreachCount { get; set; }
        public int TabSwitchCount { get; set; }
        public int WindowBlurCount { get; set; }
        public int PasteCount { get; set; }
        public int ScreenshotCount { get; set; }
        public int WarningThreshold { get; set; }
        public int FlagThreshold { get; set; }
        public int BreachRuleLimit { get; set; }
        public bool RequireFullscreen { get; set; }
        public bool BlockPaste { get; set; }
        public string? LastWarning { get; set; }
    }

    public class CodeActivitySnapshotRequest
    {
        [Required]
        public int CodingTestAttemptId { get; set; }

        [Required]
        public int UserId { get; set; }

        public int? ProblemId { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public int CodeLength { get; set; }

        public int DeltaChars { get; set; }

        [Required]
        public string Source { get; set; } = "";

        public int? PasteLength { get; set; }

        public string? CodeHash { get; set; }
    }

    public class IntegrityFlagResponse
    {
        public long Id { get; set; }
        public int CodingTestAttemptId { get; set; }
        public long? SubmissionId { get; set; }
        public string FlagType { get; set; } = "";
        public string Severity { get; set; } = "";
        public string? DetailsJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ReviewStatus { get; set; } = "";
    }

    public class ReviewIntegrityFlagRequest
    {
        [Required]
        public string ReviewStatus { get; set; } = "";
    }

    public class IntegrityReviewSummaryResponse
    {
        public int CodingTestAttemptId { get; set; }
        public int UserId { get; set; }
        public string IntegrityStatus { get; set; } = "";
        public int FlagCount { get; set; }
        public List<IntegrityFlagResponse> Flags { get; set; } = new();
    }

    public class PlagiarismReportResponse
    {
        public long Id { get; set; }
        public long SubmissionId { get; set; }
        public int CodingTestAttemptId { get; set; }
        public int? ProblemId { get; set; }
        public decimal MaxSimilarityScore { get; set; }
        public string Status { get; set; } = "";
        public DateTime? CheckedAt { get; set; }
        public List<PlagiarismMatchResponse> Matches { get; set; } = new();
    }

    public class PlagiarismMatchResponse
    {
        public long MatchedSubmissionId { get; set; }
        public decimal SimilarityScore { get; set; }
        public string MatchType { get; set; } = "";
    }

    public class PoolSectionRequest
    {
        [Required]
        public int PoolId { get; set; }

        [Required]
        [Range(1, 100)]
        public int QuestionsToPick { get; set; }

        [Required]
        public int SectionOrder { get; set; }

        [Required]
        public decimal MarksPerQuestion { get; set; }

        [Required]
        public int TimeLimitMinutes { get; set; }

        public string CustomInstructions { get; set; } = "";
    }

    public class CreateQuestionPoolRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = "";

        public int? DomainId { get; set; }
        public int? SubdomainId { get; set; }
        public int CreatedBy { get; set; }
        public List<int> ProblemIds { get; set; } = new();
    }

    public class QuestionPoolResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int? DomainId { get; set; }
        public int? SubdomainId { get; set; }
        public bool IsActive { get; set; }
        public int ItemCount { get; set; }
        public List<int> ProblemIds { get; set; } = new();
    }

    public class AttemptQuestionResponse
    {
        public int Id { get; set; }
        public int ProblemId { get; set; }
        public int QuestionOrder { get; set; }
        public decimal Marks { get; set; }
        public int TimeLimitMinutes { get; set; }
        public string Source { get; set; } = "";
        public int? CodingTestQuestionId { get; set; }
        public int? PoolSectionId { get; set; }
        public ProblemResponse? Problem { get; set; }
    }
}
