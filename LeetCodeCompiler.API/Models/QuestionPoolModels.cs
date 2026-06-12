using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeetCodeCompiler.API.Models
{
    [Table("QuestionPools")]
    public class QuestionPool
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = "";

        public int? DomainId { get; set; }

        public int? SubdomainId { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public int CreatedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<QuestionPoolItem> Items { get; set; } = new List<QuestionPoolItem>();
    }

    [Table("QuestionPoolItems")]
    public class QuestionPoolItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PoolId { get; set; }

        [Required]
        public int ProblemId { get; set; }

        public int Weight { get; set; } = 1;

        [ForeignKey("PoolId")]
        public virtual QuestionPool Pool { get; set; } = null!;

        [ForeignKey("ProblemId")]
        public virtual Problem Problem { get; set; } = null!;
    }

    [Table("CodingTestPoolSections")]
    public class CodingTestPoolSection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CodingTestId { get; set; }

        [Required]
        public int PoolId { get; set; }

        [Required]
        public int QuestionsToPick { get; set; }

        [Required]
        public int SectionOrder { get; set; }

        [Required]
        [Column(TypeName = "decimal(6,2)")]
        public decimal MarksPerQuestion { get; set; }

        [Required]
        public int TimeLimitMinutes { get; set; }

        [StringLength(1000)]
        public string CustomInstructions { get; set; } = "";

        [ForeignKey("CodingTestId")]
        public virtual CodingTest CodingTest { get; set; } = null!;

        [ForeignKey("PoolId")]
        public virtual QuestionPool Pool { get; set; } = null!;
    }

    [Table("CodingTestAttemptQuestions")]
    public class CodingTestAttemptQuestion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CodingTestAttemptId { get; set; }

        [Required]
        public int ProblemId { get; set; }

        [Required]
        public int QuestionOrder { get; set; }

        [Required]
        [Column(TypeName = "decimal(6,2)")]
        public decimal Marks { get; set; }

        [Required]
        public int TimeLimitMinutes { get; set; }

        [Required]
        [StringLength(10)]
        public string Source { get; set; } = "Fixed";

        public int? CodingTestQuestionId { get; set; }

        public int? PoolSectionId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [ForeignKey("CodingTestAttemptId")]
        public virtual CodingTestAttempt CodingTestAttempt { get; set; } = null!;

        [ForeignKey("ProblemId")]
        public virtual Problem Problem { get; set; } = null!;
    }
}
