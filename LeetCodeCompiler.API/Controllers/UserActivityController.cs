using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Services;
using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserActivityController : ControllerBase
    {
        private readonly IActivityTrackingService _activityTrackingService;

        public UserActivityController(IActivityTrackingService activityTrackingService)
        {
            _activityTrackingService = activityTrackingService;
        }

        [HttpGet("{userId}/problem/{problemId}")]
        public async Task<IActionResult> GetUserActivityForProblem(int userId, int problemId)
        {
            try
            {
                var activityLogs = await _activityTrackingService.GetUserActivityLogsAsync(userId, problemId);
                
                // Format response for user-friendly display
                var formattedLogs = activityLogs.Select(log => {
                    var sessionDuration = FormatDuration(log.TimeTakenSeconds);
                    var passedCount = string.IsNullOrEmpty(log.PassedTestCaseIDs) ? 0 : log.PassedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id));
                    var failedCount = string.IsNullOrEmpty(log.FailedTestCaseIDs) ? 0 : log.FailedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id));
                    var totalCount = passedCount + failedCount;
                    
                    return new
                    {
                        id = log.Id,
                        userId = log.UserId,
                        problemId = log.ProblemId,
                        attemptNumber = log.AttemptNumber,
                        testType = log.TestType,
                        sessionDuration = sessionDuration,
                        timeTakenSeconds = log.TimeTakenSeconds,
                        languageSwitchCount = log.LanguageSwitchCount,
                        eraseCount = log.EraseCount,
                        saveCount = log.SaveCount,
                        runClickCount = log.RunClickCount,
                        submitClickCount = log.SubmitClickCount,
                        loginLogoutCount = log.LoginLogoutCount,
                        isSessionAbandoned = log.IsSessionAbandoned,
                        passedTestCaseIds = log.PassedTestCaseIDs,
                        failedTestCaseIds = log.FailedTestCaseIDs,
                        totalTestCases = totalCount,
                        passedTestCases = passedCount,
                        failedTestCases = failedCount,
                        createdAt = log.CreatedAt,
                        updatedAt = log.UpdatedAt,
                        startTime = log.StartTime,
                        endTime = log.EndTime,
                        timeAgo = GetTimeAgo(log.CreatedAt)
                    };
                }).ToList();

                return Ok(formattedLogs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve user activity for problem", details = ex.Message });
            }
        }

        [HttpGet("{userId}/summary")]
        public async Task<IActionResult> GetUserActivitySummary(int userId)
        {
            var logs = await _activityTrackingService.GetUserActivityLogsAsync(userId);

            var attempts = logs.Select(log => new {
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

            // Calculate total time spent across ALL problems (entire environment)
            var totalTimeSpentSeconds = logs.Sum(log => log.TimeTakenSeconds);
            var totalTimeFormatted = FormatDuration(totalTimeSpentSeconds);

            // Calculate time spent by test type
            var timeSpentByTestType = logs
                .GroupBy(log => log.TestType)
                .ToDictionary(
                    g => g.Key,
                    g => new { 
                        seconds = g.Sum(log => log.TimeTakenSeconds),
                        formatted = FormatDuration(g.Sum(log => log.TimeTakenSeconds))
                    }
                );

            var response = new {
                userId = userId,
                totalProblemsAttempted = logs.Select(log => log.ProblemId).Distinct().Count(),
                totalTimeSpentSeconds = totalTimeSpentSeconds,
                totalTimeFormatted = totalTimeFormatted,
                timeSpentByTestType = timeSpentByTestType,
                attempts = attempts
            };

            return Ok(response);
        }

        [HttpPost("log")]
        public async Task<IActionResult> LogUserActivity([FromBody] LogUserActivityRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var activityLog = await _activityTrackingService.LogUserActivityAsync(
                    request.UserId,
                    request.ProblemId,
                    request.AttemptNumber,
                    request.TestType,
                    request.TimeTakenSeconds
                );

                return CreatedAtAction(nameof(GetUserActivityForProblem), new { userId = request.UserId, problemId = request.ProblemId }, activityLog);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to log user activity", details = ex.Message });
            }
        }

        [HttpPut("{activityLogId}/metrics")]
        public async Task<IActionResult> UpdateActivityMetrics(int activityLogId, [FromBody] UpdateActivityMetricsRequest request)
        {
            try
            {
                await _activityTrackingService.UpdateActivityMetricsAsync(
                    activityLogId,
                    timeTakenSeconds: null, // Add this parameter
                    languageSwitchCount: request.LanguageSwitchCount,
                    eraseCount: request.EraseCount,
                    saveCount: request.SaveCount,
                    runClickCount: request.RunClickCount,
                    submitClickCount: request.SubmitClickCount,
                    loginLogoutCount: request.LoginLogoutCount,
                    isSessionAbandoned: request.IsSessionAbandoned,
                    passedTestCaseIDs: request.PassedTestCaseIDs,
                    failedTestCaseIDs: request.FailedTestCaseIDs,
                    startTime: request.StartTime,
                    endTime: request.EndTime
                );

                return Ok(new { message = "Activity metrics updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update activity metrics", details = ex.Message });
            }
        }

        [HttpGet("{userId}/problem/{problemId}/time-analysis")]
        public async Task<IActionResult> GetProblemTimeAnalysis(int userId, int problemId)
        {
            try
            {
                var activityLogs = await _activityTrackingService.GetUserActivityLogsAsync(userId, problemId);
                
                if (!activityLogs.Any())
                {
                    return Ok(new { 
                        message = "No activity logs found for this problem",
                        totalTimeSpent = 0,
                        averageTimePerAttempt = 0,
                        attempts = 0
                    });
                }

                // Calculate time metrics
                var totalTimeSpentSeconds = activityLogs.Sum(log => log.TimeTakenSeconds);
                var averageTimePerAttempt = activityLogs.Average(log => log.TimeTakenSeconds);
                var attempts = activityLogs.Count;
                
                // Get the most recent attempt
                var latestAttempt = activityLogs.OrderByDescending(log => log.CreatedAt).First();
                
                // Format durations
                var totalTimeFormatted = FormatDuration(totalTimeSpentSeconds);
                var averageTimeFormatted = FormatDuration((int)averageTimePerAttempt);
                var latestTimeFormatted = FormatDuration(latestAttempt.TimeTakenSeconds);

                return Ok(new
                {
                    totalTimeSpent = totalTimeSpentSeconds,
                    totalTimeFormatted = totalTimeFormatted,
                    averageTimePerAttempt = (int)averageTimePerAttempt,
                    averageTimeFormatted = averageTimeFormatted,
                    attempts = attempts,
                    latestAttempt = new
                    {
                        attemptNumber = latestAttempt.AttemptNumber,
                        timeTaken = latestAttempt.TimeTakenSeconds,
                        timeFormatted = latestTimeFormatted,
                        testType = latestAttempt.TestType,
                        createdAt = latestAttempt.CreatedAt,
                        passedTestCases = string.IsNullOrEmpty(latestAttempt.PassedTestCaseIDs) ? 0 : latestAttempt.PassedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id)),
                        failedTestCases = string.IsNullOrEmpty(latestAttempt.FailedTestCaseIDs) ? 0 : latestAttempt.FailedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id))
                    },
                    allAttempts = activityLogs.Select(log => new
                    {
                        attemptNumber = log.AttemptNumber,
                        timeTaken = log.TimeTakenSeconds,
                        timeFormatted = FormatDuration(log.TimeTakenSeconds),
                        testType = log.TestType,
                        createdAt = log.CreatedAt,
                        passedTestCases = string.IsNullOrEmpty(log.PassedTestCaseIDs) ? 0 : log.PassedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id)),
                        failedTestCases = string.IsNullOrEmpty(log.FailedTestCaseIDs) ? 0 : log.FailedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id))
                    }).OrderByDescending(a => a.createdAt).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve time analysis", details = ex.Message });
            }
        }

        [HttpGet("{userId}/problem/{problemId}/current-session")]
        public async Task<IActionResult> GetCurrentSessionActivity(int userId, int problemId)
        {
            try
            {
                var activityLogs = await _activityTrackingService.GetUserActivityLogsAsync(userId, problemId);
                
                // Get only the most recent session for this problem
                var currentSession = activityLogs
                    .OrderByDescending(log => log.CreatedAt)
                    .FirstOrDefault();
                
                if (currentSession == null)
                {
                    return Ok(new { 
                        message = "No current session found for this problem",
                        hasSession = false
                    });
                }

                // Calculate session duration
                var sessionDuration = FormatDuration(currentSession.TimeTakenSeconds);
                
                return Ok(new
                {
                    hasSession = true,
                    sessionData = new
                    {
                        id = currentSession.Id,
                        userId = currentSession.UserId,
                        problemId = currentSession.ProblemId,
                        attemptNumber = currentSession.AttemptNumber,
                        testType = currentSession.TestType,
                        timeTakenSeconds = currentSession.TimeTakenSeconds,
                        timeFormatted = sessionDuration,
                        languageSwitchCount = currentSession.LanguageSwitchCount,
                        startTime = currentSession.StartTime,
                        endTime = currentSession.EndTime,
                        createdAt = currentSession.CreatedAt,
                        // Test case results
                        passedTestCases = string.IsNullOrEmpty(currentSession.PassedTestCaseIDs) ? 0 : currentSession.PassedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id)),
                        failedTestCases = string.IsNullOrEmpty(currentSession.FailedTestCaseIDs) ? 0 : currentSession.FailedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id)),
                        totalTestCases = (string.IsNullOrEmpty(currentSession.PassedTestCaseIDs) ? 0 : currentSession.PassedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id))) +
                                        (string.IsNullOrEmpty(currentSession.FailedTestCaseIDs) ? 0 : currentSession.FailedTestCaseIDs.Split(',').Count(id => !string.IsNullOrWhiteSpace(id)))
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve current session", details = ex.Message });
            }
        }

        [HttpGet("debug/all")]
        public async Task<IActionResult> GetAllActivityLogs()
        {
            try
            {
                var allLogs = await _activityTrackingService.GetUserActivityLogsAsync(1); // Get all logs for user 1
                return Ok(new { 
                    totalLogs = allLogs.Count,
                    logs = allLogs.Select(log => new {
                        id = log.Id,
                        userId = log.UserId,
                        problemId = log.ProblemId,
                        testType = log.TestType,
                        timeTakenSeconds = log.TimeTakenSeconds,
                        createdAt = log.CreatedAt,
                        startTime = log.StartTime,
                        endTime = log.EndTime
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to get all activity logs", details = ex.Message });
            }
        }

        private string FormatDuration(int seconds)
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

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;
            
            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            else if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes == 1 ? "" : "s")} ago";
            else if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours == 1 ? "" : "s")} ago";
            else
                return $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays == 1 ? "" : "s")} ago";
        }
    }

    public class LogUserActivityRequest
    {
        public int UserId { get; set; }
        public int ProblemId { get; set; }
        public int AttemptNumber { get; set; }
        public string TestType { get; set; } = "";
        public int TimeTakenSeconds { get; set; }
    }

    public class UpdateActivityMetricsRequest
    {
        public int? LanguageSwitchCount { get; set; }
        public int? EraseCount { get; set; }
        public int? SaveCount { get; set; }
        public int? RunClickCount { get; set; }
        public int? SubmitClickCount { get; set; }
        public int? LoginLogoutCount { get; set; }
        public bool? IsSessionAbandoned { get; set; }
        public string? PassedTestCaseIDs { get; set; }
        public string? FailedTestCaseIDs { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
} 