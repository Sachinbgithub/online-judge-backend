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

            var previousStatus = attempt.IntegrityStatus;
            string newStatus = "Normal";
            string? lastWarning = null;

            if (test.BreachRuleLimit > 0 && counts.TabSwitchCount >= test.BreachRuleLimit)
            {
                newStatus = "Flagged";
                lastWarning = $"Tab switch limit reached ({counts.TabSwitchCount}/{test.BreachRuleLimit})";
            }
            else if (counts.TabSwitchCount >= test.FlagThreshold)
            {
                newStatus = "Flagged";
                lastWarning = $"Tab switches flagged ({counts.TabSwitchCount}/{test.FlagThreshold})";
            }
            else if (counts.TabSwitchCount >= test.WarningThreshold)
            {
                newStatus = "Warning";
                lastWarning = $"Tab switch warning ({counts.TabSwitchCount}/{test.WarningThreshold})";
            }

            attempt.IntegrityStatus = newStatus;
            attempt.UpdatedAt = DateTime.UtcNow;

            if (newStatus == "Flagged" && previousStatus != "Flagged")
            {
                var alreadyFlagged = await _context.IntegrityFlags
                    .AnyAsync(f => f.CodingTestAttemptId == attempt.Id
                                && f.FlagType == "TabSwitch"
                                && f.ReviewStatus == "Pending");

                if (!alreadyFlagged)
                {
                    _context.IntegrityFlags.Add(new IntegrityFlag
                    {
                        CodingTestAttemptId = attempt.Id,
                        FlagType = "TabSwitch",
                        Severity = "High",
                        DetailsJson = $"{{\"tabSwitchCount\":{counts.TabSwitchCount},\"warningThreshold\":{test.WarningThreshold},\"flagThreshold\":{test.FlagThreshold},\"breachRuleLimit\":{test.BreachRuleLimit}}}",
                        CreatedAt = DateTime.UtcNow,
                        ReviewStatus = "Pending"
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task<ProctoringStatusResponse> BuildStatusResponseAsync(CodingTestAttempt attempt)
        {
            var counts = await GetEventCountsAsync(attempt.Id);
            var test = attempt.CodingTest;

            string? lastWarning = null;
            if (attempt.IntegrityStatus == "Warning")
                lastWarning = $"Tab switch warning ({counts.TabSwitchCount}/{test.WarningThreshold})";
            else if (attempt.IntegrityStatus == "Flagged")
                lastWarning = $"Attempt flagged ({counts.TabSwitchCount} tab switches)";

            return new ProctoringStatusResponse
            {
                CodingTestAttemptId = attempt.Id,
                IntegrityStatus = attempt.IntegrityStatus,
                TabSwitchCount = counts.TabSwitchCount,
                WindowBlurCount = counts.WindowBlurCount,
                PasteCount = counts.PasteCount,
                WarningThreshold = test.WarningThreshold,
                FlagThreshold = test.FlagThreshold,
                BreachRuleLimit = test.BreachRuleLimit,
                RequireFullscreen = test.RequireFullscreen,
                BlockPaste = test.BlockPaste,
                LastWarning = lastWarning
            };
        }

        private async Task<(int TabSwitchCount, int WindowBlurCount, int PasteCount)> GetEventCountsAsync(int attemptId)
        {
            var events = await _context.ProctoringEvents
                .Where(e => e.Session.CodingTestAttemptId == attemptId)
                .Select(e => e.EventType)
                .ToListAsync();

            var tabSwitch = events.Count(e => TabSwitchTypes.Contains(e) || e.Equals("TabSwitch", StringComparison.OrdinalIgnoreCase));
            var windowBlur = events.Count(e => e.Equals("WindowBlur", StringComparison.OrdinalIgnoreCase));
            var paste = events.Count(e => e.Equals("Paste", StringComparison.OrdinalIgnoreCase));

            return (tabSwitch, windowBlur, paste);
        }
    }
}
