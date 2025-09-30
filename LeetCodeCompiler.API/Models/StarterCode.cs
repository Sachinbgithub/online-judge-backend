namespace LeetCodeCompiler.API.Models
{
    public class StarterCode
    {
        public int Id { get; set; }
        public int ProblemId { get; set; }
        public required string Language { get; set; }
        public required string Code { get; set; }
    }
} 
