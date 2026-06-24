using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public static class TestMonitorStatusBuilder
    {
        public static string Compute(CodingTestAttempt? latestAttempt)
        {
            if (latestAttempt == null)
                return TestMonitorStatuses.NotStarted;

            if (latestAttempt.Status == "InProgress")
                return TestMonitorStatuses.TestRunning;

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
