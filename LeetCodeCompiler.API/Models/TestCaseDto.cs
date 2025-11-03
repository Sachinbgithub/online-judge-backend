using System.ComponentModel.DataAnnotations;

namespace LeetCodeCompiler.API.Models
{
    public class CreateTestCaseRequest
    {
        [Required(ErrorMessage = "ProblemId is required")]
        public int ProblemId { get; set; }

        [StringLength(255, ErrorMessage = "Input cannot exceed 255 characters")]
        public string? Input { get; set; }

        [StringLength(255, ErrorMessage = "ExpectedOutput cannot exceed 255 characters")]
        public string? ExpectedOutput { get; set; }
    }

    public class CreateTestCaseResponse
    {
        public int Id { get; set; }
        public int ProblemId { get; set; }
        public string? Input { get; set; }
        public string? ExpectedOutput { get; set; }
        public string ProblemTitle { get; set; } = string.Empty;
    }

    public class UpdateTestCaseRequest
    {
        [StringLength(255, ErrorMessage = "Input cannot exceed 255 characters")]
        public string? Input { get; set; }

        [StringLength(255, ErrorMessage = "ExpectedOutput cannot exceed 255 characters")]
        public string? ExpectedOutput { get; set; }
    }
}
