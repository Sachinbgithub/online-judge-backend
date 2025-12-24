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

        public bool IsPublished { get; set; } = false;

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

        // Question updates
        public List<QuestionUpdateRequest>? Questions { get; set; }
    }

    public class QuestionUpdateRequest
    {
        public int? Id { get; set; } // If null, it's a new question
        public int? ProblemId { get; set; }
        public int? QuestionOrder { get; set; }
        public int? Marks { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public string? CustomInstructions { get; set; }
        public bool? IsDeleted { get; set; } // If true, remove this question
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







public class CombinedTestResultResponse
{
    // Properties from SubmitWholeCodingTestResponse
    public long SubmissionId { get; set; }
    public int CodingTestId { get; set; }
    public string TestName { get; set; } = "";
    public int UserId { get; set; }
    public int AttemptNumber { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalScore { get; set; }
    public int MaxScore { get; set; }
    public double Percentage { get; set; }
    public bool IsLateSubmission { get; set; }
    public DateTime SubmissionTime { get; set; }
    public List<QuestionSubmissionResponse> QuestionSubmissions { get; set; } = new List<QuestionSubmissionResponse>();
    public DateTime CreatedAt { get; set; }

    // Properties from TestStatusResponse
    public long AssignedId { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TimeSpentMinutes { get; set; }
    public string Status { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public bool CanStart { get; set; }
    public bool CanEnd { get; set; }
    public bool IsExpired { get; set; }
    public string Message { get; set; } = "";

    // Additional Statistics
    public int TotalProblems { get; set; } // Total number of problems/questions
    public int CorrectProblems { get; set; } // Number of problems where all test cases passed
    public int TotalTestCases { get; set; } // Total test cases across all problems
    public int CorrectTestCases { get; set; } // Total test cases that passed
    public double ProblemAccuracy { get; set; } // Percentage of problems solved correctly
    public double TestCaseAccuracy { get; set; } // Percentage of test cases passed
    public double FinalScore { get; set; } // Score out of totalMarks (combines problemAccuracy + testCaseAccuracy)
}

























    public class SubmitCodingTestRequest
    {
        [Required]
        public int UserId { get; set; }

        public int? CodingTestId { get; set; } // Optional: Specify which test to submit for

        [Required]
        public int ProblemId { get; set; }

        [Required]
        public int AttemptNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string LanguageUsed { get; set; } = "";

        [Required]
        public string FinalCodeSnapshot { get; set; } = "";

        public int TotalTestCases { get; set; } = 0;
        public int PassedTestCases { get; set; } = 0;
        public int FailedTestCases { get; set; } = 0;
        public bool RequestedHelp { get; set; } = false;
        
        // Activity tracking properties
        public int LanguageSwitchCount { get; set; } = 0;
        public int RunClickCount { get; set; } = 0;
        public int SubmitClickCount { get; set; } = 0;
        public int EraseCount { get; set; } = 0;
        public int SaveCount { get; set; } = 0;
        public int LoginLogoutCount { get; set; } = 0;
        public bool IsSessionAbandoned { get; set; } = false;
        public int? ClassId { get; set; }
    }

    public class SubmitCodingTestResponse
    {
        public long SubmissionId { get; set; }
        public int CodingTestId { get; set; }
        public string TestName { get; set; } = "";
        public int ProblemId { get; set; }
        public string ProblemTitle { get; set; } = "";
        public int UserId { get; set; }
        public int AttemptNumber { get; set; }
        public string LanguageUsed { get; set; } = "";
        public int TotalTestCases { get; set; }
        public int PassedTestCases { get; set; }
        public int FailedTestCases { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsLateSubmission { get; set; }
        public DateTime SubmissionTime { get; set; }
        public int ExecutionTimeMs { get; set; }
        public int MemoryUsedKB { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorType { get; set; }
        public List<SubmissionTestCaseResult> TestCaseResults { get; set; } = new List<SubmissionTestCaseResult>();
        public DateTime CreatedAt { get; set; }
    }

    public class SubmissionTestCaseResult
    {
        public long ResultId { get; set; }
        public int TestCaseId { get; set; }
        public int TestCaseOrder { get; set; }
        public string Input { get; set; } = "";
        public string ExpectedOutput { get; set; } = "";
        public string? ActualOutput { get; set; }
        public bool IsPassed { get; set; }
        public int ExecutionTimeMs { get; set; }
        public int MemoryUsedKB { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorType { get; set; }
    }

    public class CodingTestSubmissionSummaryResponse
    {
        public long SubmissionId { get; set; }
        public int CodingTestId { get; set; }
        public string TestName { get; set; } = "";
        public int ProblemId { get; set; }
        public string ProblemTitle { get; set; } = "";
        public int UserId { get; set; }
        public int AttemptNumber { get; set; }
        public string LanguageUsed { get; set; } = "";
        public int TotalTestCases { get; set; }
        public int PassedTestCases { get; set; }
        public int FailedTestCases { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsLateSubmission { get; set; }
        public DateTime SubmissionTime { get; set; }
        public int ExecutionTimeMs { get; set; }
        public int MemoryUsedKB { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorType { get; set; }
        public int LanguageSwitchCount { get; set; }
        public int RunClickCount { get; set; }
        public int SubmitClickCount { get; set; }
        public int EraseCount { get; set; }
        public int SaveCount { get; set; }
        public int LoginLogoutCount { get; set; }
        public bool IsSessionAbandoned { get; set; }
        public int? ClassId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GetCodingTestSubmissionsRequest
    {
        public int? UserId { get; set; }
        public int? CodingTestId { get; set; }
        public int? ProblemId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class CodingTestStatisticsResponse
    {
        public int CodingTestId { get; set; }
        public string TestName { get; set; } = "";
        public int TotalSubmissions { get; set; }
        public int UniqueUsers { get; set; }
        public int UniqueProblems { get; set; }
        public double AverageSuccessRate { get; set; }
        public double AverageScoreRate { get; set; }
        public double AverageExecutionTimeMs { get; set; }
        public double AverageMemoryUsedKB { get; set; }
        public int TotalLanguageSwitches { get; set; }
        public int TotalRunClicks { get; set; }
        public int TotalSubmitClicks { get; set; }
        public int TotalEraseCount { get; set; }
        public int TotalSaveCount { get; set; }
        public int TotalLoginLogoutCount { get; set; }
        public int AbandonedSessions { get; set; }
        public int LateSubmissions { get; set; }
    }

    // =============================================
    // Test Status Management DTOs
    // =============================================

    public class StartTestRequest
    {
        [Required]
        public long UserId { get; set; }

        [Required]
        public int CodingTestId { get; set; }
    }

    public class EndTestRequest
    {
        [Required]
        public long UserId { get; set; }

        [Required]
        public int CodingTestId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Completed"; // Completed, Expired, Abandoned

        public DateTime? StartedAt { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        [Required]
        public int TimeSpentMinutes { get; set; }
        
        [Required]
        public bool IsLateSubmission { get; set; }
    }

    public class TestStatusResponse
    {
        public long AssignedId { get; set; }
        public int CodingTestId { get; set; }
        public string TestName { get; set; } = "";
        public long UserId { get; set; }
        public string Status { get; set; } = ""; // Assigned, InProgress, Completed, Expired
        public DateTime AssignedDate { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TimeSpentMinutes { get; set; }
        public bool IsLateSubmission { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalMarks { get; set; }
        public bool CanStart { get; set; }
        public bool CanEnd { get; set; }
        public bool IsExpired { get; set; }
        public string Message { get; set; } = "";
    }

    public class UpdateTestStatusRequest
    {
        [Required]
        public long AssignedId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = ""; // Assigned, InProgress, Completed, Expired

        public int TimeSpentMinutes { get; set; } = 0;
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

    /// <summary>
    /// Detailed CodingTest response that mirrors the CodingTests table columns
    /// without including navigation properties.
    /// </summary>
    public class CodingTestFullResponse
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
        public int TestType { get; set; }
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
        public List<StarterCodeResponse> StarterCodes { get; set; } = new List<StarterCodeResponse>();
    }

    public class StarterCodeResponse
    {
        public int Id { get; set; }
        public int ProblemId { get; set; }
        public int Language { get; set; }
        public string Code { get; set; } = "";
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

    /// <summary>
    /// Detailed AssignedCodingTest response that mirrors the AssignedCodingTests table columns.
    /// </summary>
    public class AssignedCodingTestResponse
    {
        public long AssignedId { get; set; }
        public int CodingTestId { get; set; }
        public long AssignedToUserId { get; set; }
        public byte AssignedToUserType { get; set; }
        public long AssignedByUserId { get; set; }
        public DateTime AssignedDate { get; set; }
        public int TestType { get; set; }
        public byte TestMode { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; } = "";
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TimeSpentMinutes { get; set; }
        public bool IsLateSubmission { get; set; }
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

    // =============================================
    // Whole Test Submission DTOs
    // =============================================

    public class SubmitWholeCodingTestRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int CodingTestId { get; set; }

        [Required]
        public int AttemptNumber { get; set; }

        [Required]
        public List<QuestionSubmission> QuestionSubmissions { get; set; } = new List<QuestionSubmission>();

        // Overall test metrics
        public int TotalTimeSpentMinutes { get; set; } = 0;
        public bool IsLateSubmission { get; set; } = false;
        public int? ClassId { get; set; }
    }

    public class QuestionSubmission
    {
        [Required]
        public int ProblemId { get; set; }

        [Required]
        [StringLength(50)]
        public string LanguageUsed { get; set; } = "";

        [Required]
        public string FinalCodeSnapshot { get; set; } = "";

        public int TotalTestCases { get; set; } = 0;
        public int PassedTestCases { get; set; } = 0;
        public int FailedTestCases { get; set; } = 0;
        public int Score { get; set; } = 0;
        public bool RequestedHelp { get; set; } = false;

        // Activity tracking properties
        public int LanguageSwitchCount { get; set; } = 0;
        public int RunClickCount { get; set; } = 0;
        public int SubmitClickCount { get; set; } = 0;
        public int EraseCount { get; set; } = 0;
        public int SaveCount { get; set; } = 0;
        public int LoginLogoutCount { get; set; } = 0;
        public bool IsSessionAbandoned { get; set; } = false;

        // Test case results for this question
        public List<TestCaseSubmissionResult> TestCaseResults { get; set; } = new List<TestCaseSubmissionResult>();
    }

    public class TestCaseSubmissionResult
    {
        [Required]
        public int TestCaseId { get; set; }

        [Required]
        public int TestCaseOrder { get; set; }

        [Required]
        public string Input { get; set; } = "";

        [Required]
        public string ExpectedOutput { get; set; } = "";

        public string? ActualOutput { get; set; }
        public bool IsPassed { get; set; } = false;
        public int ExecutionTimeMs { get; set; } = 0;
        public int MemoryUsedKB { get; set; } = 0;
        public string? ErrorMessage { get; set; }
        public string? ErrorType { get; set; } // CompilationError, RuntimeError, TimeoutError, WrongAnswer
    }

    public class SubmitWholeCodingTestResponse
    {
        public long SubmissionId { get; set; }
        public int CodingTestId { get; set; }
        public string TestName { get; set; } = "";
        public int UserId { get; set; }
        public int AttemptNumber { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalScore { get; set; }
        public int MaxScore { get; set; }
        public double Percentage { get; set; }
        public bool IsLateSubmission { get; set; }
        public DateTime SubmissionTime { get; set; }
        public List<QuestionSubmissionResponse> QuestionSubmissions { get; set; } = new List<QuestionSubmissionResponse>();
        public DateTime CreatedAt { get; set; }
    }

    public class QuestionSubmissionResponse
    {
        public int ProblemId { get; set; }
        public string ProblemTitle { get; set; } = "";
        public string LanguageUsed { get; set; } = "";
        public int TotalTestCases { get; set; }
        public int PassedTestCases { get; set; }
        public int FailedTestCases { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public bool IsCorrect { get; set; }
        public List<TestCaseSubmissionResult> TestCaseResults { get; set; } = new List<TestCaseSubmissionResult>();
    }

    // =============================================
    // Comprehensive Test Results DTOs
    // =============================================

    public class GetTestResultsRequest
    {
        [Required]
        public long UserId { get; set; }

        [Required]
        public int CodingTestId { get; set; }

        public int? AttemptNumber { get; set; } // Optional: Get specific attempt
    }

    public class ComprehensiveTestResultResponse
    {
        public int CodingTestId { get; set; }
        public string TestName { get; set; } = "";
        public long UserId { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalMarks { get; set; }
        public int TotalScore { get; set; } // User's total score across all questions
        public double Percentage { get; set; } // Percentage score (TotalScore / TotalMarks * 100)
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DurationMinutes { get; set; }
        public List<ProblemTestResult> ProblemResults { get; set; } = new List<ProblemTestResult>();
        public TestSummary Summary { get; set; } = new TestSummary();

        // Student profile data (optional - may be null if external API fails)
        public StudentProfileData? StudentProfile { get; set; }
    }

    public class ProblemTestResult
    {
        public int ProblemId { get; set; }
        public string ProblemTitle { get; set; } = "";
        public int QuestionOrder { get; set; }
        public int MaxScore { get; set; }
        public string LanguageUsed { get; set; } = "";
        public string FinalCodeSnapshot { get; set; } = "";
        public string CodeSource { get; set; } = ""; // Source of the code: "submission", "question_attempt", "core_result", or "none"
        public string DebugInfo { get; set; } = ""; // Debug information about what data was found
        public int TotalTestCases { get; set; }
        public int PassedTestCases { get; set; }
        public int FailedTestCases { get; set; }
        public int Score { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsLateSubmission { get; set; }
        public DateTime SubmissionTime { get; set; }
        public int ExecutionTimeMs { get; set; }
        public int MemoryUsedKB { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorType { get; set; }
        
        // Activity tracking metrics
        public int LanguageSwitchCount { get; set; }
        public int RunClickCount { get; set; }
        public int SubmitClickCount { get; set; }
        public int EraseCount { get; set; }
        public int SaveCount { get; set; }
        public int LoginLogoutCount { get; set; }
        public bool IsSessionAbandoned { get; set; }
        
        // Question/Problem details
        public QuestionDetails QuestionDetails { get; set; } = new QuestionDetails();
        
        // Detailed test case results
        public List<DetailedTestCaseResult> TestCaseResults { get; set; } = new List<DetailedTestCaseResult>();
    }

    public class QuestionDetails
    {
        public int ProblemId { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Examples { get; set; } = "";
        public string Constraints { get; set; } = "";
        public string? Hints { get; set; }
        public int? TimeLimit { get; set; }
        public int? MemoryLimit { get; set; }
        public int? SubdomainId { get; set; }
        public int? Difficulty { get; set; }
        public List<TestCaseDetails> TestCases { get; set; } = new List<TestCaseDetails>();
        public List<StarterCodeDetails> StarterCodes { get; set; } = new List<StarterCodeDetails>();
    }

    public class TestCaseDetails
    {
        public int Id { get; set; }
        public int ProblemId { get; set; }
        public string Input { get; set; } = "";
        public string ExpectedOutput { get; set; } = "";
    }

    public class StarterCodeDetails
    {
        public int Id { get; set; }
        public int ProblemId { get; set; }
        public int Language { get; set; }
        public string Code { get; set; } = "";
    }

    public class DetailedTestCaseResult
    {
        public long ResultId { get; set; }
        public int TestCaseId { get; set; }
        public int TestCaseOrder { get; set; }
        public string Input { get; set; } = "";
        public string ExpectedOutput { get; set; } = "";
        public string? ActualOutput { get; set; }
        public bool IsPassed { get; set; }
        public int ExecutionTimeMs { get; set; }
        public int MemoryUsedKB { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorType { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TestSummary
    {
        public int TotalScore { get; set; }
        public int MaxPossibleScore { get; set; }
        public double Percentage { get; set; }
        public int TotalTestCases { get; set; }
        public int PassedTestCases { get; set; }
        public int FailedTestCases { get; set; }
        public int CorrectProblems { get; set; }
        public int TotalProblems { get; set; }
        public double AverageExecutionTimeMs { get; set; }
        public double AverageMemoryUsedKB { get; set; }
        public int TotalLanguageSwitches { get; set; }
        public int TotalRunClicks { get; set; }
        public int TotalSubmitClicks { get; set; }
        public int TotalEraseCount { get; set; }
        public int TotalSaveCount { get; set; }
        public int TotalLoginLogoutCount { get; set; }
        public int AbandonedSessions { get; set; }
        public int LateSubmissions { get; set; }
        public DateTime? FirstSubmissionTime { get; set; }
        public DateTime? LastSubmissionTime { get; set; }
    }

    public class StudentProfileData
    {
        public long UserId { get; set; }
        public string UserName { get; set; } = "";
        public int UserTypeId { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string EmailId { get; set; } = "";
        public string? AlternativeEmailId { get; set; }
        public string ContactNo { get; set; } = "";
        public DateTime? BirthDate { get; set; }
        public string? PhotoUrl { get; set; }
        public int Gender { get; set; }
        public int StateId { get; set; }
        public int DistrictId { get; set; }
        public int CityId { get; set; }
        public DateTime LastLoginDate { get; set; }
        public DateTime LastLogoutDate { get; set; }
        public int UsageInMinutes { get; set; }
        public int UsageInSeconds { get; set; }
        public int NoOFVisits { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? LastPasswordChangeDate { get; set; }
        public int? StudentId { get; set; }
        public int? StudentCourseId { get; set; }
        public int? StudentStreamId { get; set; }
        public int? StudentCollegeId { get; set; }
        public int? StudentAge { get; set; }
        public string StudentAddress { get; set; } = "";
        public string RollNo { get; set; } = "";
        public int DivisionId { get; set; }
        public string? VideoPath { get; set; }
        public string? ResumePath { get; set; }
        public bool IsGraduation { get; set; }
        public bool IsPostGraduation { get; set; }
        public bool IsDiploma { get; set; }
        public bool IsClassX { get; set; }
        public bool IsClassXII { get; set; }
        public string AcademicYear { get; set; } = "";
        public int? ClassId { get; set; }
        public int? FacultyId { get; set; }
        public int? FacultyCourseId { get; set; }
        public int? FacultyStreamId { get; set; }
        public int? FacultyCollegeId { get; set; }
        public int? DesignationId { get; set; }
        public bool? IsAuthorised { get; set; }
        public string CollegeName { get; set; } = "";
        public string? CollegeAddress { get; set; }
        public string? CollegeEmail { get; set; }
        public string? CollegeWebsite { get; set; }
        public string? CollegeMobileNo { get; set; }
        public string? DefaultGroupName { get; set; }
        public string? MyGroupName { get; set; }
        public int? DefaultGroupId { get; set; }
        public int? MyGroupId { get; set; }
        public string FullName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public bool IsStudent { get; set; }
        public bool IsFaculty { get; set; }
        public string UserType { get; set; } = "";
        public int ProfileCompletionPercentage { get; set; }
    }
}
