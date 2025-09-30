using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeetCodeCompiler.API.Models
{
    [Table("CodingTestQuestionAttempts")]
    public class CodingTestQuestionAttempt
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CodingTestAttemptId { get; set; }

        [Required]
        public int CodingTestQuestionId { get; set; }

        [Required]
        public int ProblemId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [Required]
        public string Status { get; set; } = "InProgress"; // InProgress, Completed, Skipped

        [Required]
        public string LanguageUsed { get; set; } = "";

        [Required]
        public string CodeSubmitted { get; set; } = "";

        public int Score { get; set; } = 0;

        public int MaxScore { get; set; } = 0;

        public int TestCasesPassed { get; set; } = 0;

        public int TotalTestCases { get; set; } = 0;

        public double ExecutionTime { get; set; } = 0.0;

        public int RunCount { get; set; } = 0;

        public int SubmitCount { get; set; } = 0;

        public bool IsCorrect { get; set; } = false;

        [StringLength(1000)]
        public string ErrorMessage { get; set; } = "";

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CodingTestAttemptId")]
        public virtual CodingTestAttempt CodingTestAttempt { get; set; } = null!;

        [ForeignKey("CodingTestQuestionId")]
        public virtual CodingTestQuestion CodingTestQuestion { get; set; } = null!;
    }
}
