using System.ComponentModel.DataAnnotations;

namespace LeetCodeCompiler.API.Models
{
    public class CreateLanguageRequest
    {
        [Required(ErrorMessage = "Language name is required")]
        [StringLength(50, ErrorMessage = "Language name cannot exceed 50 characters")]
        public string LanguageName { get; set; } = string.Empty;
    }

    public class CreateLanguageResponse
    {
        public int Id { get; set; }
        public string LanguageName { get; set; } = string.Empty;
    }

    public class UpdateLanguageRequest
    {
        [Required(ErrorMessage = "Language name is required")]
        [StringLength(50, ErrorMessage = "Language name cannot exceed 50 characters")]
        public string LanguageName { get; set; } = string.Empty;
    }
}
