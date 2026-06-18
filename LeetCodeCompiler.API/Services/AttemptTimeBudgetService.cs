using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public static class AttemptTimeBudgetService
    {
        public static DateTime ComputeAllowedEndAt(CodingTest test, DateTime grantTimeUtc, int activeSecondsAlreadySpent)
        {
            var remainingDuration = Math.Max(0, test.DurationMinutes * 60 - activeSecondsAlreadySpent);
            var remainingWindow = Math.Max(0, (test.EndDate - grantTimeUtc).TotalSeconds);
            var allowedSeconds = (int)Math.Min(remainingDuration, remainingWindow);
            var allowedEnd = grantTimeUtc.AddSeconds(allowedSeconds);
            return allowedEnd < test.EndDate ? allowedEnd : test.EndDate;
        }

        public static int ComputeRemainingSeconds(DateTime? allowedEndAtUtc)
        {
            if (!allowedEndAtUtc.HasValue)
                return 0;
            return Math.Max(0, (int)(allowedEndAtUtc.Value - DateTime.UtcNow).TotalSeconds);
        }
    }
}
