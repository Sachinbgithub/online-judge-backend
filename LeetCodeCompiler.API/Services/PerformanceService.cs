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

        // === Group 1: Browse APIs Implementation ===

        public async Task<PagedResult<FacultyTestSummaryItem>> GetTestsByFacultyAsync(long facultyId, int pageNumber, int pageSize)
        {
            var query = _context.CodingTests
                .Where(t => t.CreatedBy == (int)facultyId);

            var totalCount = await query.CountAsync();
            var tests = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<FacultyTestSummaryItem>();
            foreach (var test in tests)
            {
                var attempts = await _context.CodingTestAttempts
                    .Where(a => a.CodingTestId == test.Id)
                    .ToListAsync();

                var assignedCount = await _context.AssignedCodingTests
                    .Where(a => a.CodingTestId == test.Id && !a.IsDeleted)
                    .CountAsync();

                result.Add(new FacultyTestSummaryItem
                {
                    CodingTestId = test.Id,
                    TestName = test.TestName,
                    StartDate = test.StartDate,
                    EndDate = test.EndDate,
                    TotalStudents = assignedCount,
                    AttemptedStudents = attempts.Select(a => a.UserId).Distinct().Count(),
                    AverageScore = attempts.Any() ? attempts.Average(a => a.Percentage) : 0,
                    PassRate = attempts.Any() ? (double)attempts.Count(a => a.Percentage >= 60) / attempts.Count * 100 : 0,
                    CompletionPercentage = assignedCount > 0 ? (double)attempts.Select(a => a.UserId).Distinct().Count() / assignedCount * 100 : 0,
                    IsActive = test.IsActive,
                    IsPublished = test.IsPublished
                });
            }

            return new PagedResult<FacultyTestSummaryItem>
            {
                Items = result,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<ClassStudentSummaryItem>> GetStudentSummaryByClassAsync(int classId, int pageNumber, int pageSize)
        {
            // Find all students assigned to ANY test in this class
            var userIdsQuery = _context.AssignedCodingTests
                .Include(a => a.CodingTest)
                .Where(a => a.CodingTest.ClassId == classId && !a.IsDeleted)
                .Select(a => a.AssignedToUserId)
                .Distinct();

            var totalCount = await userIdsQuery.CountAsync();
            var userIds = await userIdsQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<ClassStudentSummaryItem>();
            foreach (var userId in userIds)
            {
                var profile = await _studentProfileService.GetStudentProfileAsync(userId);
                var assignments = await _context.AssignedCodingTests
                    .Include(a => a.CodingTest)
                    .Where(a => a.AssignedToUserId == userId && a.CodingTest.ClassId == classId && !a.IsDeleted)
                    .ToListAsync();

                var attempts = await _context.CodingTestAttempts
                    .Where(a => a.UserId == userId && assignments.Select(asgn => asgn.CodingTestId).Contains(a.CodingTestId))
                    .ToListAsync();

                result.Add(new ClassStudentSummaryItem
                {
                    UserId = userId,
                    FullName = profile?.FullName ?? ("User " + userId),
                    EmailId = profile?.EmailId ?? "",
                    RollNo = profile?.RollNo ?? "",
                    TotalTestsAssigned = assignments.Count,
                    TestsAttempted = attempts.Select(a => a.CodingTestId).Distinct().Count(),
                    AverageScorePercentage = attempts.Any() ? attempts.Average(a => a.Percentage) : 0,
                    LastActive = attempts.Any() ? attempts.Max(a => a.SubmittedAt ?? a.CreatedAt) : (DateTime?)null,
                    OverallStatus = attempts.Count >= assignments.Count ? "On Track" : (attempts.Count > 0 ? "InProgress" : "Behind")
                });
            }

            return new PagedResult<ClassStudentSummaryItem>
            {
                Items = result,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<CollegeClassSummaryItem>> GetClassSummaryByCollegeAsync(int collegeId, int pageNumber, int pageSize)
        {
            var classesQuery = _context.CodingTests
                .Where(t => t.CollegeId == collegeId && t.ClassId != 0)
                .Select(t => t.ClassId)
                .Distinct();

            var totalCount = await classesQuery.CountAsync();
            var classIds = await classesQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<CollegeClassSummaryItem>();
            foreach (var classId in classIds)
            {
                var studentCount = await _context.AssignedCodingTests
                    .Include(a => a.CodingTest)
                    .Where(a => a.CodingTest.ClassId == classId && !a.IsDeleted)
                    .Select(a => a.AssignedToUserId)
                    .Distinct()
                    .CountAsync();

                var tests = await _context.CodingTests.Where(t => t.ClassId == classId).ToListAsync();
                var attempts = await _context.CodingTestAttempts
                    .Where(a => tests.Select(t => t.Id).Contains(a.CodingTestId))
                    .ToListAsync();

                result.Add(new CollegeClassSummaryItem
                {
                    ClassId = classId,
                    ClassName = "Class " + classId,
                    TotalStudents = studentCount,
                    TotalTestsAssigned = tests.Count,
                    AverageScorePercentage = attempts.Any() ? attempts.Average(a => a.Percentage) : 0,
                    AverageParticipationRate = studentCount > 0 ? (double)attempts.Select(a => a.UserId).Distinct().Count() / (studentCount * tests.Count) * 100 : 0
                });
            }

            return new PagedResult<CollegeClassSummaryItem>
            {
                Items = result,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<StudentTestHistoryItem>> GetStudentTestHistoryForFacultyAsync(long studentId, long facultyId, int pageNumber, int pageSize)
        {
            var query = _context.CodingTestAttempts
                .Include(a => a.CodingTest)
                .Where(a => a.UserId == studentId && a.CodingTest.CreatedBy == (int)facultyId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new StudentTestHistoryItem
                {
                    CodingTestId = a.CodingTestId,
                    TestName = a.CodingTest.TestName,
                    AttemptNumber = a.AttemptNumber,
                    Score = a.TotalScore,
                    MaxScore = a.MaxScore,
                    Percentage = a.Percentage,
                    IsPassed = a.Percentage >= 60,
                    SubmissionTime = a.SubmittedAt ?? a.CreatedAt,
                    TimeSpentMinutes = a.TimeSpentMinutes,
                    Status = a.Status
                })
                .ToListAsync();

            return new PagedResult<StudentTestHistoryItem>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<TestCompletionStatusResponse> GetTestCompletionByClassAsync(int codingTestId, int classId)
        {
            var assignedUsers = await _context.AssignedCodingTests
                .Where(a => a.CodingTestId == codingTestId && !a.IsDeleted)
                .Select(a => a.AssignedToUserId)
                .ToListAsync();

            var attempts = await _context.CodingTestAttempts
                .Where(a => a.CodingTestId == codingTestId && assignedUsers.Contains(a.UserId))
                .ToListAsync();

            int attemptedCount = attempts.Select(a => a.UserId).Distinct().Count();
            int inProgressCount = attempts.Where(a => a.Status == "InProgress").Select(a => a.UserId).Distinct().Count();

            return new TestCompletionStatusResponse
            {
                CodingTestId = codingTestId,
                ClassId = classId,
                TotalAssigned = assignedUsers.Count,
                Attempted = attemptedCount,
                InProgress = inProgressCount,
                NotAttempted = assignedUsers.Count - attemptedCount,
                CompletionRate = assignedUsers.Count > 0 ? (double)attemptedCount / assignedUsers.Count * 100 : 0
            };
        }

        // === Group 2: User Performance by UserId Implementation ===

        public async Task<UserFullPerformanceResponse> GetUserFullPerformanceAsync(long userId)
        {
            var profile = await _studentProfileService.GetStudentProfileAsync(userId);
            var overview = await GetStudentOverviewAsync(userId);

            return new UserFullPerformanceResponse
            {
                UserId = userId,
                FullName = profile?.FullName ?? ("User " + userId),
                CodingTestsAttempted = overview.TotalCodingTestsAttempted,
                AvgCodingTestPercentage = overview.AverageCodingTestScorePercentage,
                PracticeTestsAttempted = overview.TotalPracticeTestsAttempted,
                AvgPracticeTestPercentage = overview.AveragePracticeTestPercentage,
                FreeProblemsSolved = overview.TotalFreeProblemsSolved,
                FreePracticeSuccessRate = overview.OverallFreePracticeSuccessRate,
                TotalTimeSpentMinutes = overview.TotalTimeSpentMinutes,
                RecentActivity = overview.RecentActivity
            };
        }

        public async Task<PagedResult<UserCodingTestSummaryItem>> GetUserCodingTestHistoryAsync(long userId, int pageNumber, int pageSize)
        {
            var query = _context.CodingTestAttempts
                .Include(a => a.CodingTest)
                .Where(a => a.UserId == userId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new UserCodingTestSummaryItem
                {
                    CodingTestId = a.CodingTestId,
                    TestName = a.CodingTest != null ? a.CodingTest.TestName : "Unknown Test",
                    AttemptNumber = a.AttemptNumber,
                    TotalScore = a.TotalScore,
                    MaxScore = a.MaxScore,
                    Percentage = a.Percentage,
                    IsPassed = a.Percentage >= 60,
                    SubmissionTime = a.SubmittedAt ?? a.CreatedAt,
                    TimeSpentMinutes = a.TimeSpentMinutes,
                    Status = a.Status
                })
                .ToListAsync();

            return new PagedResult<UserCodingTestSummaryItem>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<UserPracticeTestSummaryItem>> GetUserPracticeTestHistoryAsync(long userId, int pageNumber, int pageSize)
        {
            var query = _context.PracticeTestResults
                .Include(r => r.PracticeTest)
                .Where(r => r.UserId == (int)userId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new UserPracticeTestSummaryItem
                {
                    PracticeTestId = r.PracticeTestId,
                    TestName = r.PracticeTest != null ? r.PracticeTest.TestName : "Unknown Practice",
                    AttemptNumber = r.AttemptNumber,
                    ObtainedMarks = r.ObtainedMarks,
                    TotalMarks = r.TotalMarks,
                    Percentage = r.Percentage,
                    IsPassed = r.IsPassed,
                    CompletionTime = r.CompletedAt ?? r.StartedAt,
                    TimeTakenMinutes = r.TimeTakenMinutes ?? 0,
                    Status = r.Status
                })
                .ToListAsync();

            return new PagedResult<UserPracticeTestSummaryItem>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<UserPracticeTestDetailResponse> GetUserPracticeTestDetailAsync(long userId, int practiceTestId, int? attemptNumber)
        {
            var query = _context.PracticeTestResults
                .Include(r => r.PracticeTest)
                .Include(r => r.QuestionResults)
                    .ThenInclude(qr => qr.Problem)
                .Where(r => r.UserId == (int)userId && r.PracticeTestId == practiceTestId);

            if (attemptNumber.HasValue)
                query = query.Where(r => r.AttemptNumber == attemptNumber.Value);
            else
                query = query.OrderByDescending(r => r.AttemptNumber);

            var result = await query.FirstOrDefaultAsync();
            if (result == null) return null;

            return new UserPracticeTestDetailResponse
            {
                PracticeTestId = result.PracticeTestId,
                TestName = result.PracticeTest?.TestName ?? "Unknown",
                UserId = result.UserId,
                AttemptNumber = result.AttemptNumber,
                StartedAt = result.StartedAt,
                CompletedAt = result.CompletedAt,
                Percentage = result.Percentage,
                Questions = result.QuestionResults.Select(qr => new UserPracticeQuestionDetail
                {
                    ProblemId = qr.ProblemId,
                    ProblemTitle = qr.Problem?.Title ?? "Problem " + qr.ProblemId,
                    SubmittedCode = qr.SubmittedCode ?? "",
                    Language = qr.Language,
                    Marks = qr.Marks,
                    ObtainedMarks = qr.ObtainedMarks,
                    IsCorrect = qr.IsCorrect,
                    ExecutionTime = qr.ExecutionTime ?? 0,
                    Status = qr.ExecutionStatus
                }).ToList()
            };
        }

        public async Task<UserFreePracticeSummaryResponse> GetUserFreePracticeSummaryAsync(long userId)
        {
            var stats = await GetFreePracticeSummaryAsync(userId);
            return new UserFreePracticeSummaryResponse
            {
                TotalProblemsAttempted = stats.TotalProblemsAttempted,
                TotalProblemsSolved = stats.TotalProblemsSolved,
                AverageSuccessRate = stats.AverageSuccessRate,
                TotalTimeSpentSeconds = stats.TotalTimeSpentSeconds,
                LanguageBreakdown = stats.LanguageBreakdown
            };
        }
    }
}
