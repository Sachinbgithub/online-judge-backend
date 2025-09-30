using LeetCodeCompiler.API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeetCodeCompiler.API.Services
{
    public interface IActivityTrackingService
    {
        Task<UserCodingActivityLog> LogUserActivityAsync(int userId, int problemId, int attemptNumber, string testType, int timeTakenSeconds);
        Task<CoreQuestionResult> CreateOrUpdateQuestionResultAsync(int userId, int problemId, int attemptNumber, string languageUsed, string finalCodeSnapshot, int totalTestCases, int passedTestCases, int failedTestCases);
        Task<CoreTestCaseResult> LogTestCaseResultAsync(int coreQuestionResultId, int userId, int problemId, int testCaseId, bool isPassed, string userOutput, string expectedOutput, double executionTime);
        Task<List<UserCodingActivityLog>> GetUserActivityLogsAsync(int userId, int? problemId = null);
        Task<List<CoreQuestionResult>> GetUserQuestionResultsAsync(int userId, int? problemId = null);
        Task<List<CoreTestCaseResult>> GetTestCaseResultsAsync(int coreQuestionResultId);
        Task<List<CoreQuestionResult>> GetAllQuestionResultsAsync();
        Task<List<CoreTestCaseResult>> GetAllTestCaseResultsAsync();
        Task<List<CoreTestCaseResult>> GetUserTestCaseResultsAsync(int userId);
        Task<List<CoreTestCaseResult>> GetUserTestCaseResultsForProblemAsync(int userId, int problemId);

        // First Overload
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
            DateTime? endTime = null
        );

        // Second Overload — this fixes the Controller error
        Task UpdateActivityMetricsAsync(
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
            string failedTestCaseIDs
        );
    }
}