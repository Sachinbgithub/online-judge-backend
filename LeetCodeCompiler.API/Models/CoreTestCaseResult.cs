using System.ComponentModel.DataAnnotations.Schema;

namespace LeetCodeCompiler.API.Models
{
    [Table("CoreTestCaseResult")]
    public class CoreTestCaseResult
    {
        public int Id { get; set; }
        public int CoreQuestionResultId { get; set; }
        public int ProblemId { get; set; }
        public int UserId { get; set; }
        public int TestCaseId { get; set; }
        public bool IsPassed { get; set; }
        public string UserOutput { get; set; } = "";
        public string ExpectedOutput { get; set; } = "";
        public double ExecutionTime { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 