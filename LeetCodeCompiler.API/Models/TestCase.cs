namespace LeetCodeCompiler.API.Models
{
    public class TestCase
    {
        public int Id { get; set; }
        public int ProblemId { get; set; }
        public string Input { get; set; } = "";
        public string ExpectedOutput { get; set; } = "";
    }
} 