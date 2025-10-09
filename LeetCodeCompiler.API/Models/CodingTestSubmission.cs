using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeetCodeCompiler.API.Models
{
    [Table("CodingTestSubmissions")]
    public class CodingTestSubmission
    {
        [Key]
        public long SubmissionId { get; set; }

        [Required]
        public int CodingTestId { get; set; }

        [Required]
        public int CodingTestAttemptId { get; set; }

        public int? CodingTestQuestionAttemptId { get; set; }

        public int? ProblemId { get; set; }

        [Required]
        public long UserId { get; set; }

        [Required]
        public int AttemptNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string LanguageUsed { get; set; } = "";

        [Required]
        public string FinalCodeSnapshot { get; set; } = "";

        public int TotalTestCases { get; set; } = 0;
        public int PassedTestCases { get; set; } = 0;
        public int FailedTestCases { get; set; } = 0;
        public bool RequestedHelp { get; set; } = false;

        // Activity Tracking Metrics
        public int LanguageSwitchCount { get; set; } = 0;
        public int RunClickCount { get; set; } = 0;
        public int SubmitClickCount { get; set; } = 0;
        public int EraseCount { get; set; } = 0;
        public int SaveCount { get; set; } = 0;
        public int LoginLogoutCount { get; set; } = 0;
        public bool IsSessionAbandoned { get; set; } = false;

        // Submission Details
        public DateTime SubmissionTime { get; set; } = DateTime.UtcNow;
        public int ExecutionTimeMs { get; set; } = 0;
        public int MemoryUsedKB { get; set; } = 0;
        public int Score { get; set; } = 0;
        public int MaxScore { get; set; } = 0;
        public bool IsCorrect { get; set; } = false;
        public bool IsLateSubmission { get; set; } = false;

        // Additional Metadata
        public int? ClassId { get; set; }
        [StringLength(50)]
        public string? UserIP { get; set; }
        [StringLength(500)]
        public string? UserAgent { get; set; }
        [StringLength(200)]
        public string? BrowserInfo { get; set; }
        [StringLength(200)]
        public string? DeviceInfo { get; set; }

        // Error Handling
        [StringLength(1000)]
        public string? ErrorMessage { get; set; }
        [StringLength(100)]
        public string? ErrorType { get; set; }
        [StringLength(1000)]
        public string? CompilationError { get; set; }
        [StringLength(1000)]
        public string? RuntimeError { get; set; }

        // Timestamps
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CodingTestId")]
        public virtual CodingTest CodingTest { get; set; } = null!;

        [ForeignKey("CodingTestAttemptId")]
        public virtual CodingTestAttempt CodingTestAttempt { get; set; } = null!;

        [ForeignKey("CodingTestQuestionAttemptId")]
        public virtual CodingTestQuestionAttempt CodingTestQuestionAttempt { get; set; } = null!;

        [ForeignKey("ProblemId")]
        public virtual Problem Problem { get; set; } = null!;

        public virtual ICollection<CodingTestSubmissionResult> SubmissionResults { get; set; } = new List<CodingTestSubmissionResult>();
    }

    [Table("CodingTestSubmissionResults")]
    public class CodingTestSubmissionResult
    {
        [Key]
        public long ResultId { get; set; }

        [Required]
        public long SubmissionId { get; set; }

        [Required]
        public int TestCaseId { get; set; }

        [Required]
        public int ProblemId { get; set; }

        [Required]
        public int TestCaseOrder { get; set; }

        [Required]
        public string Input { get; set; } = "";

        [Required]
        public string ExpectedOutput { get; set; } = "";

        public string? ActualOutput { get; set; }
        public bool IsPassed { get; set; } = false;
        public int ExecutionTimeMs { get; set; } = 0;
        public int MemoryUsedKB { get; set; } = 0;
        [StringLength(1000)]
        public string? ErrorMessage { get; set; }
        [StringLength(100)]
        public string? ErrorType { get; set; } // CompilationError, RuntimeError, TimeoutError, WrongAnswer
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("SubmissionId")]
        public virtual CodingTestSubmission Submission { get; set; } = null!;

        [ForeignKey("TestCaseId")]
        public virtual TestCase TestCase { get; set; } = null!;

        [ForeignKey("ProblemId")]
        public virtual Problem Problem { get; set; } = null!;
    }
}
