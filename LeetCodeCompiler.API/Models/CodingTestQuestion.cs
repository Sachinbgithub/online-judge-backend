using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeetCodeCompiler.API.Models
{
    [Table("CodingTestQuestions")]
    public class CodingTestQuestion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CodingTestId { get; set; }

        [Required]
        public int ProblemId { get; set; } // Reference to existing Problem

        [Required]
        public int QuestionOrder { get; set; } // Order of question in test

        [Required]
        public int Marks { get; set; }

        [Required]
        public int TimeLimitMinutes { get; set; } // Individual question time limit

        [StringLength(1000)]
        public string CustomInstructions { get; set; } = "";

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CodingTestId")]
        public virtual CodingTest CodingTest { get; set; } = null!;

        [ForeignKey("ProblemId")]
        public virtual Problem Problem { get; set; } = null!;
    }
}
