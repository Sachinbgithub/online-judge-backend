using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeetCodeCompiler.API.Models
{
    [Table("ProctoringSessions")]
    public class ProctoringSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CodingTestAttemptId { get; set; }

        [Required]
        public DateTime StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [ForeignKey("CodingTestAttemptId")]
        public virtual CodingTestAttempt CodingTestAttempt { get; set; } = null!;

        public virtual ICollection<ProctoringEvent> Events { get; set; } = new List<ProctoringEvent>();
    }

    [Table("ProctoringEvents")]
    public class ProctoringEvent
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public int SessionId { get; set; }

        [Required]
        [StringLength(50)]
        public string EventType { get; set; } = "";

        [Required]
        public DateTime OccurredAt { get; set; }

        public int ClientSequence { get; set; }

        public string? PayloadJson { get; set; }

        [ForeignKey("SessionId")]
        public virtual ProctoringSession Session { get; set; } = null!;
    }

    [Table("CodeActivitySnapshots")]
    public class CodeActivitySnapshot
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public int CodingTestAttemptId { get; set; }

        public int? ProblemId { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public int CodeLength { get; set; }

        public int DeltaChars { get; set; }

        [Required]
        [StringLength(20)]
        public string Source { get; set; } = "";

        public int? PasteLength { get; set; }

        [StringLength(64)]
        public string? CodeHash { get; set; }

        [ForeignKey("CodingTestAttemptId")]
        public virtual CodingTestAttempt CodingTestAttempt { get; set; } = null!;
    }

    [Table("IntegrityFlags")]
    public class IntegrityFlag
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public int CodingTestAttemptId { get; set; }

        public long? SubmissionId { get; set; }

        [Required]
        [StringLength(50)]
        public string FlagType { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string Severity { get; set; } = "Medium";

        public string? DetailsJson { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        [StringLength(20)]
        public string ReviewStatus { get; set; } = "Pending";

        public int? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [ForeignKey("CodingTestAttemptId")]
        public virtual CodingTestAttempt CodingTestAttempt { get; set; } = null!;
    }

    [Table("PlagiarismReports")]
    public class PlagiarismReport
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long SubmissionId { get; set; }

        [Required]
        public int CodingTestAttemptId { get; set; }

        public int? ProblemId { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal MaxSimilarityScore { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime? CheckedAt { get; set; }

        public virtual ICollection<PlagiarismMatch> Matches { get; set; } = new List<PlagiarismMatch>();
    }

    [Table("PlagiarismMatches")]
    public class PlagiarismMatch
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long ReportId { get; set; }

        [Required]
        public long MatchedSubmissionId { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal SimilarityScore { get; set; }

        [Required]
        [StringLength(20)]
        public string MatchType { get; set; } = "SameTest";

        [ForeignKey("ReportId")]
        public virtual PlagiarismReport Report { get; set; } = null!;
    }
}
