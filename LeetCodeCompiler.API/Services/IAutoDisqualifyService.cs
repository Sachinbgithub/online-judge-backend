namespace LeetCodeCompiler.API.Services
{
    public interface IAutoDisqualifyService
    {
        Task DisqualifyAndAutoSubmitAsync(int codingTestAttemptId, int breachCount);
    }
}
