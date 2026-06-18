namespace LeetCodeCompiler.API.Models
{
    public enum AutoSubmitReason
    {
        Disqualified,
        NetworkLoss
    }

    public static class SubmissionReasons
    {
        public const string Manual = "Manual";
        public const string AutoDQ = "AutoDQ";
        public const string NetworkLoss = "NetworkLoss";
    }

    public static class StudentTestActions
    {
        public const string Start = "Start";
        public const string Continue = "Continue";
        public const string ViewResults = "ViewResults";
        public const string Disqualified = "Disqualified";
        public const string NetworkLossSubmitted = "NetworkLossSubmitted";
        public const string ResumeAvailable = "ResumeAvailable";
        public const string Expired = "Expired";
    }

    public static class ResumeGrantStatuses
    {
        public const string Pending = "Pending";
        public const string Used = "Used";
        public const string Expired = "Expired";
    }
}
