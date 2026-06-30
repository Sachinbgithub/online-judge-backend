using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Services
{
    public class IntegrityAnalysisService : IIntegrityAnalysisService
    {
        public const string FlagTypeAutoDq = "AutoDQ";
        public const string FlagTypeBreachWarning = "BreachWarning";
        public const string FlagTypeBreachFlagged = "BreachFlagged";
        public const string FlagTypeNetworkLoss = "NetworkLoss";

        private readonly AppDbContext _context;
        private readonly IActivityTrackingService _activityTrackingService;

        public IntegrityAnalysisService(AppDbContext context, IActivityTrackingService activityTrackingService)
        {
            _context = context;
            _activityTrackingService = activityTrackingService;
        }

        public async Task SaveActivitySnapshotAsync(CodeActivitySnapshotRequest request)
        {
            var attempt = await _context.CodingTestAttempts.FindAsync(request.CodingTestAttemptId);
            if (attempt == null)
                throw new ArgumentException($"Attempt {request.CodingTestAttemptId} not found");
            if (attempt.UserId != request.UserId)
                throw new UnauthorizedAccessException("You do not own this attempt.");

            _context.CodeActivitySnapshots.Add(new CodeActivitySnapshot
            {
                CodingTestAttemptId = request.CodingTestAttemptId,
                ProblemId = request.ProblemId,
                Timestamp = request.Timestamp,
                CodeLength = request.CodeLength,
                DeltaChars = request.DeltaChars,
                Source = request.Source,
                PasteLength = request.PasteLength,
                CodeHash = request.CodeHash
            });

            await _context.SaveChangesAsync();
        }

        public async Task<List<IntegrityFlagResponse>> RunSubmitHeuristicsAsync(
            int codingTestAttemptId,
            long? submissionId,
            int problemId,
            string finalCode,
            bool allTestsPassed,
            DateTime attemptStartedAt)
        {
            var newFlags = new List<IntegrityFlag>();
            var snapshots = await _context.CodeActivitySnapshots
                .Where(s => s.CodingTestAttemptId == codingTestAttemptId
                         && (s.ProblemId == null || s.ProblemId == problemId))
                .OrderBy(s => s.Timestamp)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var sessionSeconds = (now - attemptStartedAt).TotalSeconds;

            var maxPaste = snapshots
                .Where(s => s.PasteLength.HasValue)
                .Select(s => s.PasteLength!.Value)
                .DefaultIfEmpty(0)
                .Max();

            if (maxPaste > 200)
            {
                newFlags.Add(CreateFlag(codingTestAttemptId, submissionId, "LargePaste", "Medium",
                    $"{{\"maxPasteLength\":{maxPaste}}}"));
            }

            var suddenInsert = snapshots.Any(s =>
                s.DeltaChars >= 500
                && s.Source.Equals("paste", StringComparison.OrdinalIgnoreCase)
                && !snapshots.Any(prev =>
                    prev.Timestamp >= s.Timestamp.AddSeconds(-3)
                    && prev.Timestamp < s.Timestamp
                    && prev.Source.Equals("keystroke", StringComparison.OrdinalIgnoreCase)));

            if (suddenInsert)
            {
                newFlags.Add(CreateFlag(codingTestAttemptId, submissionId, "SuddenInsert", "High",
                    "{\"reason\":\"Large paste without prior keystrokes\"}"));
            }

            var totalPasteChars = snapshots
                .Where(s => s.PasteLength.HasValue)
                .Sum(s => s.PasteLength!.Value);
            var totalCodeLength = finalCode.Length;

            if (allTestsPassed && sessionSeconds < 60 && totalCodeLength > 0
                && (double)totalPasteChars / totalCodeLength > 0.5)
            {
                newFlags.Add(CreateFlag(codingTestAttemptId, submissionId, "SuspiciousTiming", "High",
                    $"{{\"sessionSeconds\":{sessionSeconds:F0},\"pasteRatio\":{(double)totalPasteChars / totalCodeLength:F2}}}"));
            }

            var idleThreshold = TimeSpan.FromMinutes(10);
            var lastKeystroke = snapshots
                .Where(s => s.Source.Equals("keystroke", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.Timestamp)
                .FirstOrDefault();

            var largePasteAfterIdle = snapshots.Any(s =>
                s.PasteLength >= 100
                && lastKeystroke != null
                && s.Timestamp - lastKeystroke.Timestamp > idleThreshold);

            if (largePasteAfterIdle)
            {
                newFlags.Add(CreateFlag(codingTestAttemptId, submissionId, "IdleAnomaly", "Medium",
                    "{\"reason\":\"Large paste after extended idle period\"}"));
            }

            if (newFlags.Count > 0)
            {
                _context.IntegrityFlags.AddRange(newFlags);
                await _context.SaveChangesAsync();
            }

            return newFlags.Select(MapFlag).ToList();
        }

        public async Task<List<IntegrityFlagResponse>> GetFlagsForAttemptAsync(
            int attemptId, int? requestingUserId, bool isTestSetter)
        {
            var attempt = await _context.CodingTestAttempts.FindAsync(attemptId);
            if (attempt == null)
                throw new ArgumentException($"Attempt {attemptId} not found");

            if (!isTestSetter && attempt.UserId != requestingUserId)
                throw new UnauthorizedAccessException("Access denied.");

            var flags = await _context.IntegrityFlags
                .Where(f => f.CodingTestAttemptId == attemptId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return flags.Select(MapFlag).ToList();
        }

        public async Task<List<IntegrityReviewSummaryResponse>> GetReviewSummaryAsync(int codingTestId)
        {
            var attempts = await _context.CodingTestAttempts
                .Where(a => a.CodingTestId == codingTestId
                         && (a.IntegrityStatus == "Flagged"
                          || a.IntegrityStatus == "Warning"
                          || a.IntegrityStatus == "Disqualified"))
                .ToListAsync();

            var result = new List<IntegrityReviewSummaryResponse>();

            foreach (var attempt in attempts)
            {
                var flags = await _context.IntegrityFlags
                    .Where(f => f.CodingTestAttemptId == attempt.Id)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                result.Add(new IntegrityReviewSummaryResponse
                {
                    CodingTestAttemptId = attempt.Id,
                    UserId = attempt.UserId,
                    IntegrityStatus = attempt.IntegrityStatus,
                    FlagCount = flags.Count,
                    Flags = flags.Select(MapFlag).ToList()
                });
            }

            return result;
        }

        public async Task<IntegrityFlagResponse?> ReviewFlagAsync(long flagId, ReviewIntegrityFlagRequest request, int reviewedBy)
        {
            var flag = await _context.IntegrityFlags.FindAsync(flagId);
            if (flag == null) return null;

            var allowed = new[] { "Dismissed", "Confirmed" };
            if (!allowed.Contains(request.ReviewStatus, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("ReviewStatus must be Dismissed or Confirmed");

            flag.ReviewStatus = request.ReviewStatus;
            flag.ReviewedBy = reviewedBy;
            flag.ReviewedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapFlag(flag);
        }

        public async Task<GrantResumeResponse> GrantResumeAsync(int codingTestAttemptId, int grantedByUserId)
        {
            var attempt = await _context.CodingTestAttempts
                .Include(a => a.CodingTest)
                .FirstOrDefaultAsync(a => a.Id == codingTestAttemptId);

            if (attempt == null)
                throw new ArgumentException($"Attempt {codingTestAttemptId} not found");

            if (attempt.Status is not ("Submitted" or "Completed" or "Abandoned"))
                throw new InvalidOperationException("Resume can only be granted for submitted or abandoned attempts");

            var canGrant = attempt.Status == "Abandoned"
                        || attempt.IntegrityStatus == "Disqualified"
                        || attempt.SubmissionReason == SubmissionReasons.NetworkLoss;

            if (!canGrant)
                throw new InvalidOperationException("Resume is only allowed after disqualification, network-loss submission, or abandoned session");

            var now = DateTime.UtcNow;
            if (now > attempt.CodingTest!.EndDate)
                throw new InvalidOperationException("Test window has ended");

            var existingPending = await _context.CodingTestResumeGrants
                .AnyAsync(g => g.CodingTestId == attempt.CodingTestId
                            && g.UserId == attempt.UserId
                            && g.Status == ResumeGrantStatuses.Pending
                            && g.AllowedEndAt > now);

            if (existingPending)
                throw new InvalidOperationException("A pending resume grant already exists for this student");

            var activeSeconds = await _activityTrackingService.GetAttemptChainActiveTimeSecondsAsync(codingTestAttemptId);
            var allowedEndAt = AttemptTimeBudgetService.ComputeAllowedEndAt(attempt.CodingTest, now, activeSeconds);

            if (allowedEndAt <= now)
                throw new InvalidOperationException("No remaining time in test window for resume");

            var grant = new CodingTestResumeGrant
            {
                CodingTestId = attempt.CodingTestId,
                UserId = attempt.UserId,
                PriorAttemptId = attempt.Id,
                GrantedByUserId = grantedByUserId,
                GrantedAt = now,
                ActiveTimeSecondsAtGrant = activeSeconds,
                AllowedEndAt = allowedEndAt,
                Status = ResumeGrantStatuses.Pending
            };

            _context.CodingTestResumeGrants.Add(grant);
            await _context.SaveChangesAsync();

            return new GrantResumeResponse
            {
                GrantId = grant.Id,
                CodingTestId = grant.CodingTestId,
                UserId = grant.UserId,
                PriorAttemptId = grant.PriorAttemptId,
                AllowedEndAt = grant.AllowedEndAt,
                AllowedMinutes = Math.Max(1, (int)Math.Ceiling((grant.AllowedEndAt - now).TotalMinutes)),
                ActiveTimeSecondsAtGrant = grant.ActiveTimeSecondsAtGrant
            };
        }

        public async Task<List<CodeActivitySnapshotResponse>> GetActivitySnapshotsForAttemptAsync(int codingTestAttemptId)
        {
            return await _context.CodeActivitySnapshots
                .Where(s => s.CodingTestAttemptId == codingTestAttemptId)
                .OrderBy(s => s.Timestamp)
                .Select(s => new CodeActivitySnapshotResponse
                {
                    Id = s.Id,
                    CodingTestAttemptId = s.CodingTestAttemptId,
                    ProblemId = s.ProblemId,
                    Timestamp = s.Timestamp,
                    CodeLength = s.CodeLength,
                    DeltaChars = s.DeltaChars,
                    Source = s.Source,
                    PasteLength = s.PasteLength,
                    CodeHash = s.CodeHash
                })
                .ToListAsync();
        }

        private static IntegrityFlag CreateFlag(
            int attemptId, long? submissionId, string type, string severity, string details)
        {
            return new IntegrityFlag
            {
                CodingTestAttemptId = attemptId,
                SubmissionId = submissionId,
                FlagType = type,
                Severity = severity,
                DetailsJson = details,
                CreatedAt = DateTime.UtcNow,
                ReviewStatus = "Pending"
            };
        }

        private static IntegrityFlagResponse MapFlag(IntegrityFlag f) => new()
        {
            Id = f.Id,
            CodingTestAttemptId = f.CodingTestAttemptId,
            SubmissionId = f.SubmissionId,
            FlagType = f.FlagType,
            Severity = f.Severity,
            DetailsJson = f.DetailsJson,
            CreatedAt = f.CreatedAt,
            ReviewStatus = f.ReviewStatus
        };
    }
}
