using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public interface ICodingTestService
    {
        // Test Management
        Task<CodingTestResponse> CreateCodingTestAsync(CreateCodingTestRequest request);
        Task<CodingTestResponse> GetCodingTestByIdAsync(int id);
        Task<List<CodingTestSummaryResponse>> GetAllCodingTestsAsync();
        Task<PagedResult<CodingTestSummaryResponse>> GetAllCodingTestsPagedAsync(int pageNumber, int pageSize);
        Task<List<CodingTestSummaryResponse>> GetCodingTestsByUserAsync(int userId, string? subjectName = null, string? topicName = null, bool isEnabled = true);
        Task<PagedResult<CodingTestSummaryResponse>> GetCodingTestsByUserPagedAsync(int userId, int pageNumber, int pageSize, string? subjectName = null, string? topicName = null, bool isEnabled = true);
        Task<List<CodingTestFullResponse>> GetCodingTestsByCreatorAsync(int createdByUserId);
        Task<PagedResult<CodingTestFullResponse>> GetCodingTestsByCreatorPagedAsync(int createdByUserId, int pageNumber, int pageSize);
        Task<List<CodingTestSummaryResponse>> GetGlobalCodingTestsByCollegeIdAsync(int collegeId);
        Task<PagedResult<CodingTestSummaryResponse>> GetGlobalCodingTestsByCollegeIdPagedAsync(int collegeId, int pageNumber, int pageSize);
        Task<List<CodingTestSummaryResponse>> GetAllGlobalCodingTestsAsync();
        Task<PagedResult<CodingTestSummaryResponse>> GetAllGlobalCodingTestsPagedAsync(int pageNumber, int pageSize);
        Task<List<CodingTestSummaryResponse>> GetCodingTestsByCollegeIdAsync(int collegeId);
        Task<PagedResult<CodingTestSummaryResponse>> GetCodingTestsByCollegeIdPagedAsync(int collegeId, int pageNumber, int pageSize);
        Task<List<CodingTestSummaryResponse>> GetGlobalTestsByCollegeIdAsync(int collegeId);
        Task<PagedResult<CodingTestSummaryResponse>> GetGlobalTestsByCollegeIdPagedAsync(int collegeId, int pageNumber, int pageSize);
        Task<PagedResult<CodingTestSummaryResponse>> GetCodingTestsByFilterPagedAsync(CodingTestFilterRequest request);
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
        Task<PagedResult<CodingTestSummaryResponse>> GetCodingTestsByStatusPagedAsync(string status, int pageNumber, int pageSize);
        Task<object> GetCodingTestAnalyticsAsync(int codingTestId);
        Task<List<CodingTestAttemptResponse>> GetCodingTestResultsAsync(int codingTestId);
        Task<PagedResult<CodingTestAttemptResponse>> GetCodingTestResultsPagedAsync(int codingTestId, int pageNumber, int pageSize);

        // Assignment methods
        Task<AssignCodingTestResponse> AssignCodingTestAsync(AssignCodingTestRequest request);
        Task<List<AssignedCodingTestSummaryResponse>> GetAssignedTestsByUserAsync(long userId, byte userType, int? testType = null, long? classId = null);
        Task<PagedResult<AssignedCodingTestSummaryResponse>> GetAssignedTestsByUserPagedAsync(long userId, byte userType, int pageNumber, int pageSize, int? testType = null, long? classId = null);
        Task<bool> UnassignCodingTestAsync(long assignedId, long unassignedByUserId);
        Task<List<AssignedCodingTestSummaryResponse>> GetAssignedTestsByTestAsync(int codingTestId);
        Task<PagedResult<AssignedCodingTestSummaryResponse>> GetAssignedTestsByTestPagedAsync(int codingTestId, int pageNumber, int pageSize);
        Task<List<AssignedCodingTestResponse>> GetAssignmentsByTestIdAsync(int codingTestId);
        Task<PagedResult<AssignedCodingTestResponse>> GetAssignmentsByTestIdPagedAsync(int codingTestId, int pageNumber, int pageSize);

        // Validation
        Task<bool> ValidateAccessCodeAsync(int codingTestId, string accessCode);
        Task<bool> CanUserAttemptTestAsync(int userId, int codingTestId);
        Task<bool> IsTestActiveAsync(int codingTestId);
        Task<bool> IsTestExpiredAsync(int codingTestId);

        // Submission methods
        Task<List<CodingTestSubmissionSummaryResponse>> GetCodingTestSubmissionsAsync(GetCodingTestSubmissionsRequest request);
        Task<PagedResult<CodingTestSubmissionSummaryResponse>> GetCodingTestSubmissionsPagedAsync(GetCodingTestSubmissionsRequest request);
        Task<SubmitCodingTestResponse> GetCodingTestSubmissionByIdAsync(long submissionId);
        Task<CodingTestStatisticsResponse> GetCodingTestStatisticsAsync(int codingTestId);
        Task<List<SubmissionTestCaseResult>> GetSubmissionTestCaseResultsAsync(long submissionId);

        // Test Status Management
        Task<TestStatusResponse> EndTestAsync(EndTestRequest request);

        // Comprehensive Test Results
        Task<ComprehensiveTestResultResponse> GetComprehensiveTestResultsAsync(GetTestResultsRequest request);
        Task<CombinedTestResultResponse> GetCombinedTestResultsAsync(long userId, int codingTestId);
        Task<object> GetDebugDataAsync(long userId, int codingTestId, int? problemId = null);
    }
}
