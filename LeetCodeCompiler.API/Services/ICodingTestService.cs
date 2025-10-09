using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public interface ICodingTestService
    {
        // Test Management
        Task<CodingTestResponse> CreateCodingTestAsync(CreateCodingTestRequest request);
        Task<CodingTestResponse> GetCodingTestByIdAsync(int id);
        Task<List<CodingTestSummaryResponse>> GetAllCodingTestsAsync();
        Task<List<CodingTestSummaryResponse>> GetCodingTestsByUserAsync(int userId, string? subjectName = null, string? topicName = null, bool isEnabled = true);
        Task<CodingTestResponse> UpdateCodingTestAsync(UpdateCodingTestRequest request);
        Task<bool> DeleteCodingTestAsync(int id);
        Task<bool> PublishCodingTestAsync(int id);
        Task<bool> UnpublishCodingTestAsync(int id);

        // Test Attempts
        Task<CodingTestAttemptResponse> StartCodingTestAsync(StartCodingTestRequest request);
        Task<CodingTestAttemptResponse> GetCodingTestAttemptAsync(int attemptId);
        Task<List<CodingTestAttemptResponse>> GetUserCodingTestAttemptsAsync(int userId, int codingTestId);
        Task<SubmitCodingTestResponse> SubmitCodingTestAsync(SubmitCodingTestRequest request);
        Task<SubmitWholeCodingTestResponse> SubmitWholeCodingTestAsync(SubmitWholeCodingTestRequest request);
        Task<bool> AbandonCodingTestAsync(int attemptId, int userId);

        // Question Attempts
        Task<CodingTestQuestionAttemptResponse> StartQuestionAttemptAsync(int codingTestAttemptId, int questionId, int userId);
        Task<CodingTestQuestionAttemptResponse> GetQuestionAttemptAsync(int questionAttemptId);
        Task<List<CodingTestQuestionAttemptResponse>> GetQuestionAttemptsForTestAsync(int codingTestAttemptId);
        Task<CodingTestQuestionAttemptResponse> SubmitQuestionAsync(SubmitQuestionRequest request);

        // Analytics and Reports
        Task<List<CodingTestSummaryResponse>> GetCodingTestsByStatusAsync(string status);
        Task<object> GetCodingTestAnalyticsAsync(int codingTestId);
        Task<List<CodingTestAttemptResponse>> GetCodingTestResultsAsync(int codingTestId);

        // Assignment methods
        Task<AssignCodingTestResponse> AssignCodingTestAsync(AssignCodingTestRequest request);
        Task<List<AssignedCodingTestSummaryResponse>> GetAssignedTestsByUserAsync(long userId, byte userType, int? testType = null, long? classId = null);
        Task<bool> UnassignCodingTestAsync(long assignedId, long unassignedByUserId);
        Task<List<AssignedCodingTestSummaryResponse>> GetAssignedTestsByTestAsync(int codingTestId);

        // Validation
        Task<bool> ValidateAccessCodeAsync(int codingTestId, string accessCode);
        Task<bool> CanUserAttemptTestAsync(int userId, int codingTestId);
        Task<bool> IsTestActiveAsync(int codingTestId);
        Task<bool> IsTestExpiredAsync(int codingTestId);

        // Submission methods
        Task<List<CodingTestSubmissionSummaryResponse>> GetCodingTestSubmissionsAsync(GetCodingTestSubmissionsRequest request);
        Task<SubmitCodingTestResponse> GetCodingTestSubmissionByIdAsync(long submissionId);
        Task<CodingTestStatisticsResponse> GetCodingTestStatisticsAsync(int codingTestId);
        Task<List<SubmissionTestCaseResult>> GetSubmissionTestCaseResultsAsync(long submissionId);

        // Test Status Management
        Task<TestStatusResponse> EndTestAsync(EndTestRequest request);

        // Comprehensive Test Results
        Task<ComprehensiveTestResultResponse> GetComprehensiveTestResultsAsync(GetTestResultsRequest request);
        Task<object> GetDebugDataAsync(long userId, int codingTestId, int? problemId = null);
    }
}
