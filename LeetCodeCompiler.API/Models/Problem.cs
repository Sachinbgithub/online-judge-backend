namespace LeetCodeCompiler.API.Models
{
    public class Problem
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string Examples { get; set; }
        public required string Constraints { get; set; }
        public int? Hints { get; set; } // Optional field - matches database int
        public required int TimeLimit { get; set; } // Required field
        public required int MemoryLimit { get; set; } // Required field
        public required int SubdomainId { get; set; } // Required field
        public required int Difficulty { get; set; } // Required field (1-3)
        public int? StreamId { get; set; }
        // 🔧 DEVELOPMENT MODE: Removed TemplateCategory column reference
        // public string? TemplateCategory { get; set; }
        public List<TestCase> TestCases { get; set; } = new List<TestCase>();
        public List<StarterCode> StarterCodes { get; set; } = new List<StarterCode>();
    }
}