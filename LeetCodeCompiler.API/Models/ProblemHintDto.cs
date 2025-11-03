using System.ComponentModel.DataAnnotations;

namespace LeetCodeCompiler.API.Models
{
    public class CreateProblemHintRequest
    {
        [Required(ErrorMessage = "ProblemId is required")]
        public int ProblemId { get; set; }

        [Required(ErrorMessage = "Hint is required")]
        [StringLength(1000, ErrorMessage = "Hint cannot exceed 1000 characters")]
        public string Hint { get; set; } = string.Empty;
    }

    public class CreateProblemHintResponse
    {
        public int Id { get; set; }
        public int ProblemId { get; set; }
        public string Hint { get; set; } = string.Empty;
        public string ProblemTitle { get; set; } = string.Empty;
    }

    public class UpdateProblemHintRequest
    {
        [Required(ErrorMessage = "Hint is required")]
        [StringLength(1000, ErrorMessage = "Hint cannot exceed 1000 characters")]
        public string Hint { get; set; } = string.Empty;
    }
}
