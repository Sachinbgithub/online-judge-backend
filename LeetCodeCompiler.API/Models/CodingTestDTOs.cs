using System.ComponentModel.DataAnnotations;

namespace LeetCodeCompiler.API.Models
{
    // Request DTOs
    public class CreateCodingTestRequest
    {
        [Required]
        [StringLength(200)]
        public string TestName { get; set; } = "";

        [Required]
        public int CreatedBy { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [Range(1, 480)] // 1 minute to 8 hours
        public int DurationMinutes { get; set; }

        [Required]
        [Range(1, 100)]
        public int TotalQuestions { get; set; }

        [Required]
        [Range(1, 1000)]
        public int TotalMarks { get; set; }

        [Range(1, 10000)]
        public int TestType { get; set; } = 1; // Custom test type values

        public bool AllowMultipleAttempts { get; set; } = false;

        [Range(1, 10)]
        public int MaxAttempts { get; set; } = 1;

        public bool ShowResultsImmediately { get; set; } = true;

        public bool AllowCodeReview { get; set; } = false;

        [StringLength(100)]
        public string AccessCode { get; set; } = "";

        [StringLength(200)]
        public string Tags { get; set; } = "";

        // New parameters as requested
        public bool IsResultPublishAutomatically { get; set; } = true;

        public bool ApplyBreachRule { get; set; } = true;

        public int BreachRuleLimit { get; set; } = 0;

        [StringLength(50)]
        public string HostIP { get; set; } = "";

        public int ClassId { get; set; } = 0;

        [Required]
        public List<TopicDataRequest> TopicData { get; set; } = new List<TopicDataRequest>();

        [Required]
        public List<CodingTestQuestionRequest> Questions { get; set; } = new List<CodingTestQuestionRequest>();
    }

    public class TopicDataRequest
    {
        [Required]
        public int SectionId { get; set; }

        [Required]
        public int DomainId { get; set; }

        [Required]
        public int SubdomainId { get; set; }
    }

    public class CodingTestQuestionRequest
    {
        [Required]
        public int ProblemId { get; set; }

        [Required]
        [Range(1, 100)]
        public int QuestionOrder { get; set; }

        [Required]
        [Range(1, 100)]
        public int Marks { get; set; }

        [Required]
        [Range(1, 120)]
        public int TimeLimitMinutes { get; set; }

        [StringLength(1000)]
        public string CustomInstructions { get; set; } = "";
    }

    public class UpdateCodingTestRequest
    {
        [Required]
        public int Id { get; set; }

        [StringLength(200)]
        public string? TestName { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(1, 480)]
        public int? DurationMinutes { get; set; }

