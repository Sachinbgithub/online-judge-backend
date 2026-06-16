using LeetCodeCompiler.API.Models;
using LeetCodeCompiler.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AnyAuthenticated")]
    public class UserActivityController : ControllerBase
    {
        private readonly IActivityTrackingService _activityTrackingService;

        public UserActivityController(IActivityTrackingService activityTrackingService)
        {
            _activityTrackingService = activityTrackingService;
        }

        [HttpGet("{userId}/problem/{problemId}")]
        public async Task<IActionResult> GetUserActivityForProblem(int userId, int problemId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var pagedLogs = await _activityTrackingService.GetUserActivityLogsAsync(userId, problemId, pageNumber, pageSize);

                var formattedItems = pagedLogs.Items.Select(log =>
                {
                    var sessionDuration = FormatDuration(log.TimeTakenSeconds);
                    var passedCount = CountIds(log.PassedTestCaseIDs);
                    var failedCount = CountIds(log.FailedTestCaseIDs);

                    return new
                    {
                        id = log.Id,
                        userId = log.UserId,
                        problemId = log.ProblemId,
                        attemptNumber = log.AttemptNumber,
                        testType = log.TestType,
                        sessionDuration,
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
                        totalTestCases = passedCount + failedCount,
                        passedTestCases = passedCount,
                        failedTestCases = failedCount,
                        codingTestId = log.CodingTestId,
                        codingTestAttemptId = log.CodingTestAttemptId,
                        codingTestQuestionAttemptId = log.CodingTestQuestionAttemptId,
                        submissionId = log.SubmissionId,
                        sessionStatus = log.SessionStatus,
                        source = log.Source,
                        createdAt = log.CreatedAt,
                        updatedAt = log.UpdatedAt,
                        startTime = log.StartTime,
                        endTime = log.EndTime,
                        timeAgo = GetTimeAgo(log.CreatedAt)
                    };
                }).ToList();

                return Ok(new
                {
                    items = formattedItems,
                    totalCount = pagedLogs.TotalCount,
                    pageNumber = pagedLogs.PageNumber,
                    pageSize = pagedLogs.PageSize,
                    totalPages = pagedLogs.TotalPages,
                    hasPreviousPage = pagedLogs.HasPreviousPage,
                    hasNextPage = pagedLogs.HasNextPage
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve user activity for problem", details = ex.Message });
            }
        }

        [HttpGet("{userId}/summary")]
        public async Task<IActionResult> GetUserActivitySummary(int userId)
        {
            try
            {
                var summary = await _activityTrackingService.GetUserActivitySummaryAsync(userId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve user activity summary", details = ex.Message });
            }
        }

        [HttpPost("log")]
        public async Task<IActionResult> LogUserActivity([FromBody] LogUserActivityRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var activityLog = await _activityTrackingService.LogUserActivityAsync(request);
                return CreatedAtAction(nameof(GetUserActivityForProblem),
                    new { userId = request.UserId, problemId = request.ProblemId },
                    IActivityTrackingService.MapToResponse(activityLog));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to log user activity", details = ex.Message });
            }
        }

        [HttpPut("{activityLogId}/metrics")]
        public async Task<IActionResult> UpdateActivityMetrics(int activityLogId, [FromBody] UpdateActivityMetricsRequest request, [FromQuery] int userId)
        {
            try
            {
                await _activityTrackingService.UpdateAssessmentMetricsAsync(activityLogId, userId, new AssessmentActivityMetrics
                {
                    TimeTakenSeconds = request.TimeTakenSeconds,
                    LanguageSwitchCount = request.LanguageSwitchCount,
                    EraseCount = request.EraseCount,
                    SaveCount = request.SaveCount,
                    RunClickCount = request.RunClickCount,
                    SubmitClickCount = request.SubmitClickCount,
                    LoginLogoutCount = request.LoginLogoutCount,
                    IsSessionAbandoned = request.IsSessionAbandoned,
                    PassedTestCaseIDs = request.PassedTestCaseIDs,
                    FailedTestCaseIDs = request.FailedTestCaseIDs,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime
                });

                return Ok(new { message = "Activity metrics updated successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
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
                var analysis = await _activityTrackingService.GetProblemTimeAnalysisAsync(userId, problemId);
                return Ok(analysis);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve time analysis", details = ex.Message });
            }
        }

        [HttpGet("{userId}/problem/{problemId}/current-session")]
        public async Task<IActionResult> GetCurrentSessionActivity(int userId, int problemId, [FromQuery] int? codingTestAttemptId = null)
        {
            try
            {
                if (codingTestAttemptId.HasValue)
                {
                    var currentSession = await _activityTrackingService.GetCurrentAssessmentSessionAsync(
                        userId, problemId, codingTestAttemptId.Value);

                    if (currentSession == null)
                    {
                        return Ok(new
                        {
                            message = "No active assessment session found for this problem",
                            hasSession = false
                        });
                    }

                    return Ok(new
                    {
                        hasSession = true,
                        sessionData = BuildSessionData(currentSession)
                    });
                }

                var activityLogs = await _activityTrackingService.GetUserActivityLogsAsync(userId, problemId);
                var latest = activityLogs
                    .Where(log => log.SessionStatus == "Active" && !log.IsSessionAbandoned)
                    .OrderByDescending(log => log.CreatedAt)
                    .FirstOrDefault()
                    ?? activityLogs.OrderByDescending(log => log.CreatedAt).FirstOrDefault();

                if (latest == null)
                {
                    return Ok(new
                    {
                        message = "No current session found for this problem",
                        hasSession = false
                    });
                }

                return Ok(new
                {
                    hasSession = latest.SessionStatus == "Active" && !latest.IsSessionAbandoned,
                    sessionData = BuildSessionData(latest)
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve current session", details = ex.Message });
            }
        }

        private object BuildSessionData(UserCodingActivityLog currentSession)
        {
            var sessionDuration = FormatDuration(currentSession.TimeTakenSeconds);
            var passed = CountIds(currentSession.PassedTestCaseIDs);
            var failed = CountIds(currentSession.FailedTestCaseIDs);

            return new
            {
                id = currentSession.Id,
                userId = currentSession.UserId,
                problemId = currentSession.ProblemId,
                attemptNumber = currentSession.AttemptNumber,
                testType = currentSession.TestType,
                timeTakenSeconds = currentSession.TimeTakenSeconds,
                timeFormatted = sessionDuration,
                languageSwitchCount = currentSession.LanguageSwitchCount,
                eraseCount = currentSession.EraseCount,
                saveCount = currentSession.SaveCount,
                runClickCount = currentSession.RunClickCount,
                submitClickCount = currentSession.SubmitClickCount,
                codingTestAttemptId = currentSession.CodingTestAttemptId,
                sessionStatus = currentSession.SessionStatus,
                source = currentSession.Source,
                startTime = currentSession.StartTime,
                endTime = currentSession.EndTime,
                createdAt = currentSession.CreatedAt,
                passedTestCases = passed,
                failedTestCases = failed,
                totalTestCases = passed + failed
            };
        }

        private static int CountIds(string? ids) =>
            string.IsNullOrEmpty(ids) ? 0 : ids.Split(',').Count(id => !string.IsNullOrWhiteSpace(id));

        private string FormatDuration(int seconds)
        {
            if (seconds < 60)
                return $"{seconds} second{(seconds == 1 ? "" : "s")}";
            if (seconds < 3600)
            {
                var minutes = seconds / 60;
                var remainingSeconds = seconds % 60;
                return $"{minutes} minute{(minutes == 1 ? "" : "s")} {remainingSeconds} second{(remainingSeconds == 1 ? "" : "s")}";
            }

            var hours = seconds / 3600;
            var remainingMinutes = (seconds % 3600) / 60;
            return $"{hours} hour{(hours == 1 ? "" : "s")} {remainingMinutes} minute{(remainingMinutes == 1 ? "" : "s")}";
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes == 1 ? "" : "s")} ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours == 1 ? "" : "s")} ago";
            return $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays == 1 ? "" : "s")} ago";
        }
    }
}
