using LeetCodeCompiler.API.Models;
using LeetCodeCompiler.API.Data;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Services
{
    public class ActivityTrackingService : IActivityTrackingService
    {
        private readonly AppDbContext _context;
        private readonly IQuestionPoolService _questionPoolService;

        public ActivityTrackingService(AppDbContext context, IQuestionPoolService questionPoolService)
        {
            _context = context;
            _questionPoolService = questionPoolService;
        }

        public async Task<UserCodingActivityLog> LogUserActivityAsync(LogUserActivityRequest request)
        {
            if (request.CodingTestAttemptId.HasValue)
            {
                await EnsureUserOwnsAttemptAsync(request.CodingTestAttemptId.Value, request.UserId);
                await ValidateProblemInAttemptAsync(request.CodingTestAttemptId.Value, request.ProblemId);

                return await GetOrCreateAssessmentSessionAsync(
                    request.UserId,
                    request.ProblemId,
                    request.CodingTestAttemptId.Value,
                    string.IsNullOrWhiteSpace(request.TestType) ? "session" : request.TestType,
                    request.CodingTestId,
                    request.CodingTestQuestionAttemptId);
            }

            return await LogUserActivityAsync(
                request.UserId,
                request.ProblemId,
                request.AttemptNumber,
                request.TestType,
                request.TimeTakenSeconds);
        }

        public async Task EnsureUserOwnsAttemptAsync(int codingTestAttemptId, int userId)
        {
            var attempt = await _context.CodingTestAttempts.FindAsync(codingTestAttemptId);
            if (attempt == null)
                throw new ArgumentException($"Attempt {codingTestAttemptId} not found");
            if (attempt.UserId != userId)
                throw new UnauthorizedAccessException("You do not own this attempt.");
        }

        public async Task EnsureUserOwnsActivityLogAsync(int activityLogId, int userId)
        {
            var log = await _context.UserCodingActivityLogs.FindAsync(activityLogId);
            if (log == null)
                throw new ArgumentException($"Activity log {activityLogId} not found");
            if (log.UserId != userId)
                throw new UnauthorizedAccessException("You do not own this activity log.");
        }

        public async Task<UserCodingActivityLog> GetOrCreateAssessmentSessionAsync(
            int userId,
            int problemId,
            int codingTestAttemptId,
            string testType,
            int? codingTestId = null,
            int? codingTestQuestionAttemptId = null)
        {
            await EnsureUserOwnsAttemptAsync(codingTestAttemptId, userId);
            await ValidateProblemInAttemptAsync(codingTestAttemptId, problemId);

            var attempt = await _context.CodingTestAttempts.FindAsync(codingTestAttemptId);
            var existing = await _context.UserCodingActivityLogs
                .Where(l => l.CodingTestAttemptId == codingTestAttemptId
                         && l.ProblemId == problemId
                         && l.SessionStatus == "Active"
                         && !l.IsSessionAbandoned)
                .OrderByDescending(l => l.CreatedAt)
                .FirstOrDefaultAsync();

            if (existing != null)
                return existing;

            var now = DateTime.UtcNow;
            var log = new UserCodingActivityLog
            {
                UserId = userId,
                ProblemId = problemId,
                AttemptNumber = attempt?.AttemptNumber ?? 0,
                TestType = testType,
                TimeTakenSeconds = 0,
                CreatedAt = now,
                UpdatedAt = now,
                StartTime = now,
                EndTime = now,
                CodingTestId = codingTestId ?? attempt?.CodingTestId,
                CodingTestAttemptId = codingTestAttemptId,
                CodingTestQuestionAttemptId = codingTestQuestionAttemptId,
                SessionStatus = "Active",
                Source = "assessment"
            };

            _context.UserCodingActivityLogs.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async Task UpdateAssessmentMetricsAsync(int activityLogId, int userId, AssessmentActivityMetrics metrics)
        {
            await EnsureUserOwnsActivityLogAsync(activityLogId, userId);

            await UpdateActivityMetricsAsync(
                activityLogId,
                metrics.TimeTakenSeconds,
                metrics.LanguageSwitchCount,
                metrics.EraseCount,
                metrics.SaveCount,
                metrics.RunClickCount,
                metrics.SubmitClickCount,
                metrics.LoginLogoutCount,
                metrics.IsSessionAbandoned,
                metrics.PassedTestCaseIDs,
                metrics.FailedTestCaseIDs,
                metrics.StartTime,
                metrics.EndTime ?? DateTime.UtcNow);
        }

        public async Task<UserCodingActivityLog> CompleteAssessmentSessionAsync(
            int userId,
            int problemId,
            int codingTestAttemptId,
            long? submissionId,
            AssessmentActivityMetrics metrics,
            string testType = "submit",
            int? codingTestQuestionAttemptId = null)
        {
            await EnsureUserOwnsAttemptAsync(codingTestAttemptId, userId);

            var active = await _context.UserCodingActivityLogs
                .Where(l => l.CodingTestAttemptId == codingTestAttemptId
                         && l.ProblemId == problemId
                         && l.SessionStatus == "Active")
                .OrderByDescending(l => l.CreatedAt)
                .FirstOrDefaultAsync();

            var attempt = await _context.CodingTestAttempts.FindAsync(codingTestAttemptId);
            var now = DateTime.UtcNow;

            if (active == null)
            {
                active = new UserCodingActivityLog
                {
                    UserId = userId,
                    ProblemId = problemId,
                    AttemptNumber = attempt?.AttemptNumber ?? 0,
                    TestType = testType,
                    CreatedAt = now,
                    StartTime = metrics.StartTime ?? attempt?.StartedAt ?? now,
                    CodingTestId = attempt?.CodingTestId,
                    CodingTestAttemptId = codingTestAttemptId,
                    Source = "assessment"
                };
                _context.UserCodingActivityLogs.Add(active);
            }

            active.TestType = testType;
            active.SessionStatus = "Completed";
            active.SubmissionId = submissionId;
            if (codingTestQuestionAttemptId.HasValue)
                active.CodingTestQuestionAttemptId = codingTestQuestionAttemptId;
            active.TimeTakenSeconds = metrics.TimeTakenSeconds ?? active.TimeTakenSeconds;
            active.LanguageSwitchCount = metrics.LanguageSwitchCount ?? active.LanguageSwitchCount;
            active.EraseCount = metrics.EraseCount ?? active.EraseCount;
            active.SaveCount = metrics.SaveCount ?? active.SaveCount;
            active.RunClickCount = metrics.RunClickCount ?? active.RunClickCount;
            active.SubmitClickCount = metrics.SubmitClickCount ?? active.SubmitClickCount;
            active.LoginLogoutCount = metrics.LoginLogoutCount ?? active.LoginLogoutCount;
            active.IsSessionAbandoned = metrics.IsSessionAbandoned ?? active.IsSessionAbandoned;
            if (metrics.PassedTestCaseIDs != null) active.PassedTestCaseIDs = metrics.PassedTestCaseIDs;
            if (metrics.FailedTestCaseIDs != null) active.FailedTestCaseIDs = metrics.FailedTestCaseIDs;
            if (metrics.StartTime.HasValue) active.StartTime = metrics.StartTime.Value;
            active.EndTime = metrics.EndTime ?? now;
            active.UpdatedAt = now;

            await _context.SaveChangesAsync();
            return active;
        }

        public async Task<UserCodingActivityLog?> GetCurrentAssessmentSessionAsync(int userId, int problemId, int codingTestAttemptId)
        {
            await EnsureUserOwnsAttemptAsync(codingTestAttemptId, userId);

            return await _context.UserCodingActivityLogs
                .Where(l => l.UserId == userId
                         && l.ProblemId == problemId
                         && l.CodingTestAttemptId == codingTestAttemptId
                         && l.SessionStatus == "Active"
                         && !l.IsSessionAbandoned)
                .OrderByDescending(l => l.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<UserCodingActivityLog>> GetAttemptActivityLogsAsync(int codingTestAttemptId)
        {
            return await _context.UserCodingActivityLogs
                .Where(l => l.CodingTestAttemptId == codingTestAttemptId)
                .OrderBy(l => l.ProblemId)
                .ThenByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetAttemptActiveTimeSecondsAsync(int codingTestAttemptId)
        {
            return await _context.UserCodingActivityLogs
                .Where(l => l.CodingTestAttemptId == codingTestAttemptId && l.Source == "assessment")
                .SumAsync(l => l.TimeTakenSeconds);
        }

        public async Task<int> GetAttemptChainActiveTimeSecondsAsync(int codingTestAttemptId)
        {
            var attemptIds = new List<int>();
            var currentId = (int?)codingTestAttemptId;

            while (currentId.HasValue)
            {
                attemptIds.Add(currentId.Value);
                currentId = await _context.CodingTestAttempts
                    .Where(a => a.Id == currentId.Value)
                    .Select(a => a.ParentAttemptId)
                    .FirstOrDefaultAsync();
            }

            return await _context.UserCodingActivityLogs
                .Where(l => l.CodingTestAttemptId != null
                         && attemptIds.Contains(l.CodingTestAttemptId.Value)
                         && l.Source == "assessment")
                .SumAsync(l => l.TimeTakenSeconds);
        }

        private async Task ValidateProblemInAttemptAsync(int codingTestAttemptId, int problemId)
        {
            var resolved = await _questionPoolService.ResolveQuestionForAttemptAsync(codingTestAttemptId, problemId);
            if (resolved.IsAllowed)
                return;

            var inSnapshot = await _context.CodingTestAttemptQuestions
                .AnyAsync(q => q.CodingTestAttemptId == codingTestAttemptId && q.ProblemId == problemId);

            if (!inSnapshot)
            {
                var attempt = await _context.CodingTestAttempts
                    .Include(a => a.CodingTest)
                    .ThenInclude(t => t!.Questions)
                    .FirstOrDefaultAsync(a => a.Id == codingTestAttemptId);

                var inFixed = attempt?.CodingTest?.Questions.Any(q => q.ProblemId == problemId) == true;
                if (!inFixed)
                    throw new ArgumentException($"Problem {problemId} is not part of attempt {codingTestAttemptId}");
            }
        }

        public async Task<UserCodingActivityLog> LogUserActivityAsync(int userId, int problemId, int attemptNumber, string testType, int timeTakenSeconds)
        {
            var activityLog = new UserCodingActivityLog
            {
                UserId = userId,
                ProblemId = problemId,
                AttemptNumber = attemptNumber,
                TestType = testType,
                TimeTakenSeconds = timeTakenSeconds,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Source = "practice"
            };

            _context.UserCodingActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();
            return activityLog;
        }

        public async Task<CoreQuestionResult> CreateOrUpdateQuestionResultAsync(int userId, int problemId, int attemptNumber, string languageUsed, string finalCodeSnapshot, int totalTestCases, int passedTestCases, int failedTestCases)
        {
            var existingResult = await _context.CoreQuestionResults
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ProblemId == problemId && r.AttemptNumber == attemptNumber);

            if (existingResult != null)
            {
                existingResult.TotalTestCases = totalTestCases;
                existingResult.PassedTestCases = passedTestCases;
                existingResult.FailedTestCases = failedTestCases;
                existingResult.LanguageUsed = languageUsed;
                existingResult.FinalCodeSnapshot = finalCodeSnapshot;
                existingResult.LastSubmittedAt = DateTime.UtcNow;
            }
            else
            {
                existingResult = new CoreQuestionResult
                {
                    UserId = userId,
                    ProblemId = problemId,
                    AttemptNumber = attemptNumber,
                    TotalTestCases = totalTestCases,
                    PassedTestCases = passedTestCases,
                    FailedTestCases = failedTestCases,
                    LanguageUsed = languageUsed,
                    FinalCodeSnapshot = finalCodeSnapshot,
                    CreatedAt = DateTime.UtcNow,
                    LastSubmittedAt = DateTime.UtcNow
                };

                _context.CoreQuestionResults.Add(existingResult);
            }

            await _context.SaveChangesAsync();
            return existingResult;
        }

        public async Task<CoreTestCaseResult> LogTestCaseResultAsync(int coreQuestionResultId, int userId, int problemId, int testCaseId, bool isPassed, string userOutput, string expectedOutput, double executionTime)
        {
            // Check if a test case result already exists for this combination
            var existingResult = await _context.CoreTestCaseResults
                .FirstOrDefaultAsync(r => r.CoreQuestionResultId == coreQuestionResultId && r.TestCaseId == testCaseId);

            if (existingResult != null)
            {
                // Update existing result
                existingResult.IsPassed = isPassed;
                existingResult.UserOutput = userOutput;
                existingResult.ExpectedOutput = expectedOutput;
                existingResult.ExecutionTime = executionTime;
                await _context.SaveChangesAsync();
                return existingResult;
            }

            // Create new result
            var testCaseResult = new CoreTestCaseResult
            {
                CoreQuestionResultId = coreQuestionResultId,
                UserId = userId,
                ProblemId = problemId,
                TestCaseId = testCaseId,
                IsPassed = isPassed,
                UserOutput = userOutput,
                ExpectedOutput = expectedOutput,
                ExecutionTime = executionTime,
                CreatedAt = DateTime.UtcNow
            };

            _context.CoreTestCaseResults.Add(testCaseResult);
            await _context.SaveChangesAsync();
            return testCaseResult;
        }

        public async Task<List<UserCodingActivityLog>> GetUserActivityLogsAsync(int userId, int? problemId = null)
        {
            var query = _context.UserCodingActivityLogs.Where(log => log.UserId == userId);
            
            if (problemId.HasValue)
            {
                query = query.Where(log => log.ProblemId == problemId.Value);
            }

            return await query.OrderByDescending(log => log.CreatedAt).ToListAsync();
        }

        public async Task<PagedResult<UserCodingActivityLog>> GetUserActivityLogsAsync(int userId, int? problemId, int pageNumber, int pageSize)
        {
            var query = _context.UserCodingActivityLogs.Where(log => log.UserId == userId);
            
            if (problemId.HasValue)
            {
                query = query.Where(log => log.ProblemId == problemId.Value);
            }

            var totalCount = await query.CountAsync();
            
            var items = await query
                .OrderByDescending(log => log.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<UserCodingActivityLog>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<object> GetUserActivitySummaryAsync(int userId)
        {
            // Database-level aggregations
            var totalProblemsAttempted = await _context.UserCodingActivityLogs
                .Where(log => log.UserId == userId)
                .Select(log => log.ProblemId)
                .Distinct()
                .CountAsync();

            var totalTimeSpentSeconds = await _context.UserCodingActivityLogs
                .Where(log => log.UserId == userId)
                .SumAsync(log => log.TimeTakenSeconds);

            var timeSpentByTestTypeRaw = await _context.UserCodingActivityLogs
                .Where(log => log.UserId == userId)
                .GroupBy(log => log.TestType)
                .Select(g => new { TestType = g.Key ?? "Unknown", Seconds = g.Sum(log => log.TimeTakenSeconds) })
                .ToListAsync();

            var timeSpentByTestType = timeSpentByTestTypeRaw
                .ToDictionary(
                    x => x.TestType,
                    x => new { 
                        seconds = x.Seconds,
                        formatted = FormatDuration(x.Seconds)
                    }
                );

            // Fetch a limited number of recent attempts instead of all attempts
            var recentAttemptsLogs = await _context.UserCodingActivityLogs
                .Where(log => log.UserId == userId)
                .OrderByDescending(log => log.CreatedAt)
                .Take(50) // Adjust limit as needed
                .ToListAsync();

            var attempts = recentAttemptsLogs.Select(log => new {
                id = log.Id,
                problemId = log.ProblemId,
                attemptNumber = log.AttemptNumber,
                testType = log.TestType,
                startTime = log.StartTime,
                endTime = log.EndTime,
                timeSpentSeconds = log.TimeTakenSeconds,
                sessionDuration = FormatDuration(log.TimeTakenSeconds),
                testCases = new {
                    total = (string.IsNullOrEmpty(log.PassedTestCaseIDs) ? 0 : log.PassedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id))) +
                            (string.IsNullOrEmpty(log.FailedTestCaseIDs) ? 0 : log.FailedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id))),
                    passed = string.IsNullOrEmpty(log.PassedTestCaseIDs) ? 0 : log.PassedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id)),
                    failed = string.IsNullOrEmpty(log.FailedTestCaseIDs) ? 0 : log.FailedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id))
                }
            }).ToList();

            return new {
                userId = userId,
                totalProblemsAttempted = totalProblemsAttempted,
                totalTimeSpentSeconds = totalTimeSpentSeconds,
                totalTimeFormatted = FormatDuration(totalTimeSpentSeconds),
                timeSpentByTestType = timeSpentByTestType,
                attempts = attempts
            };
        }

        public async Task<object> GetProblemTimeAnalysisAsync(int userId, int problemId)
        {
            var logsQuery = _context.UserCodingActivityLogs
                .Where(log => log.UserId == userId && log.ProblemId == problemId);

            var attemptsCount = await logsQuery.CountAsync();

            if (attemptsCount == 0)
            {
                return new { 
                    message = "No activity logs found for this problem",
                    totalTimeSpent = 0,
                    averageTimePerAttempt = 0,
                    attempts = 0
                };
            }

            // Database aggregations
            var totalTimeSpentSeconds = await logsQuery.SumAsync(log => log.TimeTakenSeconds);
            var averageTimePerAttemptRaw = await logsQuery.AverageAsync(log => (double?)log.TimeTakenSeconds) ?? 0;
            var averageTimePerAttempt = (int)Math.Round(averageTimePerAttemptRaw);

            var latestAttempt = await logsQuery
                .OrderByDescending(log => log.CreatedAt)
                .FirstOrDefaultAsync();

            var allAttempts = await logsQuery
                .OrderByDescending(log => log.CreatedAt)
                .Select(log => new
                {
                    attemptNumber = log.AttemptNumber,
                    timeTaken = log.TimeTakenSeconds,
                    timeFormatted = FormatDuration(log.TimeTakenSeconds), // You'll need EF-compatible formatting or do it in-memory
                    testType = log.TestType,
                    createdAt = log.CreatedAt,
                    passedTestCases = log.PassedTestCaseIDs,
                    failedTestCases = log.FailedTestCaseIDs
                })
                .ToListAsync();
                
            var formattedAllAttempts = allAttempts.Select(a => new
                {
                    a.attemptNumber,
                    a.timeTaken,
                    timeFormatted = FormatDuration(a.timeTaken), // Format in memory
                    a.testType,
                    a.createdAt,
                    passedTestCases = string.IsNullOrEmpty(a.passedTestCases) ? 0 : a.passedTestCases.Split(',').Count(id => !string.IsNullOrWhiteSpace(id)),
                    failedTestCases = string.IsNullOrEmpty(a.failedTestCases) ? 0 : a.failedTestCases.Split(',').Count(id => !string.IsNullOrWhiteSpace(id))
                }).ToList();

            return new
            {
                totalTimeSpent = totalTimeSpentSeconds,
                totalTimeFormatted = FormatDuration(totalTimeSpentSeconds),
                averageTimePerAttempt = averageTimePerAttempt,
                averageTimeFormatted = FormatDuration(averageTimePerAttempt),
                attempts = attemptsCount,
                latestAttempt = latestAttempt != null ? new
                {
                    attemptNumber = latestAttempt.AttemptNumber,
                    timeTaken = latestAttempt.TimeTakenSeconds,
                    timeFormatted = FormatDuration(latestAttempt.TimeTakenSeconds),
                    testType = latestAttempt.TestType,
                    createdAt = latestAttempt.CreatedAt,
                    passedTestCases = string.IsNullOrEmpty(latestAttempt.PassedTestCaseIDs) ? 0 : latestAttempt.PassedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id)),
                    failedTestCases = string.IsNullOrEmpty(latestAttempt.FailedTestCaseIDs) ? 0 : latestAttempt.FailedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id))
                } : null,
                allAttempts = formattedAllAttempts
            };
        }

        // Helper Method to Format Duration (Copied from Controller context, or add if it doesn't exist)
        private static string FormatDuration(int seconds)
        {
            if (seconds < 60)
                return $"{seconds} second{(seconds == 1 ? "" : "s")}";
            else if (seconds < 3600)
            {
                var minutes = seconds / 60;
                var remainingSeconds = seconds % 60;
                return $"{minutes} minute{(minutes == 1 ? "" : "s")} {remainingSeconds} second{(remainingSeconds == 1 ? "" : "s")}";
            }
            else
            {
                var hours = seconds / 3600;
                var remainingMinutes = (seconds % 3600) / 60;
                return $"{hours} hour{(hours == 1 ? "" : "s")} {remainingMinutes} minute{(remainingMinutes == 1 ? "" : "s")}";
            }
        }

        public async Task<List<CoreQuestionResult>> GetUserQuestionResultsAsync(int userId, int? problemId = null)
        {
            var query = _context.CoreQuestionResults.Where(result => result.UserId == userId);
            
            if (problemId.HasValue)
            {
                query = query.Where(result => result.ProblemId == problemId.Value);
            }

            return await query.OrderByDescending(result => result.LastSubmittedAt).ToListAsync();
        }

        // FIXED METHOD - This was causing the EmptyProjectionMember error
        public async Task<List<CoreTestCaseResult>> GetTestCaseResultsAsync(int coreQuestionResultId)
        {
            return await _context.CoreTestCaseResults
                .Where(result => result.CoreQuestionResultId == coreQuestionResultId)
                .OrderBy(result => result.TestCaseId)
                .ThenByDescending(result => result.CreatedAt)
                .ToListAsync();
        }

        // ✅ Enhanced method to properly handle test case IDs
        public async Task UpdateActivityMetricsAsync(
            int activityLogId, 
            int? timeTakenSeconds = null, 
            int? languageSwitchCount = null, 
            int? eraseCount = null, 
            int? saveCount = null, 
            int? runClickCount = null, 
            int? submitClickCount = null, 
            int? loginLogoutCount = null, 
            bool? isSessionAbandoned = null, 
            string? passedTestCaseIDs = null, 
            string? failedTestCaseIDs = null, 
            DateTime? startTime = null, 
            DateTime? endTime = null)
        {
            var activityLog = await _context.UserCodingActivityLogs.FindAsync(activityLogId);
            if (activityLog == null) return;

            // ✅ Update all metrics
            if (timeTakenSeconds.HasValue) activityLog.TimeTakenSeconds = timeTakenSeconds.Value;
            if (languageSwitchCount.HasValue) activityLog.LanguageSwitchCount = languageSwitchCount.Value;
            if (eraseCount.HasValue) activityLog.EraseCount = eraseCount.Value;
            if (saveCount.HasValue) activityLog.SaveCount = saveCount.Value;
            if (runClickCount.HasValue) activityLog.RunClickCount = runClickCount.Value;
            if (submitClickCount.HasValue) activityLog.SubmitClickCount = submitClickCount.Value;
            if (loginLogoutCount.HasValue) activityLog.LoginLogoutCount = loginLogoutCount.Value;
            if (isSessionAbandoned.HasValue) activityLog.IsSessionAbandoned = isSessionAbandoned.Value;
            if (passedTestCaseIDs != null) activityLog.PassedTestCaseIDs = passedTestCaseIDs;
            if (failedTestCaseIDs != null) activityLog.FailedTestCaseIDs = failedTestCaseIDs;
            if (startTime.HasValue) activityLog.StartTime = startTime.Value;
            if (endTime.HasValue) activityLog.EndTime = endTime.Value;

            activityLog.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // Renamed: creates a new log row with full metrics (practice flows)
        public async Task<UserCodingActivityLog> CreateActivityLogWithMetricsAsync(int userId, int problemId, int attemptNumber, string testType, int timeTakenSeconds, int languageSwitchCount, int eraseCount, int saveCount, int runClickCount, int submitClickCount, int loginLogoutCount, bool isSessionAbandoned, string passedTestCaseIDs, string failedTestCaseIDs)
        {
            var log = new UserCodingActivityLog
            {
                UserId = userId,
                ProblemId = problemId,
                AttemptNumber = attemptNumber,
                TestType = testType,
                TimeTakenSeconds = timeTakenSeconds,
                LanguageSwitchCount = languageSwitchCount,
                EraseCount = eraseCount,
                SaveCount = saveCount,
                RunClickCount = runClickCount,
                SubmitClickCount = submitClickCount,
                LoginLogoutCount = loginLogoutCount,
                IsSessionAbandoned = isSessionAbandoned,
                PassedTestCaseIDs = passedTestCaseIDs,
                FailedTestCaseIDs = failedTestCaseIDs,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Source = "practice"
            };

            _context.UserCodingActivityLogs.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async Task<List<CoreQuestionResult>> GetAllQuestionResultsAsync()
        {
            return await _context.CoreQuestionResults
                .OrderByDescending(result => result.LastSubmittedAt)
                .ToListAsync();
        }

        public async Task<List<CoreTestCaseResult>> GetAllTestCaseResultsAsync()
        {
            return await _context.CoreTestCaseResults
                .OrderByDescending(result => result.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<CoreTestCaseResult>> GetUserTestCaseResultsAsync(int userId)
        {
            return await _context.CoreTestCaseResults
                .Where(result => result.UserId == userId)
                .OrderByDescending(result => result.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<CoreTestCaseResult>> GetUserTestCaseResultsForProblemAsync(int userId, int problemId)
        {
            return await _context.CoreTestCaseResults
                .Where(result => result.UserId == userId && result.ProblemId == problemId)
                .OrderByDescending(result => result.CreatedAt)
                .ToListAsync();
        }
    }
}