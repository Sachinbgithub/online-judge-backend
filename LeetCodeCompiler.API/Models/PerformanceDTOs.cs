using System;
using System.Collections.Generic;

namespace LeetCodeCompiler.API.Models
{
    public class StudentOverviewResponse
    {
        public long UserId { get; set; }
        
        // Coding Tests Summary
        public int TotalCodingTestsAttempted { get; set; }
        public double AverageCodingTestScorePercentage { get; set; }
        public int TotalCodingTestMarksObtained { get; set; }
        public int TotalCodingTestMaxMarks { get; set; }

        // Practice Tests Summary
        public int TotalPracticeTestsAttempted { get; set; }
        public double AveragePracticeTestPercentage { get; set; }
        public int PassedPracticeTests { get; set; }
        
        // Free Practice Summary
        public int TotalFreeProblemsAttempted { get; set; }
        public int TotalFreeProblemsSolved { get; set; }
        public double OverallFreePracticeSuccessRate { get; set; }
        
        // Engagement
        public int TotalTimeSpentMinutes { get; set; }
        public string MostUsedLanguage { get; set; } = "";
        
        // Recent Activity (Merged top 5 across all)
        public List<RecentActivityItem> RecentActivity { get; set; } = new List<RecentActivityItem>();
    }

    public class RecentActivityItem
    {
        public string ActivityType { get; set; } = ""; // "CodingTest", "PracticeTest", "FreePractice"
        public string Title { get; set; } = "";
        public DateTime Date { get; set; }
        public double ScoreOrPercentage { get; set; }
        public string Status { get; set; } = ""; // "Passed", "Failed", "Completed"
    }

    public class CodingTestHistoryItem
    {
        public int CodingTestId { get; set; }
        public string TestName { get; set; } = "";
        public int AttemptNumber { get; set; }
        public int TotalScore { get; set; }
        public int MaxScore { get; set; }
        public double Percentage { get; set; }
        public bool IsPassed { get; set; }
        public DateTime SubmissionTime { get; set; }
        public int TimeSpentMinutes { get; set; }
        public bool IsLateSubmission { get; set; }
    }

    public class PracticeTestHistoryItem
    {
        public int PracticeTestId { get; set; }
        public string TestName { get; set; } = "";
        public int AttemptNumber { get; set; }
        public decimal ObtainedMarks { get; set; }
        public int TotalMarks { get; set; }
        public decimal Percentage { get; set; }
        public bool IsPassed { get; set; }
        public string Status { get; set; } = "";
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? TimeTakenMinutes { get; set; }
    }

    public class FreePracticeSummaryResponse
    {
        public int TotalProblemsAttempted { get; set; }
        public int TotalProblemsSolved { get; set; }
        public double AverageSuccessRate { get; set; }
        public int TotalTimeSpentSeconds { get; set; }
        public int TotalRunClicks { get; set; }
        public int TotalSubmitClicks { get; set; }
        public Dictionary<string, int> LanguageBreakdown { get; set; } = new Dictionary<string, int>();
    }

    public class FacultyStudentResultItem
    {
        public long UserId { get; set; }
        public string FullName { get; set; } = "";
        public string EmailId { get; set; } = "";
        public string RollNo { get; set; } = "";
        public int TotalScore { get; set; }
        public int MaxScore { get; set; }
        public double Percentage { get; set; }
        public int Rank { get; set; }
        public int TimeSpentMinutes { get; set; }
        public bool IsLateSubmission { get; set; }
        public string Status { get; set; } = ""; // "Completed", "Not Attempted", "InProgress"
        public DateTime? SubmissionTime { get; set; }
    }

    public class LeaderboardItem
    {
        public int Rank { get; set; }
        public long UserId { get; set; }
        public string FullName { get; set; } = "";
        public int TotalScore { get; set; }
        public double Percentage { get; set; }
        public int TimeSpentMinutes { get; set; }
        public DateTime SubmissionTime { get; set; }
    }

    public class PracticeStudentResultItem
    {
        public long UserId { get; set; }
        public string FullName { get; set; } = "";
        public int AttemptNumber { get; set; }
        public decimal ObtainedMarks { get; set; }
        public int TotalMarks { get; set; }
        public decimal Percentage { get; set; }
        public bool IsPassed { get; set; }
        public int? TimeTakenMinutes { get; set; }
        public string Status { get; set; } = "";
        public DateTime StartedAt { get; set; }
    }

    public class ProblemAnalysisItem
    {
        public int ProblemId { get; set; }
        public string ProblemTitle { get; set; } = "";
        public int TotalAttempts { get; set; }
        public int SuccessfulAttempts { get; set; }
        public double PassRate { get; set; }
        public double AverageScore { get; set; }
        public double AverageExecutionTimeMs { get; set; }
        public Dictionary<string, int> CommonErrorTypes { get; set; } = new Dictionary<string, int>();
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
