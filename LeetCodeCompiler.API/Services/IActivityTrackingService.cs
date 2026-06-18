using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public interface IActivityTrackingService
    {
        Task<UserCodingActivityLog> LogUserActivityAsync(int userId, int problemId, int attemptNumber, string testType, int timeTakenSeconds);
        Task<UserCodingActivityLog> LogUserActivityAsync(LogUserActivityRequest request);
        Task<CoreQuestionResult> CreateOrUpdateQuestionResultAsync(int userId, int problemId, int attemptNumber, string languageUsed, string finalCodeSnapshot, int totalTestCases, int passedTestCases, int failedTestCases);
        Task<CoreTestCaseResult> LogTestCaseResultAsync(int coreQuestionResultId, int userId, int problemId, int testCaseId, bool isPassed, string userOutput, string expectedOutput, double executionTime);
        Task<List<UserCodingActivityLog>> GetUserActivityLogsAsync(int userId, int? problemId = null);
        Task<PagedResult<UserCodingActivityLog>> GetUserActivityLogsAsync(int userId, int? problemId, int pageNumber, int pageSize);
        Task<object> GetUserActivitySummaryAsync(int userId);
        Task<object> GetProblemTimeAnalysisAsync(int userId, int problemId);

        Task<UserCodingActivityLog> GetOrCreateAssessmentSessionAsync(int userId, int problemId, int codingTestAttemptId, string testType, int? codingTestId = null, int? codingTestQuestionAttemptId = null);
        Task UpdateAssessmentMetricsAsync(int activityLogId, int userId, AssessmentActivityMetrics metrics);
        Task<UserCodingActivityLog> CompleteAssessmentSessionAsync(int userId, int problemId, int codingTestAttemptId, long? submissionId, AssessmentActivityMetrics metrics, string testType = "submit", int? codingTestQuestionAttemptId = null);
        Task<UserCodingActivityLog?> GetCurrentAssessmentSessionAsync(int userId, int problemId, int codingTestAttemptId);
        Task<List<UserCodingActivityLog>> GetAttemptActivityLogsAsync(int codingTestAttemptId);
        Task<int> GetAttemptActiveTimeSecondsAsync(int codingTestAttemptId);
        Task<int> GetAttemptChainActiveTimeSecondsAsync(int codingTestAttemptId);
        Task EnsureUserOwnsActivityLogAsync(int activityLogId, int userId);
        Task EnsureUserOwnsAttemptAsync(int codingTestAttemptId, int userId);

        Task<List<CoreQuestionResult>> GetUserQuestionResultsAsync(int userId, int? problemId = null);
        Task<List<CoreTestCaseResult>> GetTestCaseResultsAsync(int coreQuestionResultId);
        Task<List<CoreQuestionResult>> GetAllQuestionResultsAsync();
        Task<List<CoreTestCaseResult>> GetAllTestCaseResultsAsync();
        Task<List<CoreTestCaseResult>> GetUserTestCaseResultsAsync(int userId);
        Task<List<CoreTestCaseResult>> GetUserTestCaseResultsForProblemAsync(int userId, int problemId);

        Task UpdateActivityMetricsAsync(
            int activityLogId,
            int? timeTakenSeconds = null,
            int? languageSwitchCount = null,
            int? eraseCount = null,
            int? saveCount = null,
            int? runClickCount = null,
            int? submitClickCount = null,
            int? loginLogoutCount = null,
            bool? isSessionAbandoned = null,
            string? passedTestCaseIDs = null,
            string? failedTestCaseIDs = null,
            DateTime? startTime = null,
            DateTime? endTime = null);

        Task<UserCodingActivityLog> CreateActivityLogWithMetricsAsync(
            int userId,
            int problemId,
            int attemptNumber,
            string testType,
            int timeTakenSeconds,
            int languageSwitchCount,
            int eraseCount,
            int saveCount,
            int runClickCount,
            int submitClickCount,
            int loginLogoutCount,
            bool isSessionAbandoned,
            string passedTestCaseIDs,
            string failedTestCaseIDs);

        static UserActivityLogResponse MapToResponse(UserCodingActivityLog log) => new()
        {
            Id = log.Id,
            UserId = log.UserId,
            ProblemId = log.ProblemId,
            AttemptNumber = log.AttemptNumber,
            TestType = log.TestType,
            TimeTakenSeconds = log.TimeTakenSeconds,
            LanguageSwitchCount = log.LanguageSwitchCount,
            EraseCount = log.EraseCount,
            SaveCount = log.SaveCount,
            RunClickCount = log.RunClickCount,
            SubmitClickCount = log.SubmitClickCount,
            LoginLogoutCount = log.LoginLogoutCount,
            IsSessionAbandoned = log.IsSessionAbandoned,
            PassedTestCaseIDs = log.PassedTestCaseIDs,
            FailedTestCaseIDs = log.FailedTestCaseIDs,
            CreatedAt = log.CreatedAt,
            UpdatedAt = log.UpdatedAt,
            StartTime = log.StartTime,
            EndTime = log.EndTime,
            CodingTestId = log.CodingTestId,
            CodingTestAttemptId = log.CodingTestAttemptId,
            CodingTestQuestionAttemptId = log.CodingTestQuestionAttemptId,
            SubmissionId = log.SubmissionId,
            SessionStatus = log.SessionStatus,
            Source = log.Source
        };
    }
}