        [Range(1, 1000)]
        public int? TotalMarks { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsPublished { get; set; }

        public int? TestType { get; set; }

        [StringLength(50)]
        public string? Difficulty { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [StringLength(500)]
        public string? Instructions { get; set; }

        public bool? AllowMultipleAttempts { get; set; }

        [Range(1, 10)]
        public int? MaxAttempts { get; set; }

        public bool? ShowResultsImmediately { get; set; }

        public bool? AllowCodeReview { get; set; }

        [StringLength(100)]
        public string? AccessCode { get; set; }

        [StringLength(200)]
        public string? Tags { get; set; }
    }

    public class StartCodingTestRequest
    {
        [Required]
        public int CodingTestId { get; set; }

        [Required]
        public int UserId { get; set; }

        [StringLength(100)]
        public string? AccessCode { get; set; }
    }

    public class SubmitCodingTestRequest
    {
        [Required]
        public int CodingTestAttemptId { get; set; }

        [Required]
        public int UserId { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class SubmitQuestionRequest
    {
        [Required]
        public int CodingTestQuestionAttemptId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string LanguageUsed { get; set; } = "";

        [Required]
        public string CodeSubmitted { get; set; } = "";

        public int RunCount { get; set; } = 0;

        public int SubmitCount { get; set; } = 0;
    }

    // Response DTOs
    public class CodingTestResponse
    {
        public int Id { get; set; }
        public string TestName { get; set; } = "";
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalMarks { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublished { get; set; }
        public int TestType { get; set; } = 1;
        public bool AllowMultipleAttempts { get; set; }
        public int MaxAttempts { get; set; }
        public bool ShowResultsImmediately { get; set; }
        public bool AllowCodeReview { get; set; }
        public string AccessCode { get; set; } = "";
        public string Tags { get; set; } = "";
        public bool IsResultPublishAutomatically { get; set; }
        public bool ApplyBreachRule { get; set; }
        public int BreachRuleLimit { get; set; }
        public string HostIP { get; set; } = "";
        public int ClassId { get; set; }
        public List<TopicDataResponse> TopicData { get; set; } = new List<TopicDataResponse>();
        public List<CodingTestQuestionResponse> Questions { get; set; } = new List<CodingTestQuestionResponse>();
        public int TotalAttempts { get; set; }
        public int CompletedAttempts { get; set; }
    }

    public class TopicDataResponse
    {
        public int Id { get; set; }
        public int CodingTestId { get; set; }
        public int SectionId { get; set; }
        public int DomainId { get; set; }
        public int SubdomainId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CodingTestQuestionResponse
    {
        public int Id { get; set; }
        public int CodingTestId { get; set; }
        public int ProblemId { get; set; }
        public int QuestionOrder { get; set; }
        public int Marks { get; set; }
        public int TimeLimitMinutes { get; set; }
        public string CustomInstructions { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public ProblemResponse? Problem { get; set; }
    }

    public class ProblemResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Examples { get; set; } = "";
        public string Constraints { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public List<TestCaseResponse> TestCases { get; set; } = new List<TestCaseResponse>();
    }

    public class TestCaseResponse
    {
        public int Id { get; set; }
        public int ProblemId { get; set; }
        public string Input { get; set; } = "";
        public string ExpectedOutput { get; set; } = "";
    }

    public class CodingTestAttemptResponse
    {
        public int Id { get; set; }
        public int CodingTestId { get; set; }
        public int UserId { get; set; }
        public int AttemptNumber { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string Status { get; set; } = "";
        public int TotalScore { get; set; }
        public int MaxScore { get; set; }
        public double Percentage { get; set; }
        public int TimeSpentMinutes { get; set; }
        public bool IsLateSubmission { get; set; }
        public string Notes { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<CodingTestQuestionAttemptResponse> QuestionAttempts { get; set; } = new List<CodingTestQuestionAttemptResponse>();
    }

    public class CodingTestQuestionAttemptResponse
    {
        public int Id { get; set; }
        public int CodingTestAttemptId { get; set; }
        public int CodingTestQuestionId { get; set; }
        public int ProblemId { get; set; }
        public int UserId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "";
        public string LanguageUsed { get; set; } = "";
        public string CodeSubmitted { get; set; } = "";
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public int TestCasesPassed { get; set; }
        public int TotalTestCases { get; set; }
        public double ExecutionTime { get; set; }
        public int RunCount { get; set; }
        public int SubmitCount { get; set; }
        public bool IsCorrect { get; set; }
        public string ErrorMessage { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ProblemResponse? Problem { get; set; }
    }

    public class CodingTestSummaryResponse
    {
        public int Id { get; set; }
        public string TestName { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalMarks { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublished { get; set; }
        public int TestType { get; set; } = 1;
        public string Status { get; set; } = ""; // Upcoming, Active, Completed, Expired
        public int TotalAttempts { get; set; }
        public int CompletedAttempts { get; set; }
        public double AverageScore { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // New fields for domain/subdomain information
        public string? SubjectName { get; set; } // Domain name
        public string? TopicName { get; set; } // Subdomain name
        public bool IsEnabled { get; set; } = true;
    }

    // =============================================
    // AssignedCodingTest DTOs
    // =============================================

    public class AssignCodingTestRequest
    {
        [Required]
        public int CodingTestId { get; set; }

        [Required]
        public long AssignedToUserId { get; set; }

        [Required]
        [Range(1, 255)]
        public byte AssignedToUserType { get; set; } // User type (25, 1, 3, etc.)

        [Required]
        public long AssignedByUserId { get; set; }

        public int TestType { get; set; } = 1002; // Your custom test type

        public byte TestMode { get; set; } = 5; // Your custom test mode
    }

    public class AssignCodingTestResponse
    {
        public long AssignedId { get; set; }
        public int CodingTestId { get; set; }
        public long AssignedToUserId { get; set; }
        public byte AssignedToUserType { get; set; }
        public long AssignedByUserId { get; set; }
        public DateTime AssignedDate { get; set; }
        public int TestType { get; set; }
        public byte TestMode { get; set; }
        public string TestName { get; set; } = "";
        public string AssignedByName { get; set; } = "";
    }

    public class AssignedCodingTestSummaryResponse
    {
        public long AssignedId { get; set; }
        public int CodingTestId { get; set; }
        public string TestName { get; set; } = "";
        public DateTime AssignedDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalMarks { get; set; }
        public int TestType { get; set; }
        public byte TestMode { get; set; }
        public string Status { get; set; } = ""; // assigned, submitted, expired
        public string AssignedByName { get; set; } = "";
        
        // Subject and Topic information
        public string? SubjectName { get; set; } // Domain name
        public string? TopicName { get; set; } // Subdomain name
    }

    public class GetAssignedTestsByUserRequest
    {
        [Required]
        public long UserId { get; set; }

        [Required]
        [Range(1, 255)]
        public byte UserType { get; set; }

        public int? TestType { get; set; } // Optional: 1002, etc.
        public long? ClassId { get; set; } // Optional: Class filter
    }

    public class UnassignCodingTestRequest
    {
        [Required]
        public long AssignedId { get; set; }

        [Required]
        public long UnassignedByUserId { get; set; }
    }
}
