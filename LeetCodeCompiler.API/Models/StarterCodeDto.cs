using System.ComponentModel.DataAnnotations;

namespace LeetCodeCompiler.API.Models
{
    public class CreateStarterCodeRequest
    {
        [Required(ErrorMessage = "ProblemId is required")]
        public int ProblemId { get; set; }

        [Required(ErrorMessage = "Language is required")]
        [Range(1, 10, ErrorMessage = "Language must be between 1 and 10")]
        public int Language { get; set; }

        [Required(ErrorMessage = "Code is required")]
        public string Code { get; set; } = string.Empty;
    }

    public class CreateStarterCodeResponse
    {
        public int Id { get; set; }
        public int ProblemId { get; set; }
        public int Language { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ProblemTitle { get; set; } = string.Empty;
    }

    public class UpdateStarterCodeRequest
    {
        [Required(ErrorMessage = "Language is required")]
        [Range(1, 10, ErrorMessage = "Language must be between 1 and 10")]
        public int Language { get; set; }

        [Required(ErrorMessage = "Code is required")]
        public string Code { get; set; } = string.Empty;
    }
}
