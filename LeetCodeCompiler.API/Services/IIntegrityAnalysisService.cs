using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public interface IIntegrityAnalysisService
    {
        Task SaveActivitySnapshotAsync(CodeActivitySnapshotRequest request);
        Task<List<IntegrityFlagResponse>> RunSubmitHeuristicsAsync(
            int codingTestAttemptId,
            long? submissionId,
            int problemId,
            string finalCode,
            bool allTestsPassed,
            DateTime attemptStartedAt);
        Task<List<IntegrityFlagResponse>> GetFlagsForAttemptAsync(int attemptId, int? requestingUserId, bool isTestSetter);
        Task<List<IntegrityReviewSummaryResponse>> GetReviewSummaryAsync(int codingTestId);
        Task<IntegrityFlagResponse?> ReviewFlagAsync(long flagId, ReviewIntegrityFlagRequest request, int reviewedBy);
        Task<List<CodeActivitySnapshotResponse>> GetActivitySnapshotsForAttemptAsync(int codingTestAttemptId);
        Task<GrantResumeResponse> GrantResumeAsync(int codingTestAttemptId, int grantedByUserId);
    }
}
