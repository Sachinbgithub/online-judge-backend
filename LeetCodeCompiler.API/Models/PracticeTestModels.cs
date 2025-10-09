using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeetCodeCompiler.API.Models
{
    // =============================================
    // Practice Test Models based on SQL Script
    // =============================================

    [Table("PracticeTests")]
    public class PracticeTest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string TestName { get; set; } = "";

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int DomainId { get; set; }

        [Required]
        public int SubdomainId { get; set; }

        [Required]
        public int TotalMarks { get; set; }

        [Required]
        public int DurationMinutes { get; set; }

        [Required]
        public int CreatedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public bool IsPublished { get; set; } = false;

        [Required]
        public bool AllowMultipleAttempts { get; set; } = true;

        [Required]
        public int MaxAttempts { get; set; } = 3;

        [Required]
        public bool ShowResultsImmediately { get; set; } = true;

        [Required]
        [StringLength(20)]
        public string DifficultyLevel { get; set; } = "Medium";

        [StringLength(200)]
        public string? Tags { get; set; }

        [StringLength(2000)]
        public string? Instructions { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal PassingPercentage { get; set; } = 60.00m;

        // Navigation properties
        [ForeignKey("DomainId")]
        public virtual Domain Domain { get; set; } = null!;

        [ForeignKey("SubdomainId")]
        public virtual Subdomain Subdomain { get; set; } = null!;

        public virtual ICollection<PracticeTestQuestion> Questions { get; set; } = new List<PracticeTestQuestion>();
        public virtual ICollection<PracticeTestResult> Results { get; set; } = new List<PracticeTestResult>();
    }

    [Table("PracticeTestQuestions")]
    public class PracticeTestQuestion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PracticeTestId { get; set; }

        [Required]
        public int ProblemId { get; set; }

        [Required]
        public int QuestionOrder { get; set; }

        [Required]
        public int Marks { get; set; }

        public int? TimeLimitMinutes { get; set; }

        [StringLength(1000)]
        public string? CustomInstructions { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PracticeTestId")]
        public virtual PracticeTest PracticeTest { get; set; } = null!;

        [ForeignKey("ProblemId")]
        public virtual Problem Problem { get; set; } = null!;

        public virtual ICollection<PracticeTestQuestionResult> QuestionResults { get; set; } = new List<PracticeTestQuestionResult>();
    }

    [Table("PracticeTestResults")]
    public class PracticeTestResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PracticeTestId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int AttemptNumber { get; set; } = 1;

        [Required]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [Required]
        public int TotalMarks { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ObtainedMarks { get; set; } = 0.00m;

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Percentage { get; set; } = 0.00m;

        [Required]
        public bool IsPassed { get; set; } = false;

        public int? TimeTakenMinutes { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "InProgress"; // InProgress, Completed, Abandoned, Timeout

        public string? SubmissionData { get; set; } // JSON data for detailed results

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("PracticeTestId")]
        public virtual PracticeTest PracticeTest { get; set; } = null!;

        public virtual ICollection<PracticeTestQuestionResult> QuestionResults { get; set; } = new List<PracticeTestQuestionResult>();
    }

    [Table("PracticeTestQuestionResults")]
    public class PracticeTestQuestionResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PracticeTestResultId { get; set; }

        [Required]
        public int PracticeTestQuestionId { get; set; }

        [Required]
        public int ProblemId { get; set; }

        [Required]
        public int QuestionOrder { get; set; }

        public string? SubmittedCode { get; set; }

        [Required]
        [StringLength(50)]
        public string Language { get; set; } = "";

        [Required]
        public int Marks { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ObtainedMarks { get; set; } = 0.00m;

        [Required]
        public bool IsCorrect { get; set; } = false;

        public int? ExecutionTime { get; set; } // in milliseconds

        public int? MemoryUsed { get; set; } // in KB

        [Required]
        public int TestCasesPassed { get; set; } = 0;

        [Required]
        public int TotalTestCases { get; set; } = 0;

        public string? ErrorMessage { get; set; }

        [Required]
        [StringLength(20)]
        public string CompilationStatus { get; set; } = "Pending"; // Pending, Success, Failed

        [Required]
        [StringLength(20)]
        public string ExecutionStatus { get; set; } = "Pending"; // Pending, Success, Failed, Timeout

        [Required]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public int? TimeTakenMinutes { get; set; }

        // Navigation properties
        [ForeignKey("PracticeTestResultId")]
        public virtual PracticeTestResult PracticeTestResult { get; set; } = null!;

        [ForeignKey("PracticeTestQuestionId")]
        public virtual PracticeTestQuestion PracticeTestQuestion { get; set; } = null!;

        [ForeignKey("ProblemId")]
        public virtual Problem Problem { get; set; } = null!;
    }

    // =============================================
    // Request/Response DTOs
    // =============================================

    public class CreatePracticeTestRequest
    {
        public int DomainId { get; set; }

        public int SubdomainId { get; set; }

        public int TotalMarks { get; set; }

        public int DurationMinutes { get; set; }

        public int CreatedBy { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsPublished { get; set; } = true;

        public bool AllowMultipleAttempts { get; set; } = true;

        public int MaxAttempts { get; set; } = 10;

        public bool ShowResultsImmediately { get; set; } = true;

        public string DifficultyLevel { get; set; } = "string";

        public string? Tags { get; set; }

        public string? Instructions { get; set; }

        public decimal PassingPercentage { get; set; } = 100;

        public List<PracticeTestQuestionRequest> Questions { get; set; } = new List<PracticeTestQuestionRequest>();
    }

    public class PracticeTestQuestionRequest
    {
        public int ProblemId { get; set; }

        public int QuestionOrder { get; set; }

        public int Marks { get; set; }

        public int TimeLimitMinutes { get; set; }

        public string? CustomInstructions { get; set; }
    }

    public class CreatePracticeTestResponse
    {
        public int PracticeTestId { get; set; }
        public string Message { get; set; } = "";
        public bool Success { get; set; }
    }

    public class StartPracticeTestRequest
    {
        [Required]
        public int PracticeTestId { get; set; }

        [Required]
        public int UserId { get; set; }
    }

    public class StartPracticeTestResponse
    {
        public int PracticeTestId { get; set; }
        public string TestName { get; set; } = "";
        public int UserId { get; set; }
        public int AttemptNumber { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationMinutes { get; set; }
        public List<QuestionInfo> Questions { get; set; } = new List<QuestionInfo>();
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class QuestionInfo
    {
        public int QuestionOrder { get; set; }
        public int ProblemId { get; set; }
        public string ProblemTitle { get; set; } = "";
        public string ProblemDescription { get; set; } = "";
        public string Examples { get; set; } = "";
        public string Constraints { get; set; } = "";
        public int Marks { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public string? CustomInstructions { get; set; }
        public List<TestCaseInfo> TestCases { get; set; } = new List<TestCaseInfo>();
        public List<StarterCodeInfo> StarterCodes { get; set; } = new List<StarterCodeInfo>();
    }

    public class TestCaseInfo
    {
        public int Id { get; set; }
        public string Input { get; set; } = "";
        public string ExpectedOutput { get; set; } = "";
    }

    public class StarterCodeInfo
    {
        public int Id { get; set; }
        public string Language { get; set; } = "";
        public string Code { get; set; } = "";
    }

    public class SubmitPracticeTestResultRequest
    {
        [Required]
        public int PracticeTestId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int AttemptNumber { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        public List<QuestionResultSubmission> QuestionResults { get; set; } = new List<QuestionResultSubmission>();
    }

    public class QuestionResultSubmission
    {
        [Required]
        public int ProblemId { get; set; }

        [Required]
        public int QuestionOrder { get; set; }

        [Required]
        public string SubmittedCode { get; set; } = "";

        [Required]
        public string Language { get; set; } = "";

        [Required]
        public int Marks { get; set; }

        [Required]
        public decimal ObtainedMarks { get; set; }

        [Required]
        public bool IsCorrect { get; set; }

        public int? ExecutionTime { get; set; }

        public int? MemoryUsed { get; set; }

        [Required]
        public int TestCasesPassed { get; set; }

        [Required]
        public int TotalTestCases { get; set; }

        public string? ErrorMessage { get; set; }

        [Required]
        public string CompilationStatus { get; set; } = "Success";

        [Required]
        public string ExecutionStatus { get; set; } = "Success";

        public int? TimeTakenMinutes { get; set; }
    }

    public class SubmitPracticeTestResultResponse
    {
        public int PracticeTestId { get; set; }
        public int UserId { get; set; }
        public int AttemptNumber { get; set; }
        public int TotalMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public decimal Percentage { get; set; }
        public bool IsPassed { get; set; }
        public int TimeTakenMinutes { get; set; }
        public string Status { get; set; } = "";
        public DateTime SubmittedAt { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class GetPracticeTestResultRequest
    {
        [Required]
        public int PracticeTestId { get; set; }

        [Required]
        public int UserId { get; set; }

        public int? AttemptNumber { get; set; } // If null, get latest attempt
    }

    public class GetPracticeTestResultResponse
    {
        public int PracticeTestId { get; set; }
        public string TestName { get; set; } = "";
        public int UserId { get; set; }
        public int AttemptNumber { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "";
        public int TotalMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public decimal Percentage { get; set; }
        public bool IsPassed { get; set; }
        public int? TimeTakenMinutes { get; set; }
        public List<QuestionResultDetail> QuestionResults { get; set; } = new List<QuestionResultDetail>();
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class QuestionResultDetail
    {
        public int QuestionOrder { get; set; }
        public int ProblemId { get; set; }
        public string ProblemTitle { get; set; } = "";
        public string ProblemDescription { get; set; } = "";
        public string Language { get; set; } = "";
        public string? SubmittedCode { get; set; }
        public int Marks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public bool IsCorrect { get; set; }
        public int? ExecutionTime { get; set; }
        public int? MemoryUsed { get; set; }
        public int TestCasesPassed { get; set; }
        public int TotalTestCases { get; set; }
        public string? ErrorMessage { get; set; }
        public string CompilationStatus { get; set; } = "";
        public string ExecutionStatus { get; set; } = "";
        public DateTime SubmittedAt { get; set; }
        public int? TimeTakenMinutes { get; set; }
    }
}
