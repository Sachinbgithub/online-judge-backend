using System.ComponentModel.DataAnnotations.Schema;

namespace LeetCodeCompiler.API.Models
{
    [Table("CoreQuestionResult")]
    public class CoreQuestionResult
    {
        public int Id { get; set; }
        public int ProblemId { get; set; }
        public int UserId { get; set; }
        public int AttemptNumber { get; set; }
        public int TotalTestCases { get; set; }
        public int PassedTestCases { get; set; }
        public int FailedTestCases { get; set; }
        public string LanguageUsed { get; set; } = "";
        public string FinalCodeSnapshot { get; set; } = "";
        public bool RequestedHelp { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastSubmittedAt { get; set; }
    }
} 