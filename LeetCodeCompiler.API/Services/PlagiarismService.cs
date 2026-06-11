using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Services
{
    public class PlagiarismService : IPlagiarismService
    {
        private readonly AppDbContext _context;
        private static readonly ConcurrentQueue<PlagiarismCheckJob> Queue = new();

        public PlagiarismService(AppDbContext context)
        {
            _context = context;
        }

        public void EnqueueCheck(PlagiarismCheckJob job) => Queue.Enqueue(job);

        public static bool TryDequeue(out PlagiarismCheckJob job) => Queue.TryDequeue(out job!);

        public async Task<PlagiarismReportResponse?> GetReportAsync(long submissionId)
        {
            var report = await _context.PlagiarismReports
                .Include(r => r.Matches)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.SubmissionId == submissionId);

            if (report == null) return null;

            return MapReport(report);
        }

        public async Task ProcessCheckAsync(PlagiarismCheckJob job)
        {
            var test = await _context.CodingTests.FindAsync(job.CodingTestId);
            if (test == null || !test.EnablePlagiarismCheck)
                return;

            var source = await _context.CodingTestSubmissions.FindAsync(job.SubmissionId);
            if (source == null || string.IsNullOrWhiteSpace(source.FinalCodeSnapshot))
                return;

            var peersQuery = _context.CodingTestSubmissions
                .Where(s => s.CodingTestId == job.CodingTestId
                         && s.SubmissionId != job.SubmissionId
                         && s.FinalCodeSnapshot != "");

            if (job.ProblemId.HasValue)
                peersQuery = peersQuery.Where(s => s.ProblemId == job.ProblemId);

            var peers = await peersQuery.ToListAsync();

            var sourceTokens = Tokenize(source.FinalCodeSnapshot);
            var matches = new List<PlagiarismMatch>();
            decimal maxScore = 0;

            foreach (var peer in peers)
            {
                var score = JaccardSimilarity(sourceTokens, Tokenize(peer.FinalCodeSnapshot));
                if (score < 0.5m) continue;

                matches.Add(new PlagiarismMatch
                {
                    MatchedSubmissionId = peer.SubmissionId,
                    SimilarityScore = score,
                    MatchType = "SameTest"
                });

                if (score > maxScore) maxScore = score;
            }

            var status = maxScore >= 80 ? "ManualReview" : "Clear";
            if (maxScore >= 95) status = "AutoFlagged";

            var report = new PlagiarismReport
            {
                SubmissionId = job.SubmissionId,
                CodingTestAttemptId = job.CodingTestAttemptId,
                ProblemId = job.ProblemId,
                MaxSimilarityScore = maxScore,
                Status = status,
                CheckedAt = DateTime.UtcNow,
                Matches = matches.OrderByDescending(m => m.SimilarityScore).Take(10).ToList()
            };

            _context.PlagiarismReports.Add(report);
            await _context.SaveChangesAsync();

            if (maxScore >= 80)
            {
                _context.IntegrityFlags.Add(new IntegrityFlag
                {
                    CodingTestAttemptId = job.CodingTestAttemptId,
                    SubmissionId = job.SubmissionId,
                    FlagType = "Plagiarism",
                    Severity = maxScore >= 95 ? "High" : "Medium",
                    DetailsJson = $"{{\"maxSimilarity\":{maxScore:F2},\"matchCount\":{matches.Count}}}",
                    CreatedAt = DateTime.UtcNow,
                    ReviewStatus = "Pending"
                });

                var attempt = await _context.CodingTestAttempts.FindAsync(job.CodingTestAttemptId);
                if (attempt != null && maxScore >= 95)
                    attempt.IntegrityStatus = "Flagged";

                await _context.SaveChangesAsync();
            }
        }

        internal static HashSet<string> Tokenize(string code)
        {
            var normalized = Regex.Replace(code, @"//.*?$|/\*.*?\*/", "", RegexOptions.Multiline | RegexOptions.Singleline);
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim().ToLowerInvariant();
            var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return tokens.ToHashSet();
        }

        internal static decimal JaccardSimilarity(HashSet<string> a, HashSet<string> b)
        {
            if (a.Count == 0 && b.Count == 0) return 100m;
            if (a.Count == 0 || b.Count == 0) return 0m;

            var intersection = a.Intersect(b).Count();
            var union = a.Union(b).Count();
            return Math.Round((decimal)intersection / union * 100m, 2);
        }

        private static PlagiarismReportResponse MapReport(PlagiarismReport report) => new()
        {
            Id = report.Id,
            SubmissionId = report.SubmissionId,
            CodingTestAttemptId = report.CodingTestAttemptId,
            ProblemId = report.ProblemId,
            MaxSimilarityScore = report.MaxSimilarityScore,
            Status = report.Status,
            CheckedAt = report.CheckedAt,
            Matches = report.Matches.Select(m => new PlagiarismMatchResponse
            {
                MatchedSubmissionId = m.MatchedSubmissionId,
                SimilarityScore = m.SimilarityScore,
                MatchType = m.MatchType
            }).ToList()
        };
    }
}
