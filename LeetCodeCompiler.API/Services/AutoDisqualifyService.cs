using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Services
{
    public class AutoDisqualifyService : IAutoDisqualifyService
    {
        private readonly AppDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;

        public AutoDisqualifyService(AppDbContext context, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _scopeFactory = scopeFactory;
        }

        public async Task DisqualifyAndAutoSubmitAsync(int codingTestAttemptId, int breachCount)
        {
            var attempt = await _context.CodingTestAttempts.FindAsync(codingTestAttemptId);
            if (attempt == null || attempt.Status is "Submitted" or "Completed")
                return;

            attempt.IntegrityStatus = "Disqualified";
            attempt.UpdatedAt = DateTime.UtcNow;

            await EnsureAutoDqFlagAsync(codingTestAttemptId, breachCount);
            await _context.SaveChangesAsync();

            using var scope = _scopeFactory.CreateScope();
            var codingTestService = scope.ServiceProvider.GetRequiredService<ICodingTestService>();
            await codingTestService.AutoSubmitDisqualifiedAttemptAsync(codingTestAttemptId);
        }

        private async Task EnsureAutoDqFlagAsync(int attemptId, int breachCount)
        {
            var exists = await _context.IntegrityFlags
                .AnyAsync(f => f.CodingTestAttemptId == attemptId
                            && f.FlagType == IntegrityAnalysisService.FlagTypeAutoDq
                            && f.ReviewStatus == "Pending");

            if (exists)
                return;

            _context.IntegrityFlags.Add(new IntegrityFlag
            {
                CodingTestAttemptId = attemptId,
                FlagType = IntegrityAnalysisService.FlagTypeAutoDq,
                Severity = "High",
                DetailsJson = $"{{\"breachCount\":{breachCount},\"reason\":\"Breach rule limit reached\"}}",
                CreatedAt = DateTime.UtcNow,
                ReviewStatus = "Pending"
            });
        }
    }
}
