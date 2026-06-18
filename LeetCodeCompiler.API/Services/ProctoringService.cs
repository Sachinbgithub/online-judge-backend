using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Services
{
    public class ProctoringService : IProctoringService
    {
        private readonly AppDbContext _context;
        private readonly IAutoDisqualifyService _autoDisqualifyService;

        private static readonly HashSet<string> TabSwitchTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "TabSwitch", "VisibilityHidden", "WindowBlur"
        };

        private static readonly HashSet<string> ScreenshotTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Screenshot", "PrintScreen", "ScreenCapture"
        };

        public ProctoringService(AppDbContext context, IAutoDisqualifyService autoDisqualifyService)
        {
            _context = context;
            _autoDisqualifyService = autoDisqualifyService;
        }

        public async Task<ProctoringSession> StartSessionAsync(int codingTestAttemptId)
        {
            var existing = await _context.ProctoringSessions
                .FirstOrDefaultAsync(s => s.CodingTestAttemptId == codingTestAttemptId && s.Status == "Active");

            if (existing != null)
                return existing;

            var session = new ProctoringSession
            {
                CodingTestAttemptId = codingTestAttemptId,
                StartedAt = DateTime.UtcNow,
                Status = "Active"
            };

            _context.ProctoringSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<ProctoringStatusResponse> IngestEventsAsync(IngestProctoringEventsRequest request)
        {
            var attempt = await _context.CodingTestAttempts
                .Include(a => a.CodingTest)
                .FirstOrDefaultAsync(a => a.Id == request.CodingTestAttemptId);

            if (attempt == null)
                throw new ArgumentException($"Attempt {request.CodingTestAttemptId} not found");

            if (attempt.UserId != request.UserId)
                throw new UnauthorizedAccessException("You do not own this attempt.");

            if (attempt.IntegrityStatus == "Disqualified" || attempt.Status is "Submitted" or "Completed")
                return await BuildStatusResponseAsync(attempt);

            var session = await _context.ProctoringSessions
                .FirstOrDefaultAsync(s => s.CodingTestAttemptId == request.CodingTestAttemptId && s.Status == "Active");

            if (session == null)
                session = await StartSessionAsync(request.CodingTestAttemptId);

            foreach (var evt in request.Events)
            {
                _context.ProctoringEvents.Add(new ProctoringEvent
                {
                    SessionId = session.Id,
                    EventType = evt.EventType,
                    OccurredAt = evt.OccurredAt,
                    ClientSequence = evt.ClientSequence,
                    PayloadJson = evt.PayloadJson
                });
            }

            await _context.SaveChangesAsync();

            if (attempt.CodingTest.ApplyBreachRule && attempt.CodingTest.EnableProctoring)
                await UpdateIntegrityStatusAsync(attempt);

            return await BuildStatusResponseAsync(attempt);
        }

        public async Task<ProctoringStatusResponse> GetStatusAsync(int attemptId, int userId)
        {
            var attempt = await _context.CodingTestAttempts
                .Include(a => a.CodingTest)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null)
                throw new ArgumentException($"Attempt {attemptId} not found");

            if (attempt.UserId != userId)
                throw new UnauthorizedAccessException("You do not own this attempt.");

            return await BuildStatusResponseAsync(attempt);
        }

        public async Task<ProctoringStatusResponse> GetStatusForAttemptAsync(int attemptId)
        {
            var attempt = await _context.CodingTestAttempts
                .Include(a => a.CodingTest)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null)
                throw new ArgumentException($"Attempt {attemptId} not found");

            return await BuildStatusResponseAsync(attempt);
        }

        public async Task<List<ProctoringEventDto>> GetEventsForAttemptAsync(int attemptId)
        {
            return await _context.ProctoringEvents
                .Where(e => e.Session.CodingTestAttemptId == attemptId)
                .OrderBy(e => e.OccurredAt)
                .Select(e => new ProctoringEventDto
                {
                    EventType = e.EventType,
                    OccurredAt = e.OccurredAt,
                    ClientSequence = e.ClientSequence,
                    PayloadJson = e.PayloadJson
                })
                .ToListAsync();
        }

        private async Task UpdateIntegrityStatusAsync(CodingTestAttempt attempt)
        {
            var test = attempt.CodingTest;
            var counts = await GetEventCountsAsync(attempt.Id);
            var breachCount = counts.BreachCount;
            var previousStatus = attempt.IntegrityStatus;

            if (test.BreachRuleLimit <= 0)
            {
                attempt.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return;
            }

            var breachStatus = ComputeIntegrityStatusFromBreaches(breachCount, test);
            var newStatus = MaxIntegrityStatus(previousStatus, breachStatus);

            attempt.IntegrityStatus = newStatus;
            attempt.UpdatedAt = DateTime.UtcNow;

            if (breachCount >= test.WarningThreshold && test.WarningThreshold > 0)
            {
                await EnsureIntegrityFlagAsync(attempt.Id, "BreachWarning", "Medium",
                    $"{{\"breachCount\":{breachCount},\"warningThreshold\":{test.WarningThreshold}}}");
            }

            if (breachCount >= test.FlagThreshold && test.FlagThreshold > 0)
            {
                await EnsureIntegrityFlagAsync(attempt.Id, "BreachFlagged", "High",
                    $"{{\"breachCount\":{breachCount},\"flagThreshold\":{test.FlagThreshold}}}");
            }

            await _context.SaveChangesAsync();

            if (newStatus == "Disqualified" && previousStatus != "Disqualified")
                await _autoDisqualifyService.DisqualifyAndAutoSubmitAsync(attempt.Id, breachCount);
        }

        private static string ComputeIntegrityStatusFromBreaches(int breachCount, CodingTest test)
        {
            if (test.BreachRuleLimit > 0 && breachCount >= test.BreachRuleLimit)
                return "Disqualified";
            if (test.FlagThreshold > 0 && breachCount >= test.FlagThreshold)
                return "Flagged";
            if (test.WarningThreshold > 0 && breachCount >= test.WarningThreshold)
                return "Warning";
            return "Normal";
        }

        private static string MaxIntegrityStatus(string a, string b)
        {
            var rank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Normal"] = 0,
                ["Warning"] = 1,
                ["Flagged"] = 2,
                ["Disqualified"] = 3
            };
            return rank.GetValueOrDefault(a, 0) >= rank.GetValueOrDefault(b, 0) ? a : b;
        }

        private async Task EnsureIntegrityFlagAsync(int attemptId, string flagType, string severity, string detailsJson)
        {
            var existing = await _context.IntegrityFlags
                .FirstOrDefaultAsync(f => f.CodingTestAttemptId == attemptId
                                       && f.FlagType == flagType
                                       && f.ReviewStatus == "Pending");

            if (existing != null)
            {
                if (SeverityRank(severity) > SeverityRank(existing.Severity))
                    existing.Severity = severity;
                existing.DetailsJson = detailsJson;
                return;
            }

            _context.IntegrityFlags.Add(new IntegrityFlag
            {
                CodingTestAttemptId = attemptId,
                FlagType = flagType,
                Severity = severity,
                DetailsJson = detailsJson,
                CreatedAt = DateTime.UtcNow,
                ReviewStatus = "Pending"
            });
        }

        private static int SeverityRank(string severity) => severity.ToLowerInvariant() switch
        {
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            _ => 0
        };

        private async Task<ProctoringStatusResponse> BuildStatusResponseAsync(CodingTestAttempt attempt)
        {
            var counts = await GetEventCountsAsync(attempt.Id);
            var test = attempt.CodingTest;

            string? lastWarning = attempt.IntegrityStatus switch
            {
                "Disqualified" => $"Disqualified: breach limit reached ({counts.BreachCount}/{test.BreachRuleLimit})",
                "Warning" => $"Proctoring warning ({counts.BreachCount}/{test.WarningThreshold})",
                "Flagged" => $"Proctoring flagged ({counts.BreachCount}/{test.FlagThreshold})",
                _ => null
            };

            return new ProctoringStatusResponse
            {
                CodingTestAttemptId = attempt.Id,
                IntegrityStatus = attempt.IntegrityStatus,
                BreachCount = counts.BreachCount,
                TabSwitchCount = counts.TabSwitchCount,
                WindowBlurCount = counts.WindowBlurCount,
                PasteCount = counts.PasteCount,
                ScreenshotCount = counts.ScreenshotCount,
                WarningThreshold = test.WarningThreshold,
                FlagThreshold = test.FlagThreshold,
                BreachRuleLimit = test.BreachRuleLimit,
                RequireFullscreen = test.RequireFullscreen,
                BlockPaste = test.BlockPaste,
                LastWarning = lastWarning
            };
        }

        private async Task<(int BreachCount, int TabSwitchCount, int WindowBlurCount, int PasteCount, int ScreenshotCount)> GetEventCountsAsync(int attemptId)
        {
            var events = await _context.ProctoringEvents
                .Where(e => e.Session.CodingTestAttemptId == attemptId)
                .Select(e => e.EventType)
                .ToListAsync();

            var breachCount = events.Count;
            var tabSwitch = events.Count(e => TabSwitchTypes.Contains(e));
            var windowBlur = events.Count(e => e.Equals("WindowBlur", StringComparison.OrdinalIgnoreCase));
            var paste = events.Count(e => e.Equals("Paste", StringComparison.OrdinalIgnoreCase)
                                       || e.Equals("Copy", StringComparison.OrdinalIgnoreCase));
            var screenshot = events.Count(e => ScreenshotTypes.Contains(e));

            return (breachCount, tabSwitch, windowBlur, paste, screenshot);
        }
    }
}
