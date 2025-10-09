using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public interface IPracticeTestService
    {
        Task<CreatePracticeTestResponse> CreatePracticeTestAsync(CreatePracticeTestRequest request);
        Task<StartPracticeTestResponse> StartPracticeTestAsync(StartPracticeTestRequest request);
        Task<SubmitPracticeTestResultResponse> SubmitPracticeTestResultAsync(SubmitPracticeTestResultRequest request);
        Task<GetPracticeTestResultResponse> GetPracticeTestResultAsync(GetPracticeTestResultRequest request);
        Task<object> ValidatePracticeTestAsync(int practiceTestId, int userId, int attemptNumber);
    }
}
