using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Services
{
    public class NetworkDisconnectService : INetworkDisconnectService
    {
        private readonly AppDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;

        public NetworkDisconnectService(AppDbContext context, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _scopeFactory = scopeFactory;
        }

        public async Task HandleDisconnectTimeoutAsync(int codingTestAttemptId, int userId)
        {
            var attempt = await _context.CodingTestAttempts.FindAsync(codingTestAttemptId);
            if (attempt == null)
                throw new ArgumentException($"Attempt {codingTestAttemptId} not found");

            if (attempt.UserId != userId)
                throw new UnauthorizedAccessException("You do not own this attempt.");

            if (attempt.Status is "Submitted" or "Completed")
                return;

            await EnsureNetworkLossFlagAsync(codingTestAttemptId);
            await _context.SaveChangesAsync();

            using var scope = _scopeFactory.CreateScope();
            var codingTestService = scope.ServiceProvider.GetRequiredService<ICodingTestService>();
            await codingTestService.AutoSubmitAttemptAsync(codingTestAttemptId, AutoSubmitReason.NetworkLoss);
        }

        private async Task EnsureNetworkLossFlagAsync(int attemptId)
        {
            var exists = await _context.IntegrityFlags
                .AnyAsync(f => f.CodingTestAttemptId == attemptId
                            && f.FlagType == IntegrityAnalysisService.FlagTypeNetworkLoss
                            && f.ReviewStatus == "Pending");

            if (exists)
                return;

            _context.IntegrityFlags.Add(new IntegrityFlag
            {
                CodingTestAttemptId = attemptId,
                FlagType = IntegrityAnalysisService.FlagTypeNetworkLoss,
                Severity = "Medium",
                DetailsJson = "{\"reason\":\"Network disconnect timeout\"}",
                CreatedAt = DateTime.UtcNow,
                ReviewStatus = "Pending"
            });
        }
    }
}
