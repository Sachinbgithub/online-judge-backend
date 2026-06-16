using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Services
{
    public class AttemptActivityReviewService : IAttemptActivityReviewService
    {
        private readonly AppDbContext _context;
        private readonly IActivityTrackingService _activityTrackingService;
        private readonly IProctoringService _proctoringService;
        private readonly IIntegrityAnalysisService _integrityAnalysisService;

        public AttemptActivityReviewService(
            AppDbContext context,
            IActivityTrackingService activityTrackingService,
            IProctoringService proctoringService,
            IIntegrityAnalysisService integrityAnalysisService)
        {
            _context = context;
            _activityTrackingService = activityTrackingService;
            _proctoringService = proctoringService;
            _integrityAnalysisService = integrityAnalysisService;
        }

        public async Task<AttemptActivityReviewResponse> GetAttemptActivityReviewAsync(int codingTestAttemptId)
        {
            var attempt = await _context.CodingTestAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == codingTestAttemptId);

            if (attempt == null)
                throw new ArgumentException($"Attempt {codingTestAttemptId} not found");

            var activityLogs = await _activityTrackingService.GetAttemptActivityLogsAsync(codingTestAttemptId);
            var submissions = await _context.CodingTestSubmissions
                .AsNoTracking()
                .Where(s => s.CodingTestAttemptId == codingTestAttemptId && s.ProblemId != null)
                .OrderByDescending(s => s.SubmissionTime)
                .ToListAsync();

            var problemIds = activityLogs.Select(l => l.ProblemId)
                .Union(submissions.Where(s => s.ProblemId.HasValue).Select(s => s.ProblemId!.Value))
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            var problemSessions = problemIds.Select(problemId => new ProblemActivitySessionResponse
            {
                ProblemId = problemId,
                ActivityLogs = activityLogs
                    .Where(l => l.ProblemId == problemId)
                    .Select(IActivityTrackingService.MapToResponse)
                    .ToList(),
                Submissions = submissions
                    .Where(s => s.ProblemId == problemId)
                    .Select(MapSubmissionSummary)
                    .ToList()
            }).ToList();

            var proctoringSummary = await _proctoringService.GetStatusForAttemptAsync(codingTestAttemptId);
            var proctoringEvents = await _proctoringService.GetEventsForAttemptAsync(codingTestAttemptId);
            var flags = await _integrityAnalysisService.GetFlagsForAttemptAsync(
                codingTestAttemptId, attempt.UserId, isTestSetter: true);
            var snapshots = await _integrityAnalysisService.GetActivitySnapshotsForAttemptAsync(codingTestAttemptId);

            var aggregatedFromLogs = activityLogs
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Run = g.Sum(x => x.RunClickCount),
                    Submit = g.Sum(x => x.SubmitClickCount),
                    Erase = g.Sum(x => x.EraseCount),
                    Save = g.Sum(x => x.SaveCount),
                    Lang = g.Sum(x => x.LanguageSwitchCount),
                    Login = g.Sum(x => x.LoginLogoutCount),
                    Time = g.Sum(x => x.TimeTakenSeconds)
                })
                .FirstOrDefault();

            var aggregatedFromSubmissions = submissions
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Run = g.Sum(x => x.RunClickCount),
                    Submit = g.Sum(x => x.SubmitClickCount),
                    Erase = g.Sum(x => x.EraseCount),
                    Save = g.Sum(x => x.SaveCount),
                    Lang = g.Sum(x => x.LanguageSwitchCount),
                    Login = g.Sum(x => x.LoginLogoutCount)
                })
                .FirstOrDefault();

            return new AttemptActivityReviewResponse
            {
                CodingTestAttemptId = attempt.Id,
                CodingTestId = attempt.CodingTestId,
                UserId = attempt.UserId,
                IntegrityStatus = attempt.IntegrityStatus,
                AttemptStatus = attempt.Status,
                StartedAt = attempt.StartedAt,
                SubmittedAt = attempt.SubmittedAt,
                TotalScore = attempt.TotalScore,
                MaxScore = attempt.MaxScore,
                Percentage = attempt.Percentage,
                ProctoringSummary = proctoringSummary,
                ProctoringEvents = proctoringEvents,
                IntegrityFlags = flags,
                CodeActivitySnapshots = snapshots,
                ProblemSessions = problemSessions,
                AggregatedMetrics = new AttemptAggregatedMetricsResponse
                {
                    TotalRunClicks = Math.Max(aggregatedFromLogs?.Run ?? 0, aggregatedFromSubmissions?.Run ?? 0),
                    TotalSubmitClicks = Math.Max(aggregatedFromLogs?.Submit ?? 0, aggregatedFromSubmissions?.Submit ?? 0),
                    TotalEraseCount = Math.Max(aggregatedFromLogs?.Erase ?? 0, aggregatedFromSubmissions?.Erase ?? 0),
                    TotalSaveCount = Math.Max(aggregatedFromLogs?.Save ?? 0, aggregatedFromSubmissions?.Save ?? 0),
                    TotalLanguageSwitches = Math.Max(aggregatedFromLogs?.Lang ?? 0, aggregatedFromSubmissions?.Lang ?? 0),
                    TotalLoginLogoutCount = Math.Max(aggregatedFromLogs?.Login ?? 0, aggregatedFromSubmissions?.Login ?? 0),
                    TotalTimeSpentSeconds = aggregatedFromLogs?.Time ?? (int)(attempt.SubmittedAt.HasValue
                        ? (attempt.SubmittedAt.Value - attempt.StartedAt).TotalSeconds
                        : (DateTime.UtcNow - attempt.StartedAt).TotalSeconds)
                }
            };
        }

        private static CodingTestSubmissionSummaryResponse MapSubmissionSummary(CodingTestSubmission s) => new()
        {
            SubmissionId = s.SubmissionId,
            CodingTestId = s.CodingTestId,
            ProblemId = s.ProblemId ?? 0,
            UserId = (int)s.UserId,
            AttemptNumber = s.AttemptNumber,
            LanguageUsed = s.LanguageUsed,
            TotalTestCases = s.TotalTestCases,
            PassedTestCases = s.PassedTestCases,
            FailedTestCases = s.FailedTestCases,
            Score = s.Score,
            MaxScore = s.MaxScore,
            IsCorrect = s.IsCorrect,
            IsLateSubmission = s.IsLateSubmission,
            SubmissionTime = s.SubmissionTime,
            ExecutionTimeMs = s.ExecutionTimeMs,
            MemoryUsedKB = s.MemoryUsedKB,
            ErrorMessage = s.ErrorMessage,
            ErrorType = s.ErrorType,
            LanguageSwitchCount = s.LanguageSwitchCount,
            RunClickCount = s.RunClickCount,
            SubmitClickCount = s.SubmitClickCount,
            EraseCount = s.EraseCount,
            SaveCount = s.SaveCount,
            LoginLogoutCount = s.LoginLogoutCount,
            IsSessionAbandoned = s.IsSessionAbandoned,
            ClassId = s.ClassId,
            CreatedAt = s.CreatedAt
        };
    }
}
