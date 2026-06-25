using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public static class TestMonitorStatusBuilder
    {
        public static string Compute(
            CodingTestAttempt? latestAttempt,
            CodingTest? test = null,
            DateTime? nowUtc = null)
        {
            if (latestAttempt == null)
                return TestMonitorStatuses.NotStarted;

            var now = nowUtc ?? DateTime.UtcNow;

            if (latestAttempt.Status == "InProgress")
            {
                if (test != null && AttemptStaleHelper.IsStale(test, latestAttempt, now))
                    return TestMonitorStatuses.StoppedAbnormally;

                return TestMonitorStatuses.TestRunning;
            }

            if (latestAttempt.Status == "Abandoned")
                return TestMonitorStatuses.StoppedAbnormally;

            if (latestAttempt.Status is "Submitted" or "Completed")
            {
                if (latestAttempt.IntegrityStatus == "Disqualified"
                    || latestAttempt.SubmissionReason == SubmissionReasons.AutoDQ)
                {
                    return TestMonitorStatuses.Breached;
                }

                if (latestAttempt.SubmissionReason == SubmissionReasons.NetworkLoss)
                    return TestMonitorStatuses.StoppedAbnormally;

                return TestMonitorStatuses.Submitted;
            }

            return TestMonitorStatuses.NotStarted;
        }
    }
}
