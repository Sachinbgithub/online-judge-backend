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
        public decimal TotalMarks { get; set; }

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

        [Required]
        public bool IsGlobal { get; set; } = false;

        public int CollegeId { get; set; } = 0;

        public int WarningThreshold { get; set; } = 3;

        public int FlagThreshold { get; set; } = 5;

        public bool RequireFullscreen { get; set; } = false;

        public bool BlockPaste { get; set; } = false;

        public bool EnableProctoring { get; set; } = true;

        public bool EnablePlagiarismCheck { get; set; } = true;

        // Navigation properties
        public virtual ICollection<CodingTestQuestion> Questions { get; set; } = new List<CodingTestQuestion>();
        public virtual ICollection<CodingTestPoolSection> PoolSections { get; set; } = new List<CodingTestPoolSection>();
        public virtual ICollection<CodingTestAttempt> Attempts { get; set; } = new List<CodingTestAttempt>();
        public virtual ICollection<CodingTestTopicData> TopicData { get; set; } = new List<CodingTestTopicData>();
    }
}
