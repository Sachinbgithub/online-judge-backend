using System.ComponentModel.DataAnnotations;

namespace LeetCodeCompiler.API.Models
{
    public class DifficultyDto
    {
        public int Id { get; set; }
        public int DifficultyId { get; set; }
        public string DifficultyName { get; set; } = string.Empty;
    }

    public class CreateDifficultyRequest
    {
        [Required(ErrorMessage = "DifficultyId is required")]
        [Range(1, 10, ErrorMessage = "DifficultyId must be between 1 and 10")]
        public int DifficultyId { get; set; }

        [Required(ErrorMessage = "Difficulty name is required")]
        [StringLength(50, ErrorMessage = "Difficulty name cannot exceed 50 characters")]
        public string DifficultyName { get; set; } = string.Empty;
    }

    public class UpdateDifficultyRequest
    {
        [Required(ErrorMessage = "DifficultyId is required")]
        [Range(1, 10, ErrorMessage = "DifficultyId must be between 1 and 10")]
        public int DifficultyId { get; set; }

        [Required(ErrorMessage = "Difficulty name is required")]
        [StringLength(50, ErrorMessage = "Difficulty name cannot exceed 50 characters")]
        public string DifficultyName { get; set; } = string.Empty;
    }

    public class CreateDifficultyResponse
    {
        public int Id { get; set; }
        public int DifficultyId { get; set; }
        public string DifficultyName { get; set; } = string.Empty;
    }

    public class UpdateDifficultyResponse
    {
        public int Id { get; set; }
        public int DifficultyId { get; set; }
        public string DifficultyName { get; set; } = string.Empty;
    }
}
