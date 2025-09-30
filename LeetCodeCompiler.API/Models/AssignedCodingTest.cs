using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeetCodeCompiler.API.Models
{
    [Table("AssignedCodingTests")]
    public class AssignedCodingTest
    {
        [Key]
        public long AssignedId { get; set; }

        [Required]
        public int CodingTestId { get; set; }

        [Required]
        public long AssignedToUserId { get; set; }

        [Required]
        public byte AssignedToUserType { get; set; } // User type (25, 1, 3, etc.)

        [Required]
        public long AssignedByUserId { get; set; }

        [Required]
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public int TestType { get; set; } = 1002; // Your custom test type

        [Required]
        public byte TestMode { get; set; } = 5; // Your custom test mode

        [Required]
        public bool IsDeleted { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CodingTestId")]
        public virtual CodingTest CodingTest { get; set; } = null!;
    }
}
