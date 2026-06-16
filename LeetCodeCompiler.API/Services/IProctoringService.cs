using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public interface IProctoringService
    {
        Task<ProctoringSession> StartSessionAsync(int codingTestAttemptId);
        Task<ProctoringStatusResponse> IngestEventsAsync(IngestProctoringEventsRequest request);
        Task<ProctoringStatusResponse> GetStatusAsync(int attemptId, int userId);
        Task<ProctoringStatusResponse> GetStatusForAttemptAsync(int attemptId);
        Task<List<ProctoringEventDto>> GetEventsForAttemptAsync(int attemptId);
    }
}
