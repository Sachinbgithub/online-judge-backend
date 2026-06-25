using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public static class AttemptStaleHelper
    {
        public static bool IsStale(CodingTest test, CodingTestAttempt attempt, DateTime nowUtc)
        {
            if (attempt.Status != "InProgress")
                return false;

            if (nowUtc > test.EndDate)
                return true;

            if (attempt.AllowedEndAt.HasValue)
                return nowUtc > attempt.AllowedEndAt.Value;

            var durationDeadline = attempt.StartedAt.AddMinutes(test.DurationMinutes);
            return nowUtc > durationDeadline;
        }
    }
}
