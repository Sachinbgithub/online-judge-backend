using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeetCodeCompiler.API.Models
{
    [Table("CodingTestTopicData")]
    public class CodingTestTopicData
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CodingTestId { get; set; }

        [Required]
        public int SectionId { get; set; }

        [Required]
        public int DomainId { get; set; }

        [Required]
        public int SubdomainId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CodingTestId")]
        public virtual CodingTest CodingTest { get; set; } = null!;
    }
}
