namespace LeetCodeCompiler.API.Models
{
    public class Problem
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public string Examples { get; set; } = "";
        public string Constraints { get; set; } = "";
        public string? Hints { get; set; }
        public int? TimeLimit { get; set; }
        public int? MemoryLimit { get; set; }
        public int? SubdomainId { get; set; }
        public int? Difficulty { get; set; }
        // 🔧 DEVELOPMENT MODE: Removed TemplateCategory column reference
        // public string? TemplateCategory { get; set; }
        public List<TestCase> TestCases { get; set; } = new List<TestCase>();
        public List<StarterCode> StarterCodes { get; set; } = new List<StarterCode>();
    }
}