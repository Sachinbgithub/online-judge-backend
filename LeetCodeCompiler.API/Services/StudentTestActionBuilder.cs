using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public static class StudentTestActionBuilder
    {
        public static string Compute(
            CodingTest test,
            CodingTestAttempt? latestAttempt,
            CodingTestResumeGrant? pendingGrant,
            DateTime nowUtc)
        {
            var inWindow = nowUtc >= test.StartDate && nowUtc <= test.EndDate;

            if (pendingGrant != null
                && pendingGrant.Status == ResumeGrantStatuses.Pending
                && pendingGrant.AllowedEndAt > nowUtc
                && nowUtc <= test.EndDate)
            {
                return StudentTestActions.ResumeAvailable;
            }

            if (!inWindow)
                return StudentTestActions.Expired;

            if (latestAttempt == null)
                return StudentTestActions.Start;

            if (latestAttempt.Status == "InProgress")
                return StudentTestActions.Continue;

            if (latestAttempt.Status is "Submitted" or "Completed")
            {
                if (latestAttempt.IntegrityStatus == "Disqualified")
                    return StudentTestActions.Disqualified;

                if (latestAttempt.SubmissionReason == SubmissionReasons.NetworkLoss)
                    return StudentTestActions.NetworkLossSubmitted;

                return StudentTestActions.ViewResults;
            }

            return StudentTestActions.Start;
        }
    }
}
