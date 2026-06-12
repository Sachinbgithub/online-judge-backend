using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public interface IQuestionPoolService
    {
        Task<QuestionPoolResponse> CreatePoolAsync(CreateQuestionPoolRequest request);
        Task<QuestionPoolResponse?> GetPoolAsync(int poolId);
        Task<List<QuestionPoolResponse>> GetPoolsAsync(bool activeOnly = true);
        Task<QuestionPoolResponse> AddProblemsToPoolAsync(int poolId, List<int> problemIds);
        Task<bool> DeletePoolAsync(int poolId);
        Task CreateAttemptQuestionSnapshotAsync(CodingTestAttempt attempt);
        Task<List<AttemptQuestionResponse>> GetAttemptQuestionsAsync(int attemptId);
        Task<(bool IsAllowed, CodingTestAttemptQuestion? Snapshot, CodingTestQuestion? FixedQuestion)> ResolveQuestionForAttemptAsync(
            int attemptId, int problemId);
    }
}
