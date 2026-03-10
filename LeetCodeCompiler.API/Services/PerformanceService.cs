using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public class PerformanceService : IPerformanceService
    {
        private readonly AppDbContext _context;
        private readonly StudentProfileService _studentProfileService;
        private readonly ILogger<PerformanceService> _logger;

        public PerformanceService(
            AppDbContext context,
            StudentProfileService studentProfileService,
            ILogger<PerformanceService> logger)
        {
            _context = context;
            _studentProfileService = studentProfileService;
            _logger = logger;
        }

        public async Task<StudentOverviewResponse> GetStudentOverviewAsync(long userId)
        {
            // 1. Coding Tests
            var codingTestAttempts = await _context.CodingTestAttempts
                .Where(a => a.UserId == userId)
                .ToListAsync();

            int totalCodingTests = codingTestAttempts.Select(a => a.CodingTestId).Distinct().Count();
            int totalCodingScore = codingTestAttempts.Sum(a => a.TotalScore);
            int totalCodingMaxScore = codingTestAttempts.Sum(a => a.MaxScore);
            double avgCodingPerc = codingTestAttempts.Any() ? codingTestAttempts.Average(a => a.Percentage) : 0;

            // 2. Practice Tests
            var practiceTestResults = await _context.PracticeTestResults
                .Where(r => r.UserId == userId)
                .ToListAsync();

            int totalPracticeTests = practiceTestResults.Count;
            int passedPracticeTests = practiceTestResults.Count(r => r.IsPassed);
            double avgPracticePerc = practiceTestResults.Any() ? (double)practiceTestResults.Average(r => r.Percentage) : 0;

            // 3. Free Practice
            var freePracticeRuns = await _context.UserCodingActivityLogs
                .Where(log => log.UserId == userId && string.IsNullOrEmpty(log.TestType))
                .ToListAsync();
            
            var coreResults = await _context.CoreQuestionResults
                .Where(r => r.UserId == userId)
                .ToListAsync();

            int totalFreeAttempted = coreResults.Select(r => r.ProblemId).Distinct().Count();
            int totalFreeSolved = coreResults.Where(r => r.FailedTestCases == 0).Select(r => r.ProblemId).Distinct().Count();
            double avgFreeSuccess = coreResults.Any() ? coreResults.Average(r => r.TotalTestCases > 0 ? (double)r.PassedTestCases / r.TotalTestCases * 100 : 0) : 0;
            
            int totalTimeSpent = freePracticeRuns.Sum(log => log.TimeTakenSeconds) / 60;
            
            var highestLang = coreResults
                .GroupBy(r => r.LanguageUsed)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "";

            // 4. Activity List
            var recentCodes = codingTestAttempts
                .Select(a => new RecentActivityItem {
                    ActivityType = "CodingTest",
                    Title = "Coding Test " + a.CodingTestId,
                    Date = a.SubmittedAt ?? a.CreatedAt,
                    ScoreOrPercentage = a.Percentage,
                    Status = a.Status
                });

            var recentPractices = practiceTestResults
                .Select(p => new RecentActivityItem {
                    ActivityType = "PracticeTest",
                    Title = "Practice Test " + p.PracticeTestId,
                    Date = p.CompletedAt ?? p.StartedAt,
                    ScoreOrPercentage = (double)p.Percentage,
                    Status = p.IsPassed ? "Passed" : "Failed"
                });

            var top5 = recentCodes.Concat(recentPractices)
                .OrderByDescending(a => a.Date)
                .Take(5)
                .ToList();

            return new StudentOverviewResponse
            {
                UserId = userId,
                TotalCodingTestsAttempted = totalCodingTests,
                AverageCodingTestScorePercentage = avgCodingPerc,
                TotalCodingTestMarksObtained = totalCodingScore,
                TotalCodingTestMaxMarks = totalCodingMaxScore,
                TotalPracticeTestsAttempted = totalPracticeTests,
                PassedPracticeTests = passedPracticeTests,
                AveragePracticeTestPercentage = avgPracticePerc,
                TotalFreeProblemsAttempted = totalFreeAttempted,
                TotalFreeProblemsSolved = totalFreeSolved,
                OverallFreePracticeSuccessRate = avgFreeSuccess,
                TotalTimeSpentMinutes = totalTimeSpent,
                MostUsedLanguage = highestLang,
                RecentActivity = top5
            };
        }

        public async Task<PagedResult<CodingTestHistoryItem>> GetCodingTestHistoryAsync(long userId, int pageNumber, int pageSize)
        {
            var query = _context.CodingTestAttempts
                .Include(a => a.CodingTest)
                .Where(a => a.UserId == userId && (a.Status == "Completed" || a.Status == "Submitted"));

            var totalCount = await query.CountAsync();
            var attempts = await query
                .OrderByDescending(a => a.SubmittedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new CodingTestHistoryItem
                {
                    CodingTestId = a.CodingTestId,
                    TestName = a.CodingTest != null ? a.CodingTest.TestName : ("Test " + a.CodingTestId),
                    AttemptNumber = a.AttemptNumber,
                    TotalScore = a.TotalScore,
                    MaxScore = a.MaxScore,
                    Percentage = a.Percentage,
                    IsPassed = a.Percentage >= (a.CodingTest != null && a.CodingTest.TotalMarks > 0 ? 60.0 : 60.0), // default to 60 pass rate for now
                    SubmissionTime = a.SubmittedAt ?? a.CreatedAt,
                    IsLateSubmission = a.IsLateSubmission,
                    TimeSpentMinutes = a.TimeSpentMinutes
                })
                .ToListAsync();

            return new PagedResult<CodingTestHistoryItem>
            {
                Items = attempts,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<PracticeTestHistoryItem>> GetPracticeTestHistoryAsync(long userId, int pageNumber, int pageSize)
        {
            var query = _context.PracticeTestResults
                .Include(r => r.PracticeTest)
                .Where(r => r.UserId == userId);

            var totalCount = await query.CountAsync();
            var results = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new PracticeTestHistoryItem
                {
                    PracticeTestId = r.PracticeTestId,
                    TestName = r.PracticeTest != null ? r.PracticeTest.TestName : ("Practice " + r.PracticeTestId),
                    AttemptNumber = r.AttemptNumber,
                    ObtainedMarks = r.ObtainedMarks,
                    TotalMarks = r.TotalMarks,
                    Percentage = r.Percentage,
                    IsPassed = r.IsPassed,
                    Status = r.Status,
                    StartedAt = r.StartedAt,
                    CompletedAt = r.CompletedAt,
                    TimeTakenMinutes = r.TimeTakenMinutes
                })
                .ToListAsync();

            return new PagedResult<PracticeTestHistoryItem>
            {
                Items = results,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<FreePracticeSummaryResponse> GetFreePracticeSummaryAsync(long userId)
        {
            var logs = await _context.UserCodingActivityLogs
                .Where(l => l.UserId == userId && string.IsNullOrEmpty(l.TestType))
                .ToListAsync();

            var cores = await _context.CoreQuestionResults
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var langBreakdown = cores
                .GroupBy(r => r.LanguageUsed)
                .ToDictionary(g => g.Key, g => g.Count());

            return new FreePracticeSummaryResponse
            {
                TotalProblemsAttempted = cores.Select(c => c.ProblemId).Distinct().Count(),
                TotalProblemsSolved = cores.Where(c => c.FailedTestCases == 0).Select(c => c.ProblemId).Distinct().Count(),
                AverageSuccessRate = cores.Any() ? cores.Average(c => c.TotalTestCases > 0 ? (double)c.PassedTestCases / c.TotalTestCases * 100 : 0) : 0,
                TotalTimeSpentSeconds = logs.Sum(l => l.TimeTakenSeconds),
                TotalRunClicks = logs.Sum(l => l.RunClickCount),
                TotalSubmitClicks = logs.Sum(l => l.SubmitClickCount),
                LanguageBreakdown = langBreakdown
            };
        }

        public async Task<PagedResult<FacultyStudentResultItem>> GetStudentsForTestAsync(int codingTestId, int pageNumber, int pageSize)
        {
            var query = _context.CodingTestAttempts
                .Where(a => a.CodingTestId == codingTestId);

            var totalCount = await query.Select(a => a.UserId).Distinct().CountAsync();
            
            var latestAttempts = await query
                .GroupBy(a => a.UserId)
                .Select(g => g.OrderByDescending(a => a.AttemptNumber).First())
                .OrderByDescending(a => a.Percentage)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<FacultyStudentResultItem>();
            int rank = ((pageNumber - 1) * pageSize) + 1;
            
            foreach (var attempt in latestAttempts)
            {
                var profile = await _studentProfileService.GetStudentProfileAsync(attempt.UserId);
                
                result.Add(new FacultyStudentResultItem
                {
                    UserId = attempt.UserId,
                    FullName = profile?.FullName ?? ("User " + attempt.UserId),
                    EmailId = profile?.EmailId ?? "",
                    RollNo = profile?.RollNo ?? "",
                    TotalScore = attempt.TotalScore,
                    MaxScore = attempt.MaxScore,
                    Percentage = attempt.Percentage,
                    Rank = rank++,
                    IsLateSubmission = attempt.IsLateSubmission,
                    Status = attempt.Status,
                    SubmissionTime = attempt.SubmittedAt,
                    TimeSpentMinutes = attempt.TimeSpentMinutes
                });
            }

            return new PagedResult<FacultyStudentResultItem>
            {
                Items = result,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<LeaderboardItem>> GetLeaderboardAsync(int codingTestId, int pageNumber, int pageSize)
        {
            var query = _context.CodingTestAttempts
                .Where(a => a.CodingTestId == codingTestId && (a.Status == "Completed" || a.Status == "Submitted"));

            var totalCount = await query.Select(a => a.UserId).Distinct().CountAsync();

            var bestAttempts = await query
                .GroupBy(a => a.UserId)
                .Select(g => g.OrderByDescending(a => a.Percentage).ThenBy(a => a.TimeSpentMinutes).First())
                .OrderByDescending(a => a.Percentage)
                .ThenBy(a => a.TimeSpentMinutes)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<LeaderboardItem>();
            int rank = ((pageNumber - 1) * pageSize) + 1;

            foreach (var attempt in bestAttempts)
            {
                var profile = await _studentProfileService.GetStudentProfileAsync(attempt.UserId);

                result.Add(new LeaderboardItem
                {
                    Rank = rank++,
                    UserId = attempt.UserId,
                    FullName = profile?.FullName ?? ("User " + attempt.UserId),
                    TotalScore = attempt.TotalScore,
                    Percentage = attempt.Percentage,
                    SubmissionTime = attempt.SubmittedAt ?? attempt.CreatedAt,
                    TimeSpentMinutes = attempt.TimeSpentMinutes
                });
            }

            return new PagedResult<LeaderboardItem>
            {
                Items = result,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<PracticeStudentResultItem>> GetPracticeStudentsAsync(int practiceTestId, int pageNumber, int pageSize)
        {
            var query = _context.PracticeTestResults
                .Where(r => r.PracticeTestId == practiceTestId);

            var totalCount = await query.Select(r => r.UserId).Distinct().CountAsync();

            var latestResults = await query
                .GroupBy(r => r.UserId)
                .Select(g => g.OrderByDescending(r => r.AttemptNumber).First())
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var responseList = new List<PracticeStudentResultItem>();
            foreach (var r in latestResults)
            {
                var profile = await _studentProfileService.GetStudentProfileAsync(r.UserId);
                responseList.Add(new PracticeStudentResultItem
                {
                    UserId = r.UserId,
                    FullName = profile?.FullName ?? ("User " + r.UserId),
                    AttemptNumber = r.AttemptNumber,
                    ObtainedMarks = r.ObtainedMarks,
                    TotalMarks = r.TotalMarks,
                    Percentage = r.Percentage,
                    IsPassed = r.IsPassed,
                    TimeTakenMinutes = r.TimeTakenMinutes,
                    Status = r.Status,
                    StartedAt = r.StartedAt
                });
            }

            return new PagedResult<PracticeStudentResultItem>
            {
                Items = responseList,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<ProblemAnalysisItem>> GetProblemAnalysisAsync(int codingTestId, int pageNumber, int pageSize)
        {
            var query = _context.CodingTestSubmissionResults
                .Include(r => r.Submission)
                .Where(r => r.Submission!.CodingTestId == codingTestId);

            var totalCount = await query.Select(r => r.ProblemId).Distinct().CountAsync();

            var groupedByProblem = await query
                .Where(r => r.Submission != null)
                .GroupBy(r => r.ProblemId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<ProblemAnalysisItem>();

            foreach (var group in groupedByProblem)
            {
                int totalAttempts = group.Select(r => r.SubmissionId).Distinct().Count();
                int successfulAttempts = group.Where(r => r.IsPassed).Select(r => r.SubmissionId).Distinct().Count();

                var errorTypes = group
                    .Where(r => !r.IsPassed && !string.IsNullOrEmpty(r.ErrorType))
                    .GroupBy(r => r.ErrorType!)
                    .ToDictionary(g => g.Key, g => g.Count());

                result.Add(new ProblemAnalysisItem
                {
                    ProblemId = group.Key,
                    ProblemTitle = "Problem " + group.Key, // would join Problem table if available easily
                    TotalAttempts = totalAttempts,
                    SuccessfulAttempts = successfulAttempts,
                    PassRate = totalAttempts > 0 ? (double)successfulAttempts / totalAttempts * 100 : 0,
                    AverageScore = 0, // derived from QuestionSubmission level
                    AverageExecutionTimeMs = group.Any() ? group.Average(r => r.ExecutionTimeMs) : 0,
                    CommonErrorTypes = errorTypes
                });
            }

            return new PagedResult<ProblemAnalysisItem>
            {
                Items = result,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
