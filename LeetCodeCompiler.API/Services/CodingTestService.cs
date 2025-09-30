using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Services
{
    public class CodingTestService : ICodingTestService
    {
        private readonly AppDbContext _context;

        public CodingTestService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CodingTestResponse> CreateCodingTestAsync(CreateCodingTestRequest request)
        {
            var codingTest = new CodingTest
            {
                TestName = request.TestName,
                CreatedBy = request.CreatedBy,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                DurationMinutes = request.DurationMinutes,
                TotalQuestions = request.TotalQuestions,
                TotalMarks = request.TotalMarks,
                TestType = request.TestType,
                AllowMultipleAttempts = request.AllowMultipleAttempts,
                MaxAttempts = request.MaxAttempts,
                ShowResultsImmediately = request.ShowResultsImmediately,
                AllowCodeReview = request.AllowCodeReview,
                AccessCode = request.AccessCode,
                Tags = request.Tags,
                IsResultPublishAutomatically = request.IsResultPublishAutomatically,
                ApplyBreachRule = request.ApplyBreachRule,
                BreachRuleLimit = request.BreachRuleLimit,
                HostIP = request.HostIP,
                ClassId = request.ClassId,
                CreatedAt = DateTime.UtcNow
            };

            _context.CodingTests.Add(codingTest);
            await _context.SaveChangesAsync();

            // Add questions
            foreach (var questionRequest in request.Questions)
            {
                var question = new CodingTestQuestion
                {
                    CodingTestId = codingTest.Id,
                    ProblemId = questionRequest.ProblemId,
                    QuestionOrder = questionRequest.QuestionOrder,
                    Marks = questionRequest.Marks,
                    TimeLimitMinutes = questionRequest.TimeLimitMinutes,
                    CustomInstructions = questionRequest.CustomInstructions,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CodingTestQuestions.Add(question);
            }

            // Add topic data
            foreach (var topicDataRequest in request.TopicData)
            {
                var topicData = new CodingTestTopicData
                {
                    CodingTestId = codingTest.Id,
                    SectionId = topicDataRequest.SectionId,
                    DomainId = topicDataRequest.DomainId,
                    SubdomainId = topicDataRequest.SubdomainId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CodingTestTopicData.Add(topicData);
            }

            await _context.SaveChangesAsync();
            return await GetCodingTestByIdAsync(codingTest.Id);
        }

        public async Task<CodingTestResponse> GetCodingTestByIdAsync(int id)
        {
            var codingTest = await _context.CodingTests
                .Include(ct => ct.Questions)
                    .ThenInclude(q => q.Problem)
                        .ThenInclude(p => p.TestCases)
                .Include(ct => ct.Attempts)
                .FirstOrDefaultAsync(ct => ct.Id == id);

            if (codingTest == null)
                throw new ArgumentException($"Coding test with ID {id} not found");

            return MapToResponse(codingTest);
        }

        public async Task<List<CodingTestSummaryResponse>> GetAllCodingTestsAsync()
        {
            var codingTests = await _context.CodingTests
                .Include(ct => ct.Attempts)
                .OrderByDescending(ct => ct.CreatedAt)
                .ToListAsync();

            return codingTests.Select(ct => MapToSummaryResponse(ct)).ToList();
        }

        public async Task<List<CodingTestSummaryResponse>> GetCodingTestsByUserAsync(int userId, string? subjectName = null, string? topicName = null, bool isEnabled = true)
        {
            var query = _context.CodingTests
                .Include(ct => ct.Attempts)
                .Include(ct => ct.TopicData)
                .Where(ct => ct.CreatedBy == userId);

            // Apply isEnabled filter
            if (isEnabled)
            {
                query = query.Where(ct => ct.IsActive);
            }

            // Apply subjectName filter (domain name)
            if (!string.IsNullOrEmpty(subjectName))
            {
                query = query.Where(ct => ct.TopicData.Any(td => 
                    _context.Domains.Any(d => d.DomainId == td.DomainId && d.DomainName.Contains(subjectName))));
            }

            // Apply topicName filter (subdomain name)
            if (!string.IsNullOrEmpty(topicName))
            {
                query = query.Where(ct => ct.TopicData.Any(td => 
                    _context.Subdomains.Any(sd => sd.SubdomainId == td.SubdomainId && sd.SubdomainName.Contains(topicName))));
            }

            var codingTests = await query
                .OrderByDescending(ct => ct.CreatedAt)
                .ToListAsync();

            return codingTests.Select(ct => MapToSummaryResponse(ct, subjectName, topicName, isEnabled)).ToList();
        }

        public async Task<CodingTestResponse> UpdateCodingTestAsync(UpdateCodingTestRequest request)
        {
            var codingTest = await _context.CodingTests.FindAsync(request.Id);
            if (codingTest == null)
                throw new ArgumentException($"Coding test with ID {request.Id} not found");

            // Update only provided fields
            if (request.TestName != null) codingTest.TestName = request.TestName;
            if (request.StartDate.HasValue) codingTest.StartDate = request.StartDate.Value;
            if (request.EndDate.HasValue) codingTest.EndDate = request.EndDate.Value;
            if (request.DurationMinutes.HasValue) codingTest.DurationMinutes = request.DurationMinutes.Value;
            if (request.TotalMarks.HasValue) codingTest.TotalMarks = request.TotalMarks.Value;
            if (request.IsActive.HasValue) codingTest.IsActive = request.IsActive.Value;
            if (request.IsPublished.HasValue) codingTest.IsPublished = request.IsPublished.Value;
            if (request.TestType.HasValue) codingTest.TestType = request.TestType.Value;
            if (request.AllowMultipleAttempts.HasValue) codingTest.AllowMultipleAttempts = request.AllowMultipleAttempts.Value;
            if (request.MaxAttempts.HasValue) codingTest.MaxAttempts = request.MaxAttempts.Value;
            if (request.ShowResultsImmediately.HasValue) codingTest.ShowResultsImmediately = request.ShowResultsImmediately.Value;
            if (request.AllowCodeReview.HasValue) codingTest.AllowCodeReview = request.AllowCodeReview.Value;
            if (request.AccessCode != null) codingTest.AccessCode = request.AccessCode;
            if (request.Tags != null) codingTest.Tags = request.Tags;

            codingTest.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetCodingTestByIdAsync(codingTest.Id);
        }

        public async Task<bool> DeleteCodingTestAsync(int id)
        {
            var codingTest = await _context.CodingTests.FindAsync(id);
            if (codingTest == null)
                return false;

            codingTest.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PublishCodingTestAsync(int id)
        {
            var codingTest = await _context.CodingTests.FindAsync(id);
            if (codingTest == null)
                return false;

            codingTest.IsPublished = true;
            codingTest.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnpublishCodingTestAsync(int id)
        {
            var codingTest = await _context.CodingTests.FindAsync(id);
            if (codingTest == null)
                return false;

            codingTest.IsPublished = false;
            codingTest.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CodingTestAttemptResponse> StartCodingTestAsync(StartCodingTestRequest request)
        {
            // Validate access
            if (!await CanUserAttemptTestAsync(request.UserId, request.CodingTestId))
                throw new InvalidOperationException("User cannot attempt this test");

            if (!string.IsNullOrEmpty(request.AccessCode) && !await ValidateAccessCodeAsync(request.CodingTestId, request.AccessCode))
                throw new InvalidOperationException("Invalid access code");

            // Check if user already has an attempt
            var existingAttempt = await _context.CodingTestAttempts
                .FirstOrDefaultAsync(cta => cta.CodingTestId == request.CodingTestId && 
                                          cta.UserId == request.UserId && 
                                          cta.Status == "InProgress");

            if (existingAttempt != null)
                return await GetCodingTestAttemptAsync(existingAttempt.Id);

            // Create new attempt
            var attempt = new CodingTestAttempt
            {
                CodingTestId = request.CodingTestId,
                UserId = request.UserId,
                AttemptNumber = await GetNextAttemptNumber(request.UserId, request.CodingTestId),
                StartedAt = DateTime.UtcNow,
                Status = "InProgress",
                CreatedAt = DateTime.UtcNow
            };

            _context.CodingTestAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return await GetCodingTestAttemptAsync(attempt.Id);
        }

        public async Task<CodingTestAttemptResponse> GetCodingTestAttemptAsync(int attemptId)
        {
            var attempt = await _context.CodingTestAttempts
                .Include(cta => cta.CodingTest)
                .Include(cta => cta.QuestionAttempts)
                    .ThenInclude(qa => qa.CodingTestQuestion)
                        .ThenInclude(q => q.Problem)
                .FirstOrDefaultAsync(cta => cta.Id == attemptId);

            if (attempt == null)
                throw new ArgumentException($"Coding test attempt with ID {attemptId} not found");

            return MapToAttemptResponse(attempt);
        }

        public async Task<List<CodingTestAttemptResponse>> GetUserCodingTestAttemptsAsync(int userId, int codingTestId)
        {
            var attempts = await _context.CodingTestAttempts
                .Include(cta => cta.CodingTest)
                .Include(cta => cta.QuestionAttempts)
                    .ThenInclude(qa => qa.CodingTestQuestion)
                        .ThenInclude(q => q.Problem)
                .Where(cta => cta.UserId == userId && cta.CodingTestId == codingTestId)
                .OrderByDescending(cta => cta.CreatedAt)
                .ToListAsync();

            return attempts.Select(MapToAttemptResponse).ToList();
        }

        public async Task<CodingTestAttemptResponse> SubmitCodingTestAsync(SubmitCodingTestRequest request)
        {
            var attempt = await _context.CodingTestAttempts
                .Include(cta => cta.CodingTest)
                .Include(cta => cta.QuestionAttempts)
                .FirstOrDefaultAsync(cta => cta.Id == request.CodingTestAttemptId && cta.UserId == request.UserId);

            if (attempt == null)
                throw new ArgumentException($"Coding test attempt with ID {request.CodingTestAttemptId} not found");

            if (attempt.Status != "InProgress")
                throw new InvalidOperationException("Test attempt is not in progress");

            // Calculate scores
            var totalScore = attempt.QuestionAttempts.Sum(qa => qa.Score);
            var maxScore = attempt.QuestionAttempts.Sum(qa => qa.MaxScore);
            var percentage = maxScore > 0 ? (double)totalScore / maxScore * 100 : 0;

            // Update attempt
            attempt.Status = "Submitted";
            attempt.SubmittedAt = DateTime.UtcNow;
            attempt.CompletedAt = DateTime.UtcNow;
            attempt.TotalScore = totalScore;
            attempt.MaxScore = maxScore;
            attempt.Percentage = percentage;
            attempt.TimeSpentMinutes = (int)(DateTime.UtcNow - attempt.StartedAt).TotalMinutes;
            attempt.Notes = request.Notes ?? "";
            attempt.IsLateSubmission = DateTime.UtcNow > attempt.CodingTest.EndDate;
            attempt.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetCodingTestAttemptAsync(attempt.Id);
        }

        public async Task<bool> AbandonCodingTestAsync(int attemptId, int userId)
        {
            var attempt = await _context.CodingTestAttempts
                .FirstOrDefaultAsync(cta => cta.Id == attemptId && cta.UserId == userId);

            if (attempt == null)
                return false;

            attempt.Status = "Abandoned";
            attempt.CompletedAt = DateTime.UtcNow;
            attempt.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CodingTestQuestionAttemptResponse> StartQuestionAttemptAsync(int codingTestAttemptId, int questionId, int userId)
        {
            var attempt = await _context.CodingTestAttempts
                .FirstOrDefaultAsync(cta => cta.Id == codingTestAttemptId && cta.UserId == userId);

            if (attempt == null)
                throw new ArgumentException("Invalid test attempt");

            if (attempt.Status != "InProgress")
                throw new InvalidOperationException("Test attempt is not in progress");

            // Check if question attempt already exists
            var existingQuestionAttempt = await _context.CodingTestQuestionAttempts
                .FirstOrDefaultAsync(qa => qa.CodingTestAttemptId == codingTestAttemptId && 
                                          qa.CodingTestQuestionId == questionId);

            if (existingQuestionAttempt != null)
                return await GetQuestionAttemptAsync(existingQuestionAttempt.Id);

            // Create new question attempt
            var questionAttempt = new CodingTestQuestionAttempt
            {
                CodingTestAttemptId = codingTestAttemptId,
                CodingTestQuestionId = questionId,
                ProblemId = await GetProblemIdFromQuestionId(questionId),
                UserId = userId,
                StartedAt = DateTime.UtcNow,
                Status = "InProgress",
                LanguageUsed = "",
                CodeSubmitted = "",
                CreatedAt = DateTime.UtcNow
            };

            _context.CodingTestQuestionAttempts.Add(questionAttempt);
            await _context.SaveChangesAsync();

            return await GetQuestionAttemptAsync(questionAttempt.Id);
        }

        public async Task<CodingTestQuestionAttemptResponse> GetQuestionAttemptAsync(int questionAttemptId)
        {
            var questionAttempt = await _context.CodingTestQuestionAttempts
                .Include(qa => qa.CodingTestQuestion)
                    .ThenInclude(q => q.Problem)
                        .ThenInclude(p => p.TestCases)
                .FirstOrDefaultAsync(qa => qa.Id == questionAttemptId);

            if (questionAttempt == null)
                throw new ArgumentException($"Question attempt with ID {questionAttemptId} not found");

            return MapToQuestionAttemptResponse(questionAttempt);
        }

        public async Task<List<CodingTestQuestionAttemptResponse>> GetQuestionAttemptsForTestAsync(int codingTestAttemptId)
        {
            var questionAttempts = await _context.CodingTestQuestionAttempts
                .Include(qa => qa.CodingTestQuestion)
                    .ThenInclude(q => q.Problem)
                        .ThenInclude(p => p.TestCases)
                .Where(qa => qa.CodingTestAttemptId == codingTestAttemptId)
                .OrderBy(qa => qa.CodingTestQuestion.QuestionOrder)
                .ToListAsync();

            return questionAttempts.Select(MapToQuestionAttemptResponse).ToList();
        }

        public async Task<CodingTestQuestionAttemptResponse> SubmitQuestionAsync(SubmitQuestionRequest request)
        {
            var questionAttempt = await _context.CodingTestQuestionAttempts
                .Include(qa => qa.CodingTestQuestion)
                    .ThenInclude(q => q.Problem)
                .FirstOrDefaultAsync(qa => qa.Id == request.CodingTestQuestionAttemptId && qa.UserId == request.UserId);

            if (questionAttempt == null)
                throw new ArgumentException($"Question attempt with ID {request.CodingTestQuestionAttemptId} not found");

            // Update question attempt
            questionAttempt.Status = "Completed";
            questionAttempt.CompletedAt = DateTime.UtcNow;
            questionAttempt.LanguageUsed = request.LanguageUsed;
            questionAttempt.CodeSubmitted = request.CodeSubmitted;
            questionAttempt.RunCount = request.RunCount;
            questionAttempt.SubmitCount = request.SubmitCount;
            questionAttempt.UpdatedAt = DateTime.UtcNow;

            // TODO: Execute code and calculate score
            // This would integrate with your existing code execution service
            questionAttempt.Score = 0; // Placeholder
            questionAttempt.MaxScore = questionAttempt.CodingTestQuestion.Marks;
            questionAttempt.TestCasesPassed = 0; // Placeholder
            questionAttempt.TotalTestCases = 0; // Placeholder
            questionAttempt.IsCorrect = false; // Placeholder

            await _context.SaveChangesAsync();
            return await GetQuestionAttemptAsync(questionAttempt.Id);
        }

        // Analytics methods
        public async Task<List<CodingTestSummaryResponse>> GetCodingTestsByStatusAsync(string status)
        {
            var now = DateTime.UtcNow;
            var query = _context.CodingTests.Include(ct => ct.Attempts).AsQueryable();

            query = status.ToLower() switch
            {
                "upcoming" => query.Where(ct => ct.StartDate > now),
                "active" => query.Where(ct => ct.StartDate <= now && ct.EndDate >= now && ct.IsActive && ct.IsPublished),
                "completed" => query.Where(ct => ct.EndDate < now),
                "expired" => query.Where(ct => ct.EndDate < now && !ct.IsActive),
                _ => query
            };

            var codingTests = await query.OrderByDescending(ct => ct.CreatedAt).ToListAsync();
            return codingTests.Select(ct => MapToSummaryResponse(ct)).ToList();
        }


        public async Task<object> GetCodingTestAnalyticsAsync(int codingTestId)
        {
            var codingTest = await _context.CodingTests
                .Include(ct => ct.Attempts)
                .FirstOrDefaultAsync(ct => ct.Id == codingTestId);

            if (codingTest == null)
                throw new ArgumentException($"Coding test with ID {codingTestId} not found");

            var totalAttempts = codingTest.Attempts.Count;
            var completedAttempts = codingTest.Attempts.Count(a => a.Status == "Submitted");
            var averageScore = completedAttempts > 0 ? codingTest.Attempts.Where(a => a.Status == "Submitted").Average(a => a.Percentage) : 0;

            return new
            {
                CodingTestId = codingTestId,
                TestName = codingTest.TestName,
                TotalAttempts = totalAttempts,
                CompletedAttempts = completedAttempts,
                AverageScore = Math.Round(averageScore, 2),
                HighestScore = completedAttempts > 0 ? codingTest.Attempts.Where(a => a.Status == "Submitted").Max(a => a.Percentage) : 0,
                LowestScore = completedAttempts > 0 ? codingTest.Attempts.Where(a => a.Status == "Submitted").Min(a => a.Percentage) : 0,
                CompletionRate = totalAttempts > 0 ? Math.Round((double)completedAttempts / totalAttempts * 100, 2) : 0
            };
        }

        public async Task<List<CodingTestAttemptResponse>> GetCodingTestResultsAsync(int codingTestId)
        {
            var attempts = await _context.CodingTestAttempts
                .Include(cta => cta.CodingTest)
                .Include(cta => cta.QuestionAttempts)
                    .ThenInclude(qa => qa.CodingTestQuestion)
                        .ThenInclude(q => q.Problem)
                .Where(cta => cta.CodingTestId == codingTestId && cta.Status == "Submitted")
                .OrderByDescending(cta => cta.Percentage)
                .ToListAsync();

            return attempts.Select(MapToAttemptResponse).ToList();
        }

        // Validation methods
        public async Task<bool> ValidateAccessCodeAsync(int codingTestId, string accessCode)
        {
            var codingTest = await _context.CodingTests.FindAsync(codingTestId);
            return codingTest != null && codingTest.AccessCode == accessCode;
        }

        public async Task<bool> CanUserAttemptTestAsync(int userId, int codingTestId)
        {
            var codingTest = await _context.CodingTests.FindAsync(codingTestId);
            if (codingTest == null || !codingTest.IsActive || !codingTest.IsPublished)
                return false;

            var now = DateTime.UtcNow;
            if (now < codingTest.StartDate || now > codingTest.EndDate)
                return false;

            // Check if the test is assigned to the user
            var isAssigned = await _context.AssignedCodingTests
                .AnyAsync(act => act.CodingTestId == codingTestId 
                             && act.AssignedToUserId == userId 
                             && !act.IsDeleted);

            if (!isAssigned)
                return false;

            if (!codingTest.AllowMultipleAttempts)
            {
                var existingAttempt = await _context.CodingTestAttempts
                    .AnyAsync(cta => cta.CodingTestId == codingTestId && cta.UserId == userId);
                if (existingAttempt)
                    return false;
            }
            else
            {
                var attemptCount = await _context.CodingTestAttempts
                    .CountAsync(cta => cta.CodingTestId == codingTestId && cta.UserId == userId);
                if (attemptCount >= codingTest.MaxAttempts)
                    return false;
            }

            return true;
        }

        public async Task<bool> IsTestActiveAsync(int codingTestId)
        {
            var codingTest = await _context.CodingTests.FindAsync(codingTestId);
            if (codingTest == null)
                return false;

            var now = DateTime.UtcNow;
            return codingTest.IsActive && codingTest.IsPublished && 
                   now >= codingTest.StartDate && now <= codingTest.EndDate;
        }

        public async Task<bool> IsTestExpiredAsync(int codingTestId)
        {
            var codingTest = await _context.CodingTests.FindAsync(codingTestId);
            return codingTest != null && DateTime.UtcNow > codingTest.EndDate;
        }

        // Helper methods
        private async Task<int> GetNextAttemptNumber(int userId, int codingTestId)
        {
            var maxAttempt = await _context.CodingTestAttempts
                .Where(cta => cta.UserId == userId && cta.CodingTestId == codingTestId)
                .MaxAsync(cta => (int?)cta.AttemptNumber) ?? 0;
            return maxAttempt + 1;
        }

        private async Task<int> GetProblemIdFromQuestionId(int questionId)
        {
            var question = await _context.CodingTestQuestions.FindAsync(questionId);
            return question?.ProblemId ?? 0;
        }

        private CodingTestResponse MapToResponse(CodingTest codingTest)
        {
            return new CodingTestResponse
            {
                Id = codingTest.Id,
                TestName = codingTest.TestName,
                CreatedBy = codingTest.CreatedBy,
                CreatedAt = codingTest.CreatedAt,
                UpdatedAt = codingTest.UpdatedAt,
                StartDate = codingTest.StartDate,
                EndDate = codingTest.EndDate,
                DurationMinutes = codingTest.DurationMinutes,
                TotalQuestions = codingTest.TotalQuestions,
                TotalMarks = codingTest.TotalMarks,
                IsActive = codingTest.IsActive,
                IsPublished = codingTest.IsPublished,
                TestType = codingTest.TestType,
                AllowMultipleAttempts = codingTest.AllowMultipleAttempts,
                MaxAttempts = codingTest.MaxAttempts,
                ShowResultsImmediately = codingTest.ShowResultsImmediately,
                AllowCodeReview = codingTest.AllowCodeReview,
                AccessCode = codingTest.AccessCode,
                Tags = codingTest.Tags,
                IsResultPublishAutomatically = codingTest.IsResultPublishAutomatically,
                ApplyBreachRule = codingTest.ApplyBreachRule,
                BreachRuleLimit = codingTest.BreachRuleLimit,
                HostIP = codingTest.HostIP,
                ClassId = codingTest.ClassId,
                TopicData = codingTest.TopicData.Select(MapToTopicDataResponse).ToList(),
                Questions = codingTest.Questions.Select(MapToQuestionResponse).ToList(),
                TotalAttempts = codingTest.Attempts.Count,
                CompletedAttempts = codingTest.Attempts.Count(a => a.Status == "Submitted")
            };
        }

        private TopicDataResponse MapToTopicDataResponse(CodingTestTopicData topicData)
        {
            return new TopicDataResponse
            {
                Id = topicData.Id,
                CodingTestId = topicData.CodingTestId,
                SectionId = topicData.SectionId,
                DomainId = topicData.DomainId,
                SubdomainId = topicData.SubdomainId,
                CreatedAt = topicData.CreatedAt
            };
        }

        private CodingTestQuestionResponse MapToQuestionResponse(CodingTestQuestion question)
        {
            return new CodingTestQuestionResponse
            {
                Id = question.Id,
                CodingTestId = question.CodingTestId,
                ProblemId = question.ProblemId,
                QuestionOrder = question.QuestionOrder,
                Marks = question.Marks,
                TimeLimitMinutes = question.TimeLimitMinutes,
                CustomInstructions = question.CustomInstructions,
                CreatedAt = question.CreatedAt,
                Problem = question.Problem != null ? MapToProblemResponse(question.Problem) : null
            };
        }

        private ProblemResponse MapToProblemResponse(Problem problem)
        {
            return new ProblemResponse
            {
                Id = problem.Id,
                Title = problem.Title,
                Description = problem.Description,
                Examples = problem.Examples,
                Constraints = problem.Constraints,
                Difficulty = problem.Difficulty?.ToString() ?? "Unknown",
                TestCases = problem.TestCases.Select(MapToTestCaseResponse).ToList()
            };
        }

        private TestCaseResponse MapToTestCaseResponse(TestCase testCase)
        {
            return new TestCaseResponse
            {
                Id = testCase.Id,
                ProblemId = testCase.ProblemId,
                Input = testCase.Input,
                ExpectedOutput = testCase.ExpectedOutput
            };
        }

        private CodingTestSummaryResponse MapToSummaryResponse(CodingTest codingTest, string? subjectName = null, string? topicName = null, bool isEnabled = true)
        {
            var now = DateTime.UtcNow;
            var status = "Upcoming";
            if (now >= codingTest.StartDate && now <= codingTest.EndDate && codingTest.IsActive && codingTest.IsPublished)
                status = "Active";
            else if (now > codingTest.EndDate)
                status = "Completed";
            else if (!codingTest.IsActive)
                status = "Expired";

            var completedAttempts = codingTest.Attempts.Where(a => a.Status == "Submitted").ToList();
            var averageScore = completedAttempts.Any() ? completedAttempts.Average(a => a.Percentage) : 0;

            return new CodingTestSummaryResponse
            {
                Id = codingTest.Id,
                TestName = codingTest.TestName,
                StartDate = codingTest.StartDate,
                EndDate = codingTest.EndDate,
                DurationMinutes = codingTest.DurationMinutes,
                TotalQuestions = codingTest.TotalQuestions,
                TotalMarks = codingTest.TotalMarks,
                IsActive = codingTest.IsActive,
                IsPublished = codingTest.IsPublished,
                TestType = codingTest.TestType,
                Status = status,
                TotalAttempts = codingTest.Attempts.Count,
                CompletedAttempts = completedAttempts.Count,
                AverageScore = Math.Round(averageScore, 2),
                CreatedAt = codingTest.CreatedAt,
                SubjectName = subjectName,
                TopicName = topicName,
                IsEnabled = isEnabled
            };
        }

        private CodingTestAttemptResponse MapToAttemptResponse(CodingTestAttempt attempt)
        {
            return new CodingTestAttemptResponse
            {
                Id = attempt.Id,
                CodingTestId = attempt.CodingTestId,
                UserId = attempt.UserId,
                AttemptNumber = attempt.AttemptNumber,
                StartedAt = attempt.StartedAt,
                CompletedAt = attempt.CompletedAt,
                SubmittedAt = attempt.SubmittedAt,
                Status = attempt.Status,
                TotalScore = attempt.TotalScore,
                MaxScore = attempt.MaxScore,
                Percentage = attempt.Percentage,
                TimeSpentMinutes = attempt.TimeSpentMinutes,
                IsLateSubmission = attempt.IsLateSubmission,
                Notes = attempt.Notes,
                CreatedAt = attempt.CreatedAt,
                UpdatedAt = attempt.UpdatedAt,
                QuestionAttempts = attempt.QuestionAttempts.Select(MapToQuestionAttemptResponse).ToList()
            };
        }

        private CodingTestQuestionAttemptResponse MapToQuestionAttemptResponse(CodingTestQuestionAttempt questionAttempt)
        {
            return new CodingTestQuestionAttemptResponse
            {
                Id = questionAttempt.Id,
                CodingTestAttemptId = questionAttempt.CodingTestAttemptId,
                CodingTestQuestionId = questionAttempt.CodingTestQuestionId,
                ProblemId = questionAttempt.ProblemId,
                UserId = questionAttempt.UserId,
                StartedAt = questionAttempt.StartedAt,
                CompletedAt = questionAttempt.CompletedAt,
                Status = questionAttempt.Status,
                LanguageUsed = questionAttempt.LanguageUsed,
                CodeSubmitted = questionAttempt.CodeSubmitted,
                Score = questionAttempt.Score,
                MaxScore = questionAttempt.MaxScore,
                TestCasesPassed = questionAttempt.TestCasesPassed,
                TotalTestCases = questionAttempt.TotalTestCases,
                ExecutionTime = questionAttempt.ExecutionTime,
                RunCount = questionAttempt.RunCount,
                SubmitCount = questionAttempt.SubmitCount,
                IsCorrect = questionAttempt.IsCorrect,
                ErrorMessage = questionAttempt.ErrorMessage,
                CreatedAt = questionAttempt.CreatedAt,
                UpdatedAt = questionAttempt.UpdatedAt,
                Problem = questionAttempt.CodingTestQuestion?.Problem != null ? 
                    MapToProblemResponse(questionAttempt.CodingTestQuestion.Problem) : null
            };
        }

        // Assignment methods
        public async Task<AssignCodingTestResponse> AssignCodingTestAsync(AssignCodingTestRequest request)
        {
            // Check if test exists
            var codingTest = await _context.CodingTests.FindAsync(request.CodingTestId);
            if (codingTest == null)
                throw new ArgumentException($"Coding test with ID {request.CodingTestId} not found");

            // Check if assignment already exists
            var existingAssignment = await _context.AssignedCodingTests
                .FirstOrDefaultAsync(act => act.CodingTestId == request.CodingTestId 
                                         && act.AssignedToUserId == request.AssignedToUserId 
                                         && act.AssignedToUserType == request.AssignedToUserType 
                                         && !act.IsDeleted);

            if (existingAssignment != null)
                throw new InvalidOperationException("Test is already assigned to this user");

            // Create new assignment
            var assignment = new AssignedCodingTest
            {
                CodingTestId = request.CodingTestId,
                AssignedToUserId = request.AssignedToUserId,
                AssignedToUserType = request.AssignedToUserType,
                AssignedByUserId = request.AssignedByUserId,
                AssignedDate = DateTime.UtcNow,
                TestType = request.TestType,
                TestMode = request.TestMode,
                CreatedAt = DateTime.UtcNow
            };

            _context.AssignedCodingTests.Add(assignment);
            await _context.SaveChangesAsync();

            return new AssignCodingTestResponse
            {
                AssignedId = assignment.AssignedId,
                CodingTestId = assignment.CodingTestId,
                AssignedToUserId = assignment.AssignedToUserId,
                AssignedToUserType = assignment.AssignedToUserType,
                AssignedByUserId = assignment.AssignedByUserId,
                AssignedDate = assignment.AssignedDate,
                TestType = assignment.TestType,
                TestMode = assignment.TestMode,
                TestName = codingTest.TestName,
                AssignedByName = "System" // You might want to get this from a user service
            };
        }

        public async Task<List<AssignedCodingTestSummaryResponse>> GetAssignedTestsByUserAsync(long userId, byte userType, int? testType = null, long? classId = null)
        {
            var query = _context.AssignedCodingTests
                .Include(act => act.CodingTest)
                .ThenInclude(ct => ct.TopicData)
                .Where(act => act.AssignedToUserId == userId 
                           && act.AssignedToUserType == userType 
                           && !act.IsDeleted);

            // Apply test type filter
            if (testType.HasValue)
            {
                query = query.Where(act => act.TestType == testType.Value);
            }

            // Apply class filter (similar to the stored procedure logic)
            if (classId.HasValue)
            {
                query = query.Where(act => act.CodingTest.ClassId == classId.Value || act.CodingTest.ClassId == 0);
            }

            var assignments = await query
                .OrderByDescending(act => act.AssignedDate)
                .ToListAsync();

            return assignments.Select(MapToAssignedSummaryResponse).ToList();
        }

        public async Task<bool> UnassignCodingTestAsync(long assignedId, long unassignedByUserId)
        {
            var assignment = await _context.AssignedCodingTests.FindAsync(assignedId);
            if (assignment == null || assignment.IsDeleted)
                return false;

            assignment.IsDeleted = true;
            assignment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<AssignedCodingTestSummaryResponse>> GetAssignedTestsByTestAsync(int codingTestId)
        {
            var assignments = await _context.AssignedCodingTests
                .Include(act => act.CodingTest)
                .ThenInclude(ct => ct.TopicData)
                .Where(act => act.CodingTestId == codingTestId && !act.IsDeleted)
                .OrderByDescending(act => act.AssignedDate)
                .ToListAsync();

            return assignments.Select(MapToAssignedSummaryResponse).ToList();
        }

        private AssignedCodingTestSummaryResponse MapToAssignedSummaryResponse(AssignedCodingTest assignment)
        {
            var codingTest = assignment.CodingTest;
            var now = DateTime.UtcNow;
            
            // Determine status
            string status = "assigned";
            if (now > codingTest.EndDate)
                status = "expired";
            // You might want to check if user has submitted the test here

            // Get subject and topic names from TopicData
            string? subjectName = null;
            string? topicName = null;

            if (codingTest.TopicData.Any())
            {
                var topicData = codingTest.TopicData.First();
                
                // Get domain name
                var domain = _context.Domains.FirstOrDefault(d => d.DomainId == topicData.DomainId);
                subjectName = domain?.DomainName;

                // Get subdomain name
                var subdomain = _context.Subdomains.FirstOrDefault(sd => sd.SubdomainId == topicData.SubdomainId);
                topicName = subdomain?.SubdomainName;
            }

            return new AssignedCodingTestSummaryResponse
            {
                AssignedId = assignment.AssignedId,
                CodingTestId = assignment.CodingTestId,
                TestName = codingTest.TestName,
                AssignedDate = assignment.AssignedDate,
                StartDate = codingTest.StartDate,
                EndDate = codingTest.EndDate,
                DurationMinutes = codingTest.DurationMinutes,
                TotalQuestions = codingTest.TotalQuestions,
                TotalMarks = codingTest.TotalMarks,
                TestType = assignment.TestType,
                TestMode = assignment.TestMode,
                Status = status,
                AssignedByName = "System", // You might want to get this from a user service
                SubjectName = subjectName,
                TopicName = topicName
            };
        }
    }
}
