using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeetCodeCompiler.API.Models
{
    [Table("CodingTestAttempts")]
    public class CodingTestAttempt
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CodingTestId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int AttemptNumber { get; set; }

        [Required]
        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? SubmittedAt { get; set; }

        [Required]
        public string Status { get; set; } = "InProgress"; // InProgress, Completed, Submitted, Abandoned

        public int TotalScore { get; set; } = 0;

        public int MaxScore { get; set; } = 0;

        public double Percentage { get; set; } = 0.0;

        public int TimeSpentMinutes { get; set; } = 0;

        public bool IsLateSubmission { get; set; } = false;

        [StringLength(500)]
        public string Notes { get; set; } = "";

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CodingTestId")]
        public virtual CodingTest CodingTest { get; set; } = null!;

        public virtual ICollection<CodingTestQuestionAttempt> QuestionAttempts { get; set; } = new List<CodingTestQuestionAttempt>();
    }
}
