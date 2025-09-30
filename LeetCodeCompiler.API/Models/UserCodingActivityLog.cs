using System.ComponentModel.DataAnnotations.Schema;

namespace LeetCodeCompiler.API.Models
{
    [Table("UserCodingActivityLog")]
    public class UserCodingActivityLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProblemId { get; set; }
        public int AttemptNumber { get; set; }
        public string TestType { get; set; } = ""; // "run", "submit", "save"
        public int TimeTakenSeconds { get; set; }
        public int LanguageSwitchCount { get; set; }
        public int EraseCount { get; set; }
        public int SaveCount { get; set; }
        public int RunClickCount { get; set; }
        public int SubmitClickCount { get; set; }
        public int LoginLogoutCount { get; set; }
        public bool IsSessionAbandoned { get; set; }
        public string PassedTestCaseIDs { get; set; } = ""; // comma-separated
        public string FailedTestCaseIDs { get; set; } = ""; // comma-separated
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
} 