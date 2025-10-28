using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeetCodeCompiler.API.Models
{
    [Table("Difficulty")]
    public class Difficulty
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "DifficultyId is required")]
        public int DifficultyId { get; set; }

        [Required(ErrorMessage = "Difficulty name is required")]
        [StringLength(50, ErrorMessage = "Difficulty name cannot exceed 50 characters")]
        [Column("Difficulty")] // Explicitly map to 'Difficulty' column in DB
        public required string DifficultyName { get; set; }
    }
}
