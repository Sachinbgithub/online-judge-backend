using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public interface IAttemptActivityReviewService
    {
        Task<AttemptActivityReviewResponse> GetAttemptActivityReviewAsync(int codingTestAttemptId);
    }
}
