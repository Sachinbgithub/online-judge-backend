using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Services
{
    public class ProctoringService : IProctoringService
    {
        private readonly AppDbContext _context;

        private static readonly HashSet<string> TabSwitchTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "TabSwitch", "VisibilityHidden", "WindowBlur"
        };

        private static readonly HashSet<string> ScreenshotTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Screenshot", "PrintScreen", "ScreenCapture"
        };

        /// <summary>First detected screenshot attempt triggers Warning; second+ triggers Flagged.</summary>
        private const int ScreenshotWarningThreshold = 1;
        private const int ScreenshotFlagThreshold = 2;

        public ProctoringService(AppDbContext context)
        {
            _context = context;
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

            var tabStatus = ComputeTabIntegrityStatus(counts.TabSwitchCount, test);
            var screenshotStatus = ComputeScreenshotIntegrityStatus(counts.ScreenshotCount);
            var newStatus = MaxIntegrityStatus(tabStatus, screenshotStatus);

            attempt.IntegrityStatus = newStatus;
            attempt.UpdatedAt = DateTime.UtcNow;

            if (tabStatus == "Flagged")
            {
                await EnsureIntegrityFlagAsync(attempt.Id, "TabSwitch", "High",
                    $"{{\"tabSwitchCount\":{counts.TabSwitchCount},\"warningThreshold\":{test.WarningThreshold},\"flagThreshold\":{test.FlagThreshold},\"breachRuleLimit\":{test.BreachRuleLimit}}}");
            }

            if (counts.ScreenshotCount >= ScreenshotFlagThreshold)
            {
                await EnsureIntegrityFlagAsync(attempt.Id, "Screenshot", "High",
                    $"{{\"screenshotCount\":{counts.ScreenshotCount},\"flagThreshold\":{ScreenshotFlagThreshold}}}");
            }
            else if (counts.ScreenshotCount >= ScreenshotWarningThreshold)
            {
                await EnsureIntegrityFlagAsync(attempt.Id, "Screenshot", "Medium",
                    $"{{\"screenshotCount\":{counts.ScreenshotCount},\"warningThreshold\":{ScreenshotWarningThreshold}}}");
            }

            await _context.SaveChangesAsync();
        }

        private static string ComputeTabIntegrityStatus(int tabSwitchCount, CodingTest test)
        {
            if (test.BreachRuleLimit > 0 && tabSwitchCount >= test.BreachRuleLimit)
                return "Flagged";
            if (tabSwitchCount >= test.FlagThreshold)
                return "Flagged";
            if (tabSwitchCount >= test.WarningThreshold)
                return "Warning";
            return "Normal";
        }

        private static string ComputeScreenshotIntegrityStatus(int screenshotCount)
        {
            if (screenshotCount >= ScreenshotFlagThreshold)
                return "Flagged";
            if (screenshotCount >= ScreenshotWarningThreshold)
                return "Warning";
            return "Normal";
        }

        private static string MaxIntegrityStatus(string a, string b)
        {
            var rank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Normal"] = 0,
                ["Warning"] = 1,
                ["Flagged"] = 2
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

            string? lastWarning = null;
            if (attempt.IntegrityStatus == "Warning")
            {
                if (counts.ScreenshotCount >= ScreenshotWarningThreshold)
                    lastWarning = $"Screenshot attempt detected ({counts.ScreenshotCount})";
                else
                    lastWarning = $"Tab switch warning ({counts.TabSwitchCount}/{test.WarningThreshold})";
            }
            else if (attempt.IntegrityStatus == "Flagged")
            {
                if (counts.ScreenshotCount >= ScreenshotFlagThreshold)
                    lastWarning = $"Screenshot limit reached ({counts.ScreenshotCount})";
                else if (counts.ScreenshotCount >= ScreenshotWarningThreshold)
                    lastWarning = $"Screenshot attempt detected ({counts.ScreenshotCount})";
                else
                    lastWarning = $"Attempt flagged ({counts.TabSwitchCount} tab switches)";
            }

            return new ProctoringStatusResponse
            {
                CodingTestAttemptId = attempt.Id,
                IntegrityStatus = attempt.IntegrityStatus,
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

        private async Task<(int TabSwitchCount, int WindowBlurCount, int PasteCount, int ScreenshotCount)> GetEventCountsAsync(int attemptId)
        {
            var events = await _context.ProctoringEvents
                .Where(e => e.Session.CodingTestAttemptId == attemptId)
                .Select(e => e.EventType)
                .ToListAsync();

            var tabSwitch = events.Count(e => TabSwitchTypes.Contains(e));
            var windowBlur = events.Count(e => e.Equals("WindowBlur", StringComparison.OrdinalIgnoreCase));
            var paste = events.Count(e => e.Equals("Paste", StringComparison.OrdinalIgnoreCase));
            var screenshot = events.Count(e => ScreenshotTypes.Contains(e));

            return (tabSwitch, windowBlur, paste, screenshot);
        }
    }
}
