using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeetCodeCompiler.API.Models
{
    [Table("CodingTests")]
    public class CodingTest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string TestName { get; set; } = "";

        [Required]
        public int CreatedBy { get; set; } // User ID who created the test

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public int DurationMinutes { get; set; }

        [Required]
        public int TotalQuestions { get; set; }

        [Required]
        public int TotalMarks { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public bool IsPublished { get; set; } = false;

        [Required]
        public int TestType { get; set; } = 1; // Custom test type values (1-10000)

        public bool AllowMultipleAttempts { get; set; } = false;

        public int MaxAttempts { get; set; } = 1;

        public bool ShowResultsImmediately { get; set; } = true;

        public bool AllowCodeReview { get; set; } = false;

        [StringLength(100)]
        public string AccessCode { get; set; } = ""; // Optional access code for test

        [StringLength(200)]
        public string Tags { get; set; } = ""; // Comma-separated tags

        // New fields as requested
        public bool IsResultPublishAutomatically { get; set; } = true;

        public bool ApplyBreachRule { get; set; } = true;

        public int BreachRuleLimit { get; set; } = 0;

        [StringLength(50)]
        public string HostIP { get; set; } = "";

        public int ClassId { get; set; } = 0;

        // Navigation properties
        public virtual ICollection<CodingTestQuestion> Questions { get; set; } = new List<CodingTestQuestion>();
        public virtual ICollection<CodingTestAttempt> Attempts { get; set; } = new List<CodingTestAttempt>();
        public virtual ICollection<CodingTestTopicData> TopicData { get; set; } = new List<CodingTestTopicData>();
    }
}
