using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeetCodeCompiler.API.Models
{
    [Table("CodingTestResumeGrants")]
    public class CodingTestResumeGrant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CodingTestId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int PriorAttemptId { get; set; }

        [Required]
        public int GrantedByUserId { get; set; }

        [Required]
        public DateTime GrantedAt { get; set; }

        public int ActiveTimeSecondsAtGrant { get; set; }

        [Required]
        public DateTime AllowedEndAt { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = ResumeGrantStatuses.Pending;

        public int? UsedByAttemptId { get; set; }

        [ForeignKey(nameof(CodingTestId))]
        public virtual CodingTest? CodingTest { get; set; }

        [ForeignKey(nameof(PriorAttemptId))]
        public virtual CodingTestAttempt? PriorAttempt { get; set; }
    }
}
