using System.ComponentModel.DataAnnotations;

namespace LeetCodeCompiler.API.Models
{
    public class CreateProblemRequest
    {
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Examples is required")]
        public string Examples { get; set; } = string.Empty;

        [Required(ErrorMessage = "Constraints is required")]
        public string Constraints { get; set; } = string.Empty;

        public int? Hints { get; set; } // Optional field - matches database int

        [Required(ErrorMessage = "TimeLimit is required")]
        [Range(1, 60, ErrorMessage = "TimeLimit must be between 1 and 60 seconds")]
        public int TimeLimit { get; set; } = 5; // Default 5 seconds

        [Required(ErrorMessage = "MemoryLimit is required")]
        [Range(64, 1024, ErrorMessage = "MemoryLimit must be between 64 and 1024 MB")]
        public int MemoryLimit { get; set; } = 256; // Default 256 MB

        [Required(ErrorMessage = "SubdomainId is required")]
        public int SubdomainId { get; set; } = 9; // Default to subdomain ID 9

        [Required(ErrorMessage = "Difficulty is required")]
        [Range(1, 3, ErrorMessage = "Difficulty must be between 1 and 3")]
        public int Difficulty { get; set; }
    }

    public class CreateProblemResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Examples { get; set; } = string.Empty;
        public string Constraints { get; set; } = string.Empty;
        public int? Hints { get; set; }
        public int TimeLimit { get; set; }
        public int MemoryLimit { get; set; }
        public int SubdomainId { get; set; }
        public int Difficulty { get; set; }
        public string SubdomainName { get; set; } = string.Empty;
        public string DomainName { get; set; } = string.Empty;
        public List<TestCase> TestCases { get; set; } = new List<TestCase>();
        public List<StarterCode> StarterCodes { get; set; } = new List<StarterCode>();
    }
}
