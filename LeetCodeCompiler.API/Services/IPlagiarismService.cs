using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public record PlagiarismCheckJob(long SubmissionId, int CodingTestId, int? ProblemId, int CodingTestAttemptId);

    public interface IPlagiarismService
    {
        void EnqueueCheck(PlagiarismCheckJob job);
        Task<PlagiarismReportResponse?> GetReportAsync(long submissionId);
        Task ProcessCheckAsync(PlagiarismCheckJob job);
    }
}
