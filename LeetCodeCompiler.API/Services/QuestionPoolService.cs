using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Services
{
    public class QuestionPoolService : IQuestionPoolService
    {
        private readonly AppDbContext _context;

        public QuestionPoolService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<QuestionPoolResponse> CreatePoolAsync(CreateQuestionPoolRequest request)
        {
            var pool = new QuestionPool
            {
                Name = request.Name,
                DomainId = request.DomainId,
                SubdomainId = request.SubdomainId,
                CreatedBy = request.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.QuestionPools.Add(pool);
            await _context.SaveChangesAsync();

            if (request.ProblemIds.Count > 0)
                await AddProblemsToPoolAsync(pool.Id, request.ProblemIds);

            return (await GetPoolAsync(pool.Id))!;
        }

        public async Task<QuestionPoolResponse?> GetPoolAsync(int poolId)
        {
            var pool = await _context.QuestionPools
                .Include(p => p.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == poolId);

            if (pool == null) return null;

            return new QuestionPoolResponse
            {
                Id = pool.Id,
                Name = pool.Name,
                DomainId = pool.DomainId,
                SubdomainId = pool.SubdomainId,
                IsActive = pool.IsActive,
                ItemCount = pool.Items.Count,
                ProblemIds = pool.Items.Select(i => i.ProblemId).ToList()
            };
        }

        public async Task<List<QuestionPoolResponse>> GetPoolsAsync(bool activeOnly = true)
        {
            var query = _context.QuestionPools.Include(p => p.Items).AsNoTracking();
            if (activeOnly)
                query = query.Where(p => p.IsActive);

            var pools = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            return pools.Select(pool => new QuestionPoolResponse
            {
                Id = pool.Id,
                Name = pool.Name,
                DomainId = pool.DomainId,
                SubdomainId = pool.SubdomainId,
                IsActive = pool.IsActive,
                ItemCount = pool.Items.Count,
                ProblemIds = pool.Items.Select(i => i.ProblemId).ToList()
            }).ToList();
        }

        public async Task<QuestionPoolResponse> AddProblemsToPoolAsync(int poolId, List<int> problemIds)
        {
            var pool = await _context.QuestionPools.FindAsync(poolId)
                ?? throw new ArgumentException($"Pool {poolId} not found");

            var existing = await _context.QuestionPoolItems
                .Where(i => i.PoolId == poolId)
                .Select(i => i.ProblemId)
                .ToListAsync();

            foreach (var problemId in problemIds.Distinct().Where(id => !existing.Contains(id)))
            {
                _context.QuestionPoolItems.Add(new QuestionPoolItem
                {
                    PoolId = poolId,
                    ProblemId = problemId,
                    Weight = 1
                });
            }

            await _context.SaveChangesAsync();
            return (await GetPoolAsync(poolId))!;
        }

        public async Task<bool> DeletePoolAsync(int poolId)
        {
            var pool = await _context.QuestionPools.FindAsync(poolId);
            if (pool == null) return false;

            pool.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task CreateAttemptQuestionSnapshotAsync(CodingTestAttempt attempt)
        {
            if (attempt.QuestionSnapshotCreated)
                return;

            var codingTest = await _context.CodingTests
                .Include(t => t.Questions)
                .Include(t => t.PoolSections)
                .FirstOrDefaultAsync(t => t.Id == attempt.CodingTestId);

            if (codingTest == null)
                throw new ArgumentException($"Test {attempt.CodingTestId} not found");

            var seed = Guid.NewGuid();
            attempt.PoolSelectionSeed = seed;
            var rng = new Random(seed.GetHashCode());

            var selectedProblemIds = new HashSet<int>();
            var snapshotRows = new List<CodingTestAttemptQuestion>();

            foreach (var fixedQ in codingTest.Questions.OrderBy(q => q.QuestionOrder))
            {
                selectedProblemIds.Add(fixedQ.ProblemId);
                snapshotRows.Add(new CodingTestAttemptQuestion
                {
                    CodingTestAttemptId = attempt.Id,
                    ProblemId = fixedQ.ProblemId,
                    QuestionOrder = fixedQ.QuestionOrder,
                    Marks = fixedQ.Marks,
                    TimeLimitMinutes = fixedQ.TimeLimitMinutes,
                    Source = "Fixed",
                    CodingTestQuestionId = fixedQ.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var nextOrder = snapshotRows.Count > 0 ? snapshotRows.Max(r => r.QuestionOrder) + 1 : 1;

            foreach (var section in codingTest.PoolSections.OrderBy(s => s.SectionOrder))
            {
                var poolItems = await _context.QuestionPoolItems
                    .Where(i => i.PoolId == section.PoolId && i.Pool.IsActive)
                    .Select(i => i.ProblemId)
                    .ToListAsync();

                var available = poolItems.Where(id => !selectedProblemIds.Contains(id)).ToList();
                var pickCount = Math.Min(section.QuestionsToPick, available.Count);

                var picked = available.OrderBy(_ => rng.Next()).Take(pickCount).ToList();

                foreach (var problemId in picked)
                {
                    selectedProblemIds.Add(problemId);
                    snapshotRows.Add(new CodingTestAttemptQuestion
                    {
                        CodingTestAttemptId = attempt.Id,
                        ProblemId = problemId,
                        QuestionOrder = nextOrder++,
                        Marks = section.MarksPerQuestion,
                        TimeLimitMinutes = section.TimeLimitMinutes,
                        Source = "Pool",
                        PoolSectionId = section.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Shuffle display order with seeded RNG
            var shuffled = snapshotRows.OrderBy(_ => rng.Next()).ToList();
            for (int i = 0; i < shuffled.Count; i++)
                shuffled[i].QuestionOrder = i + 1;

            _context.CodingTestAttemptQuestions.AddRange(shuffled);
            attempt.QuestionSnapshotCreated = true;
            attempt.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<List<AttemptQuestionResponse>> GetAttemptQuestionsAsync(int attemptId)
        {
            var questions = await _context.CodingTestAttemptQuestions
                .Include(q => q.Problem)
                .Where(q => q.CodingTestAttemptId == attemptId)
                .OrderBy(q => q.QuestionOrder)
                .AsNoTracking()
                .ToListAsync();

            if (questions.Count > 0)
                return questions.Select(MapAttemptQuestion).ToList();

            var attempt = await _context.CodingTestAttempts.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attemptId);
            if (attempt == null) return new List<AttemptQuestionResponse>();

            var fixedQuestions = await _context.CodingTestQuestions
                .Include(q => q.Problem)
                .Where(q => q.CodingTestId == attempt.CodingTestId)
                .OrderBy(q => q.QuestionOrder)
                .AsNoTracking()
                .ToListAsync();

            return fixedQuestions.Select(fq => new AttemptQuestionResponse
            {
                Id = fq.Id,
                ProblemId = fq.ProblemId,
                QuestionOrder = fq.QuestionOrder,
                Marks = fq.Marks,
                TimeLimitMinutes = fq.TimeLimitMinutes,
                Source = "Fixed",
                CodingTestQuestionId = fq.Id,
                Problem = fq.Problem != null ? new ProblemResponse
                {
                    Id = fq.Problem.Id,
                    Title = fq.Problem.Title,
                    Description = fq.Problem.Description,
                    Examples = fq.Problem.Examples,
                    Constraints = fq.Problem.Constraints,
                    Difficulty = fq.Problem.Difficulty.ToString()
                } : null
            }).ToList();
        }

        public async Task<(bool IsAllowed, CodingTestAttemptQuestion? Snapshot, CodingTestQuestion? FixedQuestion)> ResolveQuestionForAttemptAsync(
            int attemptId, int problemId)
        {
            var snapshot = await _context.CodingTestAttemptQuestions
                .FirstOrDefaultAsync(q => q.CodingTestAttemptId == attemptId && q.ProblemId == problemId);

            if (snapshot != null)
            {
                CodingTestQuestion? fixedQ = null;
                if (snapshot.CodingTestQuestionId.HasValue)
                    fixedQ = await _context.CodingTestQuestions.FindAsync(snapshot.CodingTestQuestionId.Value);

                return (true, snapshot, fixedQ);
            }

            var hasSnapshot = await _context.CodingTestAttemptQuestions
                .AnyAsync(q => q.CodingTestAttemptId == attemptId);

            if (hasSnapshot)
                return (false, null, null);

            var attempt = await _context.CodingTestAttempts.FindAsync(attemptId);
            if (attempt == null)
                return (false, null, null);

            var fixedQuestion = await _context.CodingTestQuestions
                .FirstOrDefaultAsync(q => q.CodingTestId == attempt.CodingTestId && q.ProblemId == problemId);

            return (fixedQuestion != null, null, fixedQuestion);
        }

        private static AttemptQuestionResponse MapAttemptQuestion(CodingTestAttemptQuestion q)
        {
            return new AttemptQuestionResponse
            {
                Id = q.Id,
                ProblemId = q.ProblemId,
                QuestionOrder = q.QuestionOrder,
                Marks = q.Marks,
                TimeLimitMinutes = q.TimeLimitMinutes,
                Source = q.Source,
                CodingTestQuestionId = q.CodingTestQuestionId,
                PoolSectionId = q.PoolSectionId,
                Problem = q.Problem != null ? new ProblemResponse
                {
                    Id = q.Problem.Id,
                    Title = q.Problem.Title,
                    Description = q.Problem.Description,
                    Examples = q.Problem.Examples,
                    Constraints = q.Problem.Constraints,
                    Difficulty = q.Problem.Difficulty.ToString()
                } : null
            };
        }
    }
}
