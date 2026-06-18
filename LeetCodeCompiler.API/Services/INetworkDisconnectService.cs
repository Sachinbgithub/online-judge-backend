namespace LeetCodeCompiler.API.Services
{
    public interface INetworkDisconnectService
    {
        Task HandleDisconnectTimeoutAsync(int codingTestAttemptId, int userId);
    }
}
