using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LeetCodeCompiler.API.Services
{
    public class CodingTestService : ICodingTestService
    {
        private readonly AppDbContext _context;
        private readonly StudentProfileService _studentProfileService;
        private readonly IJudgeService _judgeService;
        private readonly ScoringOptions _scoringOptions;
        private readonly IProctoringService _proctoringService;
        private readonly IQuestionPoolService _questionPoolService;
        private readonly IIntegrityAnalysisService _integrityAnalysisService;
        private readonly IPlagiarismService _plagiarismService;
        private readonly IActivityTrackingService _activityTrackingService;

        public CodingTestService(
            AppDbContext context,
            StudentProfileService studentProfileService,
            IJudgeService judgeService,
            IOptions<ScoringOptions> scoringOptions,
            IProctoringService proctoringService,
            IQuestionPoolService questionPoolService,
            IIntegrityAnalysisService integrityAnalysisService,
            IPlagiarismService plagiarismService,
            IActivityTrackingService activityTrackingService)
        {
            _context = context;
            _studentProfileService = studentProfileService;
            _judgeService = judgeService;
            _scoringOptions = scoringOptions.Value;
            _proctoringService = proctoringService;
            _questionPoolService = questionPoolService;
            _integrityAnalysisService = integrityAnalysisService;
            _plagiarismService = plagiarismService;
            _activityTrackingService = activityTrackingService;
        }

        public async Task<CodingTestResponse> CreateCodingTestAsync(CreateCodingTestRequest request)
        {
            var problemIds = request.Questions.Select(q => q.ProblemId).Distinct().ToList();
            var problemDifficulties = await _context.Problems
                .AsNoTracking()
                .Where(p => problemIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Difficulty);

            var totalQuestionMarks = request.Questions.Sum(q =>
                q.Marks > 0
                    ? q.Marks
                    : _scoringOptions.GetDefaultMarks(
                        problemDifficulties.GetValueOrDefault(q.ProblemId, 2)));

            if (totalQuestionMarks != request.TotalMarks)
            {
                throw new ArgumentException(
                    $"Total marks ({request.TotalMarks}) must equal sum of question marks ({totalQuestionMarks})");
            }

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
                IsPublished = request.IsPublished,
                AccessCode = request.AccessCode,
                Tags = request.Tags,
                IsResultPublishAutomatically = request.IsResultPublishAutomatically,
                ApplyBreachRule = request.ApplyBreachRule,
                BreachRuleLimit = request.BreachRuleLimit,
                WarningThreshold = request.WarningThreshold,
                FlagThreshold = request.FlagThreshold,
                RequireFullscreen = request.RequireFullscreen,
                BlockPaste = request.BlockPaste,
                EnableProctoring = request.EnableProctoring,
                EnablePlagiarismCheck = request.EnablePlagiarismCheck,
                HostIP = request.HostIP,
                ClassId = request.ClassId,
                IsGlobal = request.IsGlobal,
                CollegeId = request.CollegeId, // Store as-is from frontend
                CreatedAt = DateTime.UtcNow
            };

            BreachThresholdHelper.ApplyBreachDefaults(codingTest);

            _context.CodingTests.Add(codingTest);
            await _context.SaveChangesAsync();

            // Add questions (default marks from difficulty when Marks <= 0)
            foreach (var questionRequest in request.Questions)
            {
                var marks = questionRequest.Marks > 0
                    ? questionRequest.Marks
                    : _scoringOptions.GetDefaultMarks(
                        problemDifficulties.GetValueOrDefault(questionRequest.ProblemId, 2));

                var question = new CodingTestQuestion
                {
                    CodingTestId = codingTest.Id,
                    ProblemId = questionRequest.ProblemId,
                    QuestionOrder = questionRequest.QuestionOrder,
                    Marks = marks,
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

            foreach (var poolSection in request.PoolSections)
            {
                _context.CodingTestPoolSections.Add(new CodingTestPoolSection
                {
                    CodingTestId = codingTest.Id,
                    PoolId = poolSection.PoolId,
                    QuestionsToPick = poolSection.QuestionsToPick,
                    SectionOrder = poolSection.SectionOrder,
                    MarksPerQuestion = poolSection.MarksPerQuestion,
                    TimeLimitMinutes = poolSection.TimeLimitMinutes,
                    CustomInstructions = poolSection.CustomInstructions
                });
            }

            await _context.SaveChangesAsync();
            return await GetCodingTestCreatedResponseAsync(codingTest.Id);
        }

        /// <summary>
        /// Lightweight load for create/update responses — avoids pulling full problem graphs
        /// (test cases, starter code) which can fail or time out behind production proxies.
        /// </summary>
        private async Task<CodingTestResponse> GetCodingTestCreatedResponseAsync(int id)
        {
            var codingTest = await _context.CodingTests
                .Include(ct => ct.Questions)
                .Include(ct => ct.TopicData)
                .Include(ct => ct.PoolSections)
                .Include(ct => ct.Attempts)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct => ct.Id == id);

            if (codingTest == null)
                throw new ArgumentException($"Coding test with ID {id} not found");

            return MapToResponse(codingTest);
        }


             public async Task<CombinedTestResultResponse> GetCombinedTestResultsAsync(long userId, int codingTestId)
{
    // Get the main submission data with JOIN queries
    var submissionData = await (from s in _context.CodingTestSubmissions
                               join ct in _context.CodingTests on s.CodingTestId equals ct.Id
                               where s.UserId == userId && s.CodingTestId == codingTestId
                               orderby s.SubmissionTime descending
                               select new
                               {
                                   Submission = s,
                                   Test = ct
                               }).FirstOrDefaultAsync();

    if (submissionData == null)
        throw new ArgumentException($"No submission found for user {userId} and test {codingTestId}");

    // Get assignment data with JOIN queries
    var assignmentData = await (from act in _context.AssignedCodingTests
                               join ct in _context.CodingTests on act.CodingTestId equals ct.Id
                               where act.AssignedToUserId == userId && act.CodingTestId == codingTestId && !act.IsDeleted
                               select new
                               {
                                   Assignment = act,
                                   Test = ct
                               }).FirstOrDefaultAsync();

    if (assignmentData == null)
        throw new ArgumentException($"No assignment found for user {userId} and test {codingTestId}");

    var submissionId = submissionData.Submission.SubmissionId;
    var attemptNumber = submissionData.Submission.AttemptNumber;

    var storedTestCaseResults = await _context.CodingTestSubmissionResults
        .Where(r => r.SubmissionId == submissionId)
        .OrderBy(r => r.ProblemId)
        .ThenBy(r => r.TestCaseOrder)
        .ToListAsync();

    var testCaseResultsByProblem = storedTestCaseResults
        .GroupBy(r => r.ProblemId)
        .ToDictionary(g => g.Key, g => g.ToList());

    // Get question attempts for this submission's attempt number
    var questionAttempts = await (from qa in _context.CodingTestQuestionAttempts
                                 join cta in _context.CodingTestAttempts on qa.CodingTestAttemptId equals cta.Id
                                 join ct in _context.CodingTests on cta.CodingTestId equals ct.Id
                                 join cq in _context.CodingTestQuestions on qa.CodingTestQuestionId equals cq.Id
                                 join p in _context.Problems on cq.ProblemId equals p.Id into problemJoin
                                 from p in problemJoin.DefaultIfEmpty()
                                 where qa.UserId == userId
                                       && cta.CodingTestId == codingTestId
                                       && cta.AttemptNumber == attemptNumber
                                 select new
                                 {
                                     QuestionAttempt = qa,
                                     TestQuestion = cq,
                                     Problem = p
                                 }).ToListAsync();

    var questionSubmissionResponses = new List<QuestionSubmissionResponse>();

    // Group by problem to get question submissions
    var problemGroups = questionAttempts.GroupBy(qa => qa.QuestionAttempt.ProblemId);

    foreach (var problemGroup in problemGroups)
    {
        var problemId = problemGroup.Key;
        var attempts = problemGroup.ToList();
        var problem = attempts.First().Problem;

        // Get the best question attempt (latest or most complete)
        var bestAttempt = attempts
            .OrderByDescending(a => a.QuestionAttempt.UpdatedAt ?? a.QuestionAttempt.CreatedAt)
            .First().QuestionAttempt;

        var testCaseResults = testCaseResultsByProblem.TryGetValue(problemId, out var stored)
            ? stored.Select(MapStoredToTestCaseSubmissionResult).ToList()
            : new List<TestCaseSubmissionResult>();

        questionSubmissionResponses.Add(new QuestionSubmissionResponse
        {
            ProblemId = problemId,
            ProblemTitle = problem?.Title ?? "Unknown Problem",
            LanguageUsed = bestAttempt.LanguageUsed,
            TotalTestCases = bestAttempt.TotalTestCases,
            PassedTestCases = bestAttempt.TestCasesPassed,
            FailedTestCases = bestAttempt.TotalTestCases - bestAttempt.TestCasesPassed,
            Score = bestAttempt.Score,
            MaxScore = bestAttempt.MaxScore,
            IsCorrect = bestAttempt.IsCorrect,
            TestCaseResults = testCaseResults
        });
    }

    // Prefer authoritative totals from the submission row when present (whole-test submit)
    var submission = submissionData.Submission;
    var totalScore = submission.MaxScore > 0
        ? submission.Score
        : questionSubmissionResponses.Sum(q => q.Score);
    var maxScore = submission.MaxScore > 0
        ? submission.MaxScore
        : questionSubmissionResponses.Sum(q => q.MaxScore);
    var percentage = maxScore > 0 ? (double)totalScore / (double)maxScore * 100 : 0;

    // Calculate additional statistics
    var totalProblems = questionSubmissionResponses.Count;
    var correctProblems = questionSubmissionResponses.Count(q => q.IsCorrect);
    var totalTestCases = questionSubmissionResponses.Sum(q => q.TotalTestCases);
    var correctTestCases = questionSubmissionResponses.Sum(q => q.PassedTestCases);
    var problemAccuracy = totalProblems > 0 ? (correctProblems * 100.0) / totalProblems : 0;
    var testCaseAccuracy = totalTestCases > 0 ? (correctTestCases * 100.0) / totalTestCases : 0;

    return new CombinedTestResultResponse
    {
        // From submit-whole-test response
        SubmissionId = submissionData.Submission.SubmissionId,
        CodingTestId = submissionData.Submission.CodingTestId,
        TestName = submissionData.Test.TestName,
        UserId = (int)submissionData.Submission.UserId,
        AttemptNumber = submissionData.Submission.AttemptNumber,
        TotalQuestions = totalProblems,
        TotalScore = totalScore,
        MaxScore = maxScore,
        Percentage = percentage,
        IsLateSubmission = submissionData.Submission.IsLateSubmission,
        SubmissionTime = submissionData.Submission.SubmissionTime,
        QuestionSubmissions = questionSubmissionResponses,
        CreatedAt = submissionData.Submission.CreatedAt,

        // From end-test response
        AssignedId = assignmentData.Assignment.AssignedId,
        AssignedDate = assignmentData.Assignment.AssignedDate,
        StartedAt = assignmentData.Assignment.StartedAt,
        CompletedAt = assignmentData.Assignment.CompletedAt,
        TimeSpentMinutes = assignmentData.Assignment.TimeSpentMinutes,
        Status = assignmentData.Assignment.Status,
        StartDate = assignmentData.Test.StartDate,
        EndDate = assignmentData.Test.EndDate,
        DurationMinutes = assignmentData.Test.DurationMinutes,
        TotalMarks = assignmentData.Test.TotalMarks,
        CanStart = false, // Based on current status
        CanEnd = false,   // Based on current status
        IsExpired = DateTime.UtcNow > assignmentData.Test.EndDate,
        Message = "", // Can add logic for messages

        // Additional Statistics
        TotalProblems = totalProblems,
        CorrectProblems = correctProblems,
        TotalTestCases = totalTestCases,
        CorrectTestCases = correctTestCases,
        ProblemAccuracy = problemAccuracy,
        TestCaseAccuracy = testCaseAccuracy,
        // Official score: same as TotalScore (backward-compatible field for clients reading FinalScore)
        FinalScore = (double)totalScore
    };
}
















            
        public async Task<CodingTestResponse> GetCodingTestByIdAsync(int id)
        {
            var codingTest = await _context.CodingTests
                .Include(ct => ct.TopicData)
                .Include(ct => ct.PoolSections)
                .Include(ct => ct.Questions)
                    .ThenInclude(q => q.Problem)
                        .ThenInclude(p => p.TestCases)
                .Include(ct => ct.Questions)
                    .ThenInclude(q => q.Problem)
                        .ThenInclude(p => p.StarterCodes)
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

        public async Task<PagedResult<CodingTestSummaryResponse>> GetAllCodingTestsPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.CodingTests
                .Include(ct => ct.Attempts);
                
            var totalCount = await query.CountAsync();
            
            var codingTests = await query
                .OrderByDescending(ct => ct.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<CodingTestSummaryResponse>
            {
                Items = codingTests.Select(ct => MapToSummaryResponse(ct)).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
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

        public async Task<PagedResult<CodingTestSummaryResponse>> GetCodingTestsByUserPagedAsync(int userId, int pageNumber, int pageSize, string? subjectName = null, string? topicName = null, bool isEnabled = true)
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
            
            var totalCount = await query.CountAsync();

            var codingTests = await query
                .OrderByDescending(ct => ct.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<CodingTestSummaryResponse>
            {
                Items = codingTests.Select(ct => MapToSummaryResponse(ct, subjectName, topicName, isEnabled)).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<CodingTestFullResponse>> GetCodingTestsByCreatorAsync(int createdByUserId)
        {
            var codingTests = await _context.CodingTests
                .Where(ct => ct.CreatedBy == createdByUserId)
                .OrderByDescending(ct => ct.CreatedAt)
                .ToListAsync();

            return codingTests.Select(ct => new CodingTestFullResponse
            {
                Id = ct.Id,
                TestName = ct.TestName,
                CreatedBy = ct.CreatedBy,
                CreatedAt = ct.CreatedAt,
                UpdatedAt = ct.UpdatedAt,
                StartDate = ct.StartDate,
                EndDate = ct.EndDate,
                DurationMinutes = ct.DurationMinutes,
                TotalQuestions = ct.TotalQuestions,
                TotalMarks = ct.TotalMarks,
                IsActive = ct.IsActive,
                IsPublished = ct.IsPublished,
                AllowMultipleAttempts = ct.AllowMultipleAttempts,
                MaxAttempts = ct.MaxAttempts,
                ShowResultsImmediately = ct.ShowResultsImmediately,
                AllowCodeReview = ct.AllowCodeReview,
                AccessCode = ct.AccessCode,
                Tags = ct.Tags,
                IsResultPublishAutomatically = ct.IsResultPublishAutomatically,
                ApplyBreachRule = ct.ApplyBreachRule,
                BreachRuleLimit = ct.BreachRuleLimit,
                HostIP = ct.HostIP,
                ClassId = ct.ClassId,
                IsGlobal = ct.IsGlobal,
                CollegeId = ct.CollegeId,
                TestType = ct.TestType
            }).ToList();
        }

        public async Task<PagedResult<CodingTestFullResponse>> GetCodingTestsByCreatorPagedAsync(int createdByUserId, int pageNumber, int pageSize)
        {
            var query = _context.CodingTests
                .Where(ct => ct.CreatedBy == createdByUserId);
                
            var totalCount = await query.CountAsync();
            
            var codingTests = await query
                .OrderByDescending(ct => ct.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<CodingTestFullResponse>
            {
                Items = codingTests.Select(ct => new CodingTestFullResponse
                {
                    Id = ct.Id,
                    TestName = ct.TestName,
                    CreatedBy = ct.CreatedBy,
                    CreatedAt = ct.CreatedAt,
                    UpdatedAt = ct.UpdatedAt,
                    StartDate = ct.StartDate,
                    EndDate = ct.EndDate,
                    DurationMinutes = ct.DurationMinutes,
                    TotalQuestions = ct.TotalQuestions,
                    TotalMarks = ct.TotalMarks,
                    IsActive = ct.IsActive,
                    IsPublished = ct.IsPublished,
                    AllowMultipleAttempts = ct.AllowMultipleAttempts,
                    MaxAttempts = ct.MaxAttempts,
                    ShowResultsImmediately = ct.ShowResultsImmediately,
                    AllowCodeReview = ct.AllowCodeReview,
                    AccessCode = ct.AccessCode,
                    Tags = ct.Tags,
                    IsResultPublishAutomatically = ct.IsResultPublishAutomatically,
                    ApplyBreachRule = ct.ApplyBreachRule,
                    BreachRuleLimit = ct.BreachRuleLimit,
                    HostIP = ct.HostIP,
                    ClassId = ct.ClassId,
                    IsGlobal = ct.IsGlobal,
                    CollegeId = ct.CollegeId,
                    TestType = ct.TestType
                }).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Gets tests available to a college (global tests + tests specific to that college)
        /// </summary>
        public async Task<List<CodingTestSummaryResponse>> GetGlobalCodingTestsByCollegeIdAsync(int collegeId)
        {
            var codingTests = await _context.CodingTests
                .Include(ct => ct.Attempts)
                .Where(ct => ct.IsGlobal || ct.CollegeId == collegeId)
                .OrderByDescending(ct => ct.CreatedAt)
                .ToListAsync();

            return codingTests.Select(ct => MapToSummaryResponse(ct)).ToList();
        }

        public async Task<PagedResult<CodingTestSummaryResponse>> GetGlobalCodingTestsByCollegeIdPagedAsync(int collegeId, int pageNumber, int pageSize)
        {
            var query = _context.CodingTests
                .Include(ct => ct.Attempts)
                .Where(ct => ct.IsGlobal || ct.CollegeId == collegeId);
                
            var totalCount = await query.CountAsync();
            
            var codingTests = await query
                .OrderByDescending(ct => ct.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<CodingTestSummaryResponse>
            {
                Items = codingTests.Select(ct => MapToSummaryResponse(ct)).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Gets all global coding tests (IsGlobal = true)
        /// </summary>
        public async Task<List<CodingTestSummaryResponse>> GetAllGlobalCodingTestsAsync()
        {
            var codingTests = await _context.CodingTests
                .Include(ct => ct.Attempts)
                .Where(ct => ct.IsGlobal)
                .OrderByDescending(ct => ct.CreatedAt)
                .ToListAsync();

            return codingTests.Select(ct => MapToSummaryResponse(ct)).ToList();
        }

        public async Task<PagedResult<CodingTestSummaryResponse>> GetAllGlobalCodingTestsPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.CodingTests
                .Include(ct => ct.Attempts)
                .Where(ct => ct.IsGlobal);
                
            var totalCount = await query.CountAsync();
            
            var codingTests = await query
                .OrderByDescending(ct => ct.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<CodingTestSummaryResponse>
            {
                Items = codingTests.Select(ct => MapToSummaryResponse(ct)).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Gets all tests specific to a college (CollegeId = collegeId, not global)
        /// </summary>
        public async Task<List<CodingTestSummaryResponse>> GetCodingTestsByCollegeIdAsync(int collegeId)
        {
            var codingTests = await _context.CodingTests
                .Include(ct => ct.Attempts)
                .Where(ct => ct.CollegeId == collegeId && !ct.IsGlobal)
                .OrderByDescending(ct => ct.CreatedAt)
                .ToListAsync();

            return codingTests.Select(ct => MapToSummaryResponse(ct)).ToList();
        }

        public async Task<PagedResult<CodingTestSummaryResponse>> GetCodingTestsByCollegeIdPagedAsync(int collegeId, int pageNumber, int pageSize)
        {
            var query = _context.CodingTests
                .Include(ct => ct.Attempts)
                .Where(ct => ct.CollegeId == collegeId && !ct.IsGlobal);
                
            var totalCount = await query.CountAsync();
            
            var codingTests = await query
                .OrderByDescending(ct => ct.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<CodingTestSummaryResponse>
            {
                Items = codingTests.Select(ct => MapToSummaryResponse(ct)).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Gets only global tests for a particular college (IsGlobal = true AND CollegeId = collegeId)
        /// </summary>
        public async Task<List<CodingTestSummaryResponse>> GetGlobalTestsByCollegeIdAsync(int collegeId)
        {
            var codingTests = await _context.CodingTests
                .Include(ct => ct.Attempts)
                .Where(ct => ct.IsGlobal && ct.CollegeId == collegeId)
                .OrderByDescending(ct => ct.CreatedAt)
                .ToListAsync();

            return codingTests.Select(ct => MapToSummaryResponse(ct)).ToList();
        }

        public async Task<PagedResult<CodingTestSummaryResponse>> GetGlobalTestsByCollegeIdPagedAsync(int collegeId, int pageNumber, int pageSize)
        {
            var query = _context.CodingTests
                .Include(ct => ct.Attempts)
                .Where(ct => ct.IsGlobal && ct.CollegeId == collegeId);
                
            var totalCount = await query.CountAsync();
            
            var codingTests = await query
                .OrderByDescending(ct => ct.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<CodingTestSummaryResponse>
            {
                Items = codingTests.Select(ct => MapToSummaryResponse(ct)).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<CodingTestSummaryResponse>> GetCodingTestsByFilterPagedAsync(CodingTestFilterRequest request)
        {
            var query = _context.CodingTests
                .Include(ct => ct.Attempts)
                .AsQueryable();

            if (request.CodingTestId.HasValue)
            {
                query = query.Where(ct => ct.Id == request.CodingTestId.Value);
            }

            if (request.CreatedBy.HasValue)
            {
                query = query.Where(ct => ct.CreatedBy == request.CreatedBy.Value);
            }

            if (request.ClassId.HasValue)
            {
                query = query.Where(ct => ct.ClassId == request.ClassId.Value);
            }

            if (request.CollegeId.HasValue)
            {
                query = query.Where(ct => ct.CollegeId == request.CollegeId.Value);
            }

            if (request.AssignedToUserId.HasValue)
            {
                var assignedTestIds = _context.AssignedCodingTests
                    .Where(act => act.AssignedToUserId == request.AssignedToUserId.Value && !act.IsDeleted)
                    .Select(act => act.CodingTestId)
                    .Distinct();
                    
                query = query.Where(ct => assignedTestIds.Contains(ct.Id));
            }

            var totalCount = await query.CountAsync();
            
            var codingTests = await query
                .OrderByDescending(ct => ct.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<CodingTestSummaryResponse>
            {
                Items = codingTests.Select(ct => MapToSummaryResponse(ct)).ToList(),
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<CodingTestResponse> UpdateCodingTestAsync(UpdateCodingTestRequest request)
        {
            var codingTest = await _context.CodingTests
                .Include(ct => ct.Questions)
                .FirstOrDefaultAsync(ct => ct.Id == request.Id);
            
            if (codingTest == null)
                throw new ArgumentException($"Coding test with ID {request.Id} not found");

            // Handle IsGlobal and CollegeId updates
            // Store CollegeId as-is from frontend, only use default (0) if not provided
            if (request.IsGlobal.HasValue || request.CollegeId.HasValue)
            {
                if (request.IsGlobal.HasValue) codingTest.IsGlobal = request.IsGlobal.Value;
                if (request.CollegeId.HasValue) codingTest.CollegeId = request.CollegeId.Value;
            }

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
            if (request.ApplyBreachRule.HasValue) codingTest.ApplyBreachRule = request.ApplyBreachRule.Value;
            if (request.BreachRuleLimit.HasValue) codingTest.BreachRuleLimit = request.BreachRuleLimit.Value;
            if (request.EnableProctoring.HasValue) codingTest.EnableProctoring = request.EnableProctoring.Value;

            if (request.BreachRuleLimit.HasValue)
                BreachThresholdHelper.ApplyBreachDefaults(codingTest);

            // Handle question updates if provided
            if (request.Questions != null && request.Questions.Any())
            {
                await UpdateCodingTestQuestionsAsync(codingTest.Id, request.Questions);
            }

            codingTest.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await ValidateCodingTestMarksAlignmentAsync(codingTest.Id);

            return await GetCodingTestByIdAsync(codingTest.Id);
        }

        private async Task UpdateCodingTestQuestionsAsync(int codingTestId, List<QuestionUpdateRequest> questionUpdates)
        {
            foreach (var questionUpdate in questionUpdates)
            {
                if (questionUpdate.IsDeleted == true && questionUpdate.Id.HasValue)
                {
                    // Delete existing question
                    var questionToDelete = await _context.CodingTestQuestions
                        .FirstOrDefaultAsync(q => q.Id == questionUpdate.Id.Value && q.CodingTestId == codingTestId);
                    
                    if (questionToDelete != null)
                    {
                        _context.CodingTestQuestions.Remove(questionToDelete);
                    }
                }
                else if (questionUpdate.Id.HasValue)
                {
                    // Update existing question
                    var existingQuestion = await _context.CodingTestQuestions
                        .FirstOrDefaultAsync(q => q.Id == questionUpdate.Id.Value && q.CodingTestId == codingTestId);
                    
                    if (existingQuestion != null)
                    {
                        if (questionUpdate.ProblemId.HasValue) existingQuestion.ProblemId = questionUpdate.ProblemId.Value;
                        if (questionUpdate.QuestionOrder.HasValue) existingQuestion.QuestionOrder = questionUpdate.QuestionOrder.Value;
                        if (questionUpdate.Marks.HasValue) existingQuestion.Marks = questionUpdate.Marks.Value;
                        if (questionUpdate.TimeLimitMinutes.HasValue) existingQuestion.TimeLimitMinutes = questionUpdate.TimeLimitMinutes.Value;
                        if (questionUpdate.CustomInstructions != null) existingQuestion.CustomInstructions = questionUpdate.CustomInstructions;
                    }
                }
                else if (questionUpdate.ProblemId.HasValue)
                {
                    // Add new question
                    var newQuestion = new CodingTestQuestion
                    {
                        CodingTestId = codingTestId,
                        ProblemId = questionUpdate.ProblemId.Value,
                        QuestionOrder = questionUpdate.QuestionOrder ?? 1,
                        Marks = questionUpdate.Marks ?? 10m,
                        TimeLimitMinutes = questionUpdate.TimeLimitMinutes ?? 30,
                        CustomInstructions = questionUpdate.CustomInstructions ?? "",
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    _context.CodingTestQuestions.Add(newQuestion);
                }
            }
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
            var codingTest = await _context.CodingTests.FindAsync(request.CodingTestId);
            if (codingTest == null)
                throw new InvalidOperationException("Coding test not found");

            var existingAttempt = await _context.CodingTestAttempts
                .FirstOrDefaultAsync(cta => cta.CodingTestId == request.CodingTestId &&
                                          cta.UserId == request.UserId &&
                                          cta.Status == "InProgress");

            if (existingAttempt != null)
                return await GetCodingTestAttemptAsync(existingAttempt.Id);

            var pendingGrant = await GetPendingResumeGrantAsync(request.UserId, request.CodingTestId);
            var canStart = pendingGrant != null
                ? await CanUserResumeTestAsync(request.UserId, request.CodingTestId, request.AccessCode, pendingGrant)
                : await CanUserAttemptTestAsync(request.UserId, request.CodingTestId, request.AccessCode);

            if (!canStart)
                throw new InvalidOperationException("User cannot attempt this test");

            var attempt = new CodingTestAttempt
            {
                CodingTestId = request.CodingTestId,
                UserId = request.UserId,
                AttemptNumber = await GetNextAttemptNumber(request.UserId, request.CodingTestId),
                StartedAt = DateTime.UtcNow,
                Status = "InProgress",
                IntegrityStatus = "Normal",
                CreatedAt = DateTime.UtcNow
            };

            if (pendingGrant != null)
            {
                attempt.ParentAttemptId = pendingGrant.PriorAttemptId;
                attempt.AllowedEndAt = pendingGrant.AllowedEndAt;
                pendingGrant.Status = ResumeGrantStatuses.Used;
                pendingGrant.UsedByAttemptId = null;
            }

            _context.CodingTestAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            if (pendingGrant != null)
            {
                pendingGrant.UsedByAttemptId = attempt.Id;
                await _context.SaveChangesAsync();
            }

            await _questionPoolService.CreateAttemptQuestionSnapshotAsync(attempt);

            if (codingTest.EnableProctoring)
                await _proctoringService.StartSessionAsync(attempt.Id);

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

            return await BuildAttemptResponseAsync(attempt);
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

            var results = new List<CodingTestAttemptResponse>();
            foreach (var attempt in attempts)
                results.Add(await BuildAttemptResponseAsync(attempt));
            return results;
        }

        public async Task<SubmitCodingTestResponse> SubmitCodingTestAsync(SubmitCodingTestRequest request)
        {
            CodingTestQuestion? codingTestQuestion = null;
            CodingTestAttemptQuestion? attemptQuestion = null;
            int targetCodingTestId;

            // Determine which coding test to use
            if (request.CodingTestId.HasValue)
            {
                targetCodingTestId = request.CodingTestId.Value;

                var inProgressAttempt = await _context.CodingTestAttempts
                    .FirstOrDefaultAsync(a => a.UserId == request.UserId
                                           && a.CodingTestId == targetCodingTestId
                                           && (a.Status == "InProgress" || a.Status == "Started"));

                if (inProgressAttempt != null)
                {
                    var resolved = await _questionPoolService.ResolveQuestionForAttemptAsync(
                        inProgressAttempt.Id, request.ProblemId);
                    if (!resolved.IsAllowed)
                        throw new ArgumentException($"Problem {request.ProblemId} is not part of this test attempt");
                    attemptQuestion = resolved.Snapshot;
                    codingTestQuestion = resolved.FixedQuestion;
                }
                else
                {
                    codingTestQuestion = await _context.CodingTestQuestions
                        .Include(ctq => ctq.CodingTest)
                        .FirstOrDefaultAsync(ctq => ctq.ProblemId == request.ProblemId && ctq.CodingTestId == targetCodingTestId);

                    if (codingTestQuestion == null)
                        throw new ArgumentException($"Problem {request.ProblemId} is not part of coding test {targetCodingTestId}");
                }
            }
            else
            {
                // Find the assigned coding test for this user and problem
                var assignedTest = await _context.AssignedCodingTests
                    .Include(act => act.CodingTest)
                        .ThenInclude(ct => ct.Questions)
                    .Where(act => act.AssignedToUserId == request.UserId && !act.IsDeleted)
                    .FirstOrDefaultAsync(act => act.CodingTest.Questions.Any(q => q.ProblemId == request.ProblemId));

                if (assignedTest != null)
                {
                    targetCodingTestId = assignedTest.CodingTestId;
                    codingTestQuestion = await _context.CodingTestQuestions
                        .Include(ctq => ctq.CodingTest)
                        .FirstOrDefaultAsync(ctq => ctq.ProblemId == request.ProblemId && ctq.CodingTestId == targetCodingTestId);
                }
                else
                {
                    // Fallback: Use any coding test that contains this problem
                    codingTestQuestion = await _context.CodingTestQuestions
                        .Include(ctq => ctq.CodingTest)
                        .FirstOrDefaultAsync(ctq => ctq.ProblemId == request.ProblemId);

                    if (codingTestQuestion == null)
                        throw new ArgumentException($"Problem {request.ProblemId} is not part of any coding test");

                    targetCodingTestId = codingTestQuestion.CodingTestId;
                }
            }

            // Find or create the coding test attempt for this user
            CodingTestAttempt? attempt = null;
            if (request.CodingTestAttemptId.HasValue)
            {
                attempt = await _context.CodingTestAttempts
                    .Include(cta => cta.CodingTest)
                    .Include(cta => cta.QuestionAttempts)
                        .ThenInclude(qa => qa.CodingTestQuestion)
                    .FirstOrDefaultAsync(cta => cta.Id == request.CodingTestAttemptId.Value
                                             && cta.UserId == request.UserId);

                if (attempt == null)
                    throw new ArgumentException($"Attempt {request.CodingTestAttemptId} not found for user {request.UserId}");

                targetCodingTestId = attempt.CodingTestId;
            }

            attempt ??= await _context.CodingTestAttempts
                .Include(cta => cta.CodingTest)
                .Include(cta => cta.QuestionAttempts)
                    .ThenInclude(qa => qa.CodingTestQuestion)
                .FirstOrDefaultAsync(cta => cta.UserId == request.UserId && 
                                          cta.CodingTestId == targetCodingTestId &&
                                          (cta.Status == "InProgress" || cta.Status == "Started"));

            // If no attempt exists, create one automatically
            if (attempt == null)
            {
                attempt = new CodingTestAttempt
                {
                    CodingTestId = targetCodingTestId,
                    UserId = request.UserId,
                    AttemptNumber = request.AttemptNumber,
                    StartedAt = DateTime.UtcNow,
                    Status = "InProgress",
                    CreatedAt = DateTime.UtcNow
                };
                _context.CodingTestAttempts.Add(attempt);
                await _context.SaveChangesAsync();

                // Load the test for the attempt
                attempt.CodingTest = codingTestQuestion!.CodingTest;

                // Update the AssignedCodingTests status to "InProgress" when test is started
                var startAssignment = await _context.AssignedCodingTests
                    .FirstOrDefaultAsync(act => act.AssignedToUserId == request.UserId && 
                                              act.CodingTestId == targetCodingTestId &&
                                              !act.IsDeleted);

                if (startAssignment != null && startAssignment.Status == "Assigned")
                {
                    startAssignment.Status = "InProgress";
                    startAssignment.StartedAt = attempt.StartedAt;
                    startAssignment.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            if (attemptQuestion == null && attempt.QuestionSnapshotCreated)
            {
                attemptQuestion = await _context.CodingTestAttemptQuestions
                    .FirstOrDefaultAsync(q => q.CodingTestAttemptId == attempt.Id && q.ProblemId == request.ProblemId);
            }

            if (codingTestQuestion == null && attemptQuestion?.CodingTestQuestionId != null)
            {
                codingTestQuestion = await _context.CodingTestQuestions
                    .Include(ctq => ctq.CodingTest)
                    .FirstOrDefaultAsync(ctq => ctq.Id == attemptQuestion.CodingTestQuestionId);
            }

            // Find or create the specific question attempt for this problem
            var questionAttempt = attempt.QuestionAttempts
                .FirstOrDefault(qa => qa.ProblemId == request.ProblemId);

            if (questionAttempt == null)
            {
                questionAttempt = new CodingTestQuestionAttempt
                {
                    CodingTestAttemptId = attempt.Id,
                    CodingTestQuestionId = codingTestQuestion?.Id,
                    CodingTestAttemptQuestionId = attemptQuestion?.Id,
                    ProblemId = request.ProblemId,
                    UserId = request.UserId,
                    StartedAt = DateTime.UtcNow,
                    Status = "InProgress",
                    LanguageUsed = request.LanguageUsed,
                    CodeSubmitted = "",
                    CreatedAt = DateTime.UtcNow
                };
                _context.CodingTestQuestionAttempts.Add(questionAttempt);
                await _context.SaveChangesAsync();

                if (codingTestQuestion != null)
                    questionAttempt.CodingTestQuestion = codingTestQuestion;
            }

            var judgeResult = await _judgeService.EvaluateAsync(
                request.ProblemId, request.LanguageUsed, request.FinalCodeSnapshot);

            // Create the submission record (authoritative counts from server-side judge)
            var submission = new CodingTestSubmission
            {
                CodingTestId = attempt.CodingTestId,
                CodingTestAttemptId = attempt.Id,
                CodingTestQuestionAttemptId = questionAttempt.Id,
                ProblemId = request.ProblemId,
                UserId = request.UserId,
                AttemptNumber = request.AttemptNumber,
                LanguageUsed = request.LanguageUsed,
                FinalCodeSnapshot = request.FinalCodeSnapshot,
                TotalTestCases = judgeResult.TotalTestCases,
                PassedTestCases = judgeResult.PassedTestCases,
                FailedTestCases = judgeResult.FailedTestCases,
                RequestedHelp = request.RequestedHelp,
                LanguageSwitchCount = request.LanguageSwitchCount,
                RunClickCount = request.RunClickCount,
                SubmitClickCount = request.SubmitClickCount,
                EraseCount = request.EraseCount,
                SaveCount = request.SaveCount,
                LoginLogoutCount = request.LoginLogoutCount,
                IsSessionAbandoned = request.IsSessionAbandoned,
                ClassId = request.ClassId,
                SubmissionTime = DateTime.UtcNow,
                IsLateSubmission = attempt.CodingTest != null && DateTime.UtcNow > attempt.CodingTest.EndDate,
                CreatedAt = DateTime.UtcNow,
                ExecutionTimeMs = judgeResult.ExecutionTimeMs,
                MemoryUsedKB = judgeResult.MemoryUsedKB
            };

            var maxScore = attemptQuestion?.Marks ?? codingTestQuestion?.Marks ?? questionAttempt.MaxScore;
            var score = ScoreCalculator.DecimalScore(
                judgeResult.PassedTestCases, judgeResult.TotalTestCases, maxScore);

            submission.Score = score;
            submission.MaxScore = maxScore;
            submission.IsCorrect = judgeResult.IsCorrect;

            _context.CodingTestSubmissions.Add(submission);

            // Update the question attempt with the submitted code and results
            questionAttempt.Status = "Completed";
            questionAttempt.CompletedAt = DateTime.UtcNow;
            questionAttempt.LanguageUsed = request.LanguageUsed;
            questionAttempt.CodeSubmitted = request.FinalCodeSnapshot;
            questionAttempt.TestCasesPassed = judgeResult.PassedTestCases;
            questionAttempt.TotalTestCases = judgeResult.TotalTestCases;
            questionAttempt.RunCount = request.RunClickCount;
            questionAttempt.SubmitCount = request.SubmitClickCount;
            questionAttempt.IsCorrect = submission.IsCorrect;
            questionAttempt.Score = score;
            questionAttempt.MaxScore = maxScore;
            questionAttempt.UpdatedAt = DateTime.UtcNow;

            // Update the overall test attempt
            var totalScore = attempt.QuestionAttempts.Sum(qa => qa.Score);
            var totalMaxScore = attempt.QuestionAttempts.Sum(qa => qa.MaxScore);
            var percentage = totalMaxScore > 0 ? (double)totalScore / (double)totalMaxScore * 100 : 0;

            attempt.Status = "Submitted";
            attempt.SubmittedAt = DateTime.UtcNow;
            attempt.CompletedAt = DateTime.UtcNow;
            attempt.TotalScore = totalScore;
            attempt.MaxScore = totalMaxScore;
            attempt.Percentage = percentage;
            attempt.TimeSpentMinutes = (int)(DateTime.UtcNow - attempt.StartedAt).TotalMinutes;
            attempt.IsLateSubmission = submission.IsLateSubmission;
            attempt.UpdatedAt = DateTime.UtcNow;

            if (string.IsNullOrEmpty(attempt.SubmissionReason))
                attempt.SubmissionReason = SubmissionReasons.Manual;

            // Update the AssignedCodingTests status if this is an assigned test
            var assignment = await _context.AssignedCodingTests
                .FirstOrDefaultAsync(act => act.AssignedToUserId == request.UserId && 
                                          act.CodingTestId == targetCodingTestId &&
                                          !act.IsDeleted);

            if (assignment != null)
            {
                // Update assignment status based on attempt status
                assignment.Status = attempt.Status; // "Submitted"
                assignment.StartedAt = assignment.StartedAt ?? attempt.StartedAt;
                assignment.CompletedAt = attempt.CompletedAt;
                assignment.IsLateSubmission = attempt.IsLateSubmission;
                assignment.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            await ApplyActiveTimeToAttemptAsync(attempt, assignment);
            await _context.SaveChangesAsync();

            var passedIds = string.Join(",", judgeResult.TestCaseResults.Where(r => r.IsPassed).Select(r => r.TestCaseId));
            var failedIds = string.Join(",", judgeResult.TestCaseResults.Where(r => !r.IsPassed).Select(r => r.TestCaseId));
            await _activityTrackingService.CompleteAssessmentSessionAsync(
                request.UserId,
                request.ProblemId,
                attempt.Id,
                submission.SubmissionId,
                new AssessmentActivityMetrics
                {
                    TimeTakenSeconds = (int)(DateTime.UtcNow - questionAttempt.StartedAt).TotalSeconds,
                    LanguageSwitchCount = request.LanguageSwitchCount,
                    RunClickCount = request.RunClickCount,
                    SubmitClickCount = request.SubmitClickCount,
                    EraseCount = request.EraseCount,
                    SaveCount = request.SaveCount,
                    LoginLogoutCount = request.LoginLogoutCount,
                    IsSessionAbandoned = request.IsSessionAbandoned,
                    PassedTestCaseIDs = passedIds,
                    FailedTestCaseIDs = failedIds,
                    StartTime = questionAttempt.StartedAt,
                    EndTime = DateTime.UtcNow
                },
                "submit");

            await _integrityAnalysisService.RunSubmitHeuristicsAsync(
                attempt.Id,
                submission.SubmissionId,
                request.ProblemId,
                request.FinalCodeSnapshot,
                judgeResult.IsCorrect,
                attempt.StartedAt);

            if (attempt.CodingTest?.EnablePlagiarismCheck == true)
            {
                _plagiarismService.EnqueueCheck(new PlagiarismCheckJob(
                    submission.SubmissionId,
                    attempt.CodingTestId,
                    request.ProblemId,
                    attempt.Id));
            }

            // Get the problem details for response
            var problem = await _context.Problems.FindAsync(request.ProblemId);

            return new SubmitCodingTestResponse
            {
                SubmissionId = submission.SubmissionId,
                CodingTestId = attempt.CodingTestId,
                TestName = attempt.CodingTest?.TestName ?? "Unknown Test",
                ProblemId = request.ProblemId,
                ProblemTitle = problem?.Title ?? "Unknown Problem",
                UserId = request.UserId,
                AttemptNumber = request.AttemptNumber,
                LanguageUsed = request.LanguageUsed,
                TotalTestCases = judgeResult.TotalTestCases,
                PassedTestCases = judgeResult.PassedTestCases,
                FailedTestCases = judgeResult.FailedTestCases,
                Score = score,
                MaxScore = maxScore,
                IsCorrect = judgeResult.IsCorrect,
                IsLateSubmission = submission.IsLateSubmission,
                SubmissionTime = DateTime.UtcNow,
                ExecutionTimeMs = judgeResult.ExecutionTimeMs,
                MemoryUsedKB = judgeResult.MemoryUsedKB,
                ErrorMessage = null,
                ErrorType = null,
                TestCaseResults = MapJudgeToSubmissionTestCaseResults(judgeResult),
                CreatedAt = DateTime.UtcNow
            };
        }

        public async Task<SubmitWholeCodingTestResponse> SubmitWholeCodingTestAsync(SubmitWholeCodingTestRequest request)
        {
            // Validate the coding test exists
            var codingTest = await _context.CodingTests
                .Include(ct => ct.Questions)
                    .ThenInclude(q => q.Problem)
                .FirstOrDefaultAsync(ct => ct.Id == request.CodingTestId);

            if (codingTest == null)
                throw new ArgumentException($"Coding test with ID {request.CodingTestId} not found");

            // Find or create the coding test attempt for this user
            var attempt = await _context.CodingTestAttempts
                .Include(cta => cta.CodingTest)
                .Include(cta => cta.QuestionAttempts)
                    .ThenInclude(qa => qa.CodingTestQuestion)
                        .ThenInclude(ctq => ctq.Problem)
                .FirstOrDefaultAsync(cta => cta.UserId == request.UserId && 
                                          cta.CodingTestId == request.CodingTestId &&
                                          cta.AttemptNumber == request.AttemptNumber);

            // If no attempt exists, create one automatically
            if (attempt == null)
            {
                attempt = new CodingTestAttempt
                {
                    CodingTestId = request.CodingTestId,
                    UserId = request.UserId,
                    AttemptNumber = request.AttemptNumber,
                    StartedAt = DateTime.UtcNow.AddMinutes(-request.TotalTimeSpentMinutes), // Back-calculate start time
                    Status = "InProgress",
                    CreatedAt = DateTime.UtcNow
                };
                _context.CodingTestAttempts.Add(attempt);
                await _context.SaveChangesAsync();

                // Load the test for the attempt
                attempt.CodingTest = codingTest;

                // Update the AssignedCodingTests status to "InProgress" when test is started
                var startAssignment = await _context.AssignedCodingTests
                    .FirstOrDefaultAsync(act => act.AssignedToUserId == request.UserId && 
                                              act.CodingTestId == request.CodingTestId &&
                                              !act.IsDeleted);

                if (startAssignment != null && startAssignment.Status == "Assigned")
                {
                    startAssignment.Status = "InProgress";
                    startAssignment.StartedAt = attempt.StartedAt;
                    startAssignment.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            // Create a combined code snapshot for the whole test
            var combinedCodeSnapshot = string.Join("\n\n--- QUESTION SEPARATOR ---\n\n",
                request.QuestionSubmissions.Select((q, index) =>
                    $"// Question {index + 1} (Problem ID: {q.ProblemId})\n" +
                    $"// Language: {q.LanguageUsed}\n" +
                    q.FinalCodeSnapshot));

            if (!attempt.QuestionSnapshotCreated)
                await _questionPoolService.CreateAttemptQuestionSnapshotAsync(attempt);

            var evaluatedQuestions =
                new List<(QuestionSubmission Qs, JudgeResult Judge, decimal Marks, int? CodingTestQuestionId, int? AttemptQuestionId)>();
            foreach (var questionSubmission in request.QuestionSubmissions)
            {
                var resolved = await _questionPoolService.ResolveQuestionForAttemptAsync(
                    attempt.Id, questionSubmission.ProblemId);

                if (!resolved.IsAllowed)
                    throw new ArgumentException(
                        $"Problem {questionSubmission.ProblemId} is not part of coding test {request.CodingTestId}");

                var marks = resolved.Snapshot?.Marks ?? resolved.FixedQuestion?.Marks ?? 0m;

                var judgeResult = await _judgeService.EvaluateAsync(
                    questionSubmission.ProblemId,
                    questionSubmission.LanguageUsed,
                    questionSubmission.FinalCodeSnapshot);

                evaluatedQuestions.Add((
                    questionSubmission,
                    judgeResult,
                    marks,
                    resolved.FixedQuestion?.Id ?? resolved.Snapshot?.CodingTestQuestionId,
                    resolved.Snapshot?.Id));
            }

            var serverTotalTests = evaluatedQuestions.Sum(x => x.Judge.TotalTestCases);
            var serverPassed = evaluatedQuestions.Sum(x => x.Judge.PassedTestCases);
            var serverFailed = evaluatedQuestions.Sum(x => x.Judge.FailedTestCases);
            var serverExecutionMs = evaluatedQuestions.Sum(x => x.Judge.ExecutionTimeMs);
            var serverMemoryKb = evaluatedQuestions.Sum(x => x.Judge.MemoryUsedKB);

            // Create the main submission record for the whole test (authoritative aggregates)
            var submission = new CodingTestSubmission
            {
                CodingTestId = request.CodingTestId,
                CodingTestAttemptId = attempt.Id,
                CodingTestQuestionAttemptId = null, // Null for whole test submissions
                ProblemId = null, // Null for whole test submissions
                UserId = (long)request.UserId, // Cast to long to match database schema
                AttemptNumber = request.AttemptNumber,
                LanguageUsed = string.Join(", ", request.QuestionSubmissions.Select(q => q.LanguageUsed).Distinct()),
                FinalCodeSnapshot = combinedCodeSnapshot,
                TotalTestCases = serverTotalTests,
                PassedTestCases = serverPassed,
                FailedTestCases = serverFailed,
                RequestedHelp = request.QuestionSubmissions.Any(q => q.RequestedHelp),
                LanguageSwitchCount = request.QuestionSubmissions.Sum(q => q.LanguageSwitchCount),
                RunClickCount = request.QuestionSubmissions.Sum(q => q.RunClickCount),
                SubmitClickCount = request.QuestionSubmissions.Sum(q => q.SubmitClickCount),
                EraseCount = request.QuestionSubmissions.Sum(q => q.EraseCount),
                SaveCount = request.QuestionSubmissions.Sum(q => q.SaveCount),
                LoginLogoutCount = request.QuestionSubmissions.Sum(q => q.LoginLogoutCount),
                IsSessionAbandoned = request.QuestionSubmissions.Any(q => q.IsSessionAbandoned),
                ClassId = request.ClassId,
                SubmissionTime = DateTime.UtcNow,
                IsLateSubmission = request.IsLateSubmission,
                CreatedAt = DateTime.UtcNow,
                ExecutionTimeMs = serverExecutionMs,
                MemoryUsedKB = serverMemoryKb
            };

            _context.CodingTestSubmissions.Add(submission);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save submission: {ex.Message}. Inner exception: {ex.InnerException?.Message}", ex);
            }

            var questionSubmissionResponses = new List<QuestionSubmissionResponse>();
            var totalScore = 0m;
            var totalMaxScore = 0m;

            foreach (var (questionSubmission, judgeResult, marks, codingTestQuestionId, attemptQuestionId) in evaluatedQuestions)
            {
                var questionAttempt = attempt.QuestionAttempts?
                    .FirstOrDefault(qa => qa.ProblemId == questionSubmission.ProblemId);

                if (questionAttempt == null)
                {
                    questionAttempt = new CodingTestQuestionAttempt
                    {
                        CodingTestAttemptId = attempt.Id,
                        CodingTestQuestionId = codingTestQuestionId,
                        CodingTestAttemptQuestionId = attemptQuestionId,
                        ProblemId = questionSubmission.ProblemId,
                        UserId = request.UserId,
                        StartedAt = DateTime.UtcNow,
                        Status = "InProgress",
                        LanguageUsed = questionSubmission.LanguageUsed,
                        CodeSubmitted = "",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CodingTestQuestionAttempts.Add(questionAttempt);

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to save question attempt for ProblemId {questionSubmission.ProblemId}: {ex.Message}. Inner exception: {ex.InnerException?.Message}", ex);
                    }
                }

                var maxScore = marks;
                var score = ScoreCalculator.DecimalScore(
                    judgeResult.PassedTestCases, judgeResult.TotalTestCases, maxScore);

                // Update the question attempt
                questionAttempt.Status = "Completed";
                questionAttempt.CompletedAt = DateTime.UtcNow;
                questionAttempt.LanguageUsed = questionSubmission.LanguageUsed;
                questionAttempt.CodeSubmitted = questionSubmission.FinalCodeSnapshot;
                questionAttempt.TestCasesPassed = judgeResult.PassedTestCases;
                questionAttempt.TotalTestCases = judgeResult.TotalTestCases;
                questionAttempt.RunCount = questionSubmission.RunClickCount;
                questionAttempt.SubmitCount = questionSubmission.SubmitClickCount;
                questionAttempt.IsCorrect = judgeResult.IsCorrect;
                questionAttempt.Score = score;
                questionAttempt.MaxScore = maxScore;
                questionAttempt.UpdatedAt = DateTime.UtcNow;

                totalScore += score;
                totalMaxScore += maxScore;

                foreach (var tc in judgeResult.TestCaseResults)
                {
                    var submissionResult = new CodingTestSubmissionResult
                    {
                        SubmissionId = submission.SubmissionId,
                        TestCaseId = tc.TestCaseId,
                        ProblemId = questionSubmission.ProblemId,
                        TestCaseOrder = tc.TestCaseOrder,
                        Input = tc.Input,
                        ExpectedOutput = tc.ExpectedOutput,
                        ActualOutput = tc.ActualOutput,
                        IsPassed = tc.IsPassed,
                        ExecutionTimeMs = tc.ExecutionTimeMs,
                        MemoryUsedKB = tc.MemoryUsedKB,
                        ErrorMessage = tc.ErrorMessage,
                        ErrorType = tc.ErrorType,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.CodingTestSubmissionResults.Add(submissionResult);
                }

                var problemTitle = await _context.Problems
                    .Where(p => p.Id == questionSubmission.ProblemId)
                    .Select(p => p.Title)
                    .FirstOrDefaultAsync() ?? "Unknown Problem";

                questionSubmissionResponses.Add(new QuestionSubmissionResponse
                {
                    ProblemId = questionSubmission.ProblemId,
                    ProblemTitle = problemTitle,
                    LanguageUsed = questionSubmission.LanguageUsed,
                    TotalTestCases = judgeResult.TotalTestCases,
                    PassedTestCases = judgeResult.PassedTestCases,
                    FailedTestCases = judgeResult.FailedTestCases,
                    Score = score,
                    MaxScore = maxScore,
                    IsCorrect = judgeResult.IsCorrect,
                    TestCaseResults = MapJudgeToTestCaseSubmissionResults(judgeResult)
                });
            }

            // Update submission with calculated scores
            submission.Score = totalScore;
            submission.MaxScore = totalMaxScore;
            submission.IsCorrect = submission.PassedTestCases == submission.TotalTestCases && submission.TotalTestCases > 0;

            // Update the overall test attempt
            var percentage = totalMaxScore > 0 ? (double)totalScore / (double)totalMaxScore * 100 : 0;

            attempt.Status = "Submitted";
            attempt.SubmittedAt = DateTime.UtcNow;
            attempt.CompletedAt = DateTime.UtcNow;
            attempt.TotalScore = totalScore;
            attempt.MaxScore = totalMaxScore;
            attempt.Percentage = percentage;
            attempt.TimeSpentMinutes = request.TotalTimeSpentMinutes;
            attempt.IsLateSubmission = request.IsLateSubmission;
            attempt.UpdatedAt = DateTime.UtcNow;

            if (string.IsNullOrEmpty(attempt.SubmissionReason))
                attempt.SubmissionReason = SubmissionReasons.Manual;

            // Update the AssignedCodingTests status if this is an assigned test
            var assignment = await _context.AssignedCodingTests
                .FirstOrDefaultAsync(act => act.AssignedToUserId == request.UserId && 
                                          act.CodingTestId == request.CodingTestId &&
                                          !act.IsDeleted);

            if (assignment != null)
            {
                assignment.Status = "Completed";
                assignment.StartedAt = assignment.StartedAt ?? attempt.StartedAt;
                assignment.CompletedAt = attempt.CompletedAt;
                assignment.IsLateSubmission = attempt.IsLateSubmission;
                assignment.UpdatedAt = DateTime.UtcNow;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save final changes: {ex.Message}. Inner exception: {ex.InnerException?.Message}", ex);
            }

            await ApplyActiveTimeToAttemptAsync(attempt, assignment);
            await _context.SaveChangesAsync();

            foreach (var (questionSubmission, judgeResult, _, _, _) in evaluatedQuestions)
            {
                var passedIds = string.Join(",", judgeResult.TestCaseResults.Where(r => r.IsPassed).Select(r => r.TestCaseId));
                var failedIds = string.Join(",", judgeResult.TestCaseResults.Where(r => !r.IsPassed).Select(r => r.TestCaseId));
                var qa = attempt.QuestionAttempts?.FirstOrDefault(q => q.ProblemId == questionSubmission.ProblemId);

                await _activityTrackingService.CompleteAssessmentSessionAsync(
                    request.UserId,
                    questionSubmission.ProblemId,
                    attempt.Id,
                    submission.SubmissionId,
                    new AssessmentActivityMetrics
                    {
                        LanguageSwitchCount = questionSubmission.LanguageSwitchCount,
                        RunClickCount = questionSubmission.RunClickCount,
                        SubmitClickCount = questionSubmission.SubmitClickCount,
                        EraseCount = questionSubmission.EraseCount,
                        SaveCount = questionSubmission.SaveCount,
                        LoginLogoutCount = questionSubmission.LoginLogoutCount,
                        IsSessionAbandoned = questionSubmission.IsSessionAbandoned,
                        PassedTestCaseIDs = passedIds,
                        FailedTestCaseIDs = failedIds,
                        StartTime = qa?.StartedAt ?? attempt.StartedAt,
                        EndTime = DateTime.UtcNow,
                        TimeTakenSeconds = qa != null
                            ? (int)(DateTime.UtcNow - qa.StartedAt).TotalSeconds
                            : request.TotalTimeSpentMinutes * 60
                    },
                    "submit");

                await _integrityAnalysisService.RunSubmitHeuristicsAsync(
                    attempt.Id,
                    submission.SubmissionId,
                    questionSubmission.ProblemId,
                    questionSubmission.FinalCodeSnapshot,
                    judgeResult.IsCorrect,
                    attempt.StartedAt);
            }

            if (codingTest.EnablePlagiarismCheck)
            {
                foreach (var qs in request.QuestionSubmissions)
                {
                    _plagiarismService.EnqueueCheck(new PlagiarismCheckJob(
                        submission.SubmissionId,
                        request.CodingTestId,
                        qs.ProblemId,
                        attempt.Id));
                }
            }

            return new SubmitWholeCodingTestResponse
            {
                SubmissionId = submission.SubmissionId,
                CodingTestId = submission.CodingTestId,
                TestName = codingTest.TestName,
                UserId = request.UserId,
                AttemptNumber = request.AttemptNumber,
                TotalQuestions = request.QuestionSubmissions.Count,
                TotalScore = totalScore,
                MaxScore = totalMaxScore,
                Percentage = percentage,
                IsLateSubmission = request.IsLateSubmission,
                SubmissionTime = submission.SubmissionTime,
                QuestionSubmissions = questionSubmissionResponses,
                CreatedAt = submission.CreatedAt
            };
        }

        public Task AutoSubmitDisqualifiedAttemptAsync(int codingTestAttemptId)
            => AutoSubmitAttemptAsync(codingTestAttemptId, AutoSubmitReason.Disqualified);

        public async Task AutoSubmitAttemptAsync(int codingTestAttemptId, AutoSubmitReason reason)
        {
            var attempt = await _context.CodingTestAttempts
                .Include(a => a.QuestionAttempts)
                .Include(a => a.CodingTest)
                .FirstOrDefaultAsync(a => a.Id == codingTestAttemptId);

            if (attempt == null || attempt.Status is "Submitted" or "Completed")
                return;

            if (reason == AutoSubmitReason.Disqualified)
                attempt.IntegrityStatus = "Disqualified";

            var questionSubmissions = await BuildPartialQuestionSubmissionsAsync(attempt, reason);
            if (questionSubmissions.Count == 0)
            {
                await FinalizeAttemptWithoutSubmitAsync(attempt, reason);
                return;
            }

            var activeSeconds = await _activityTrackingService.GetAttemptActiveTimeSecondsAsync(attempt.Id);
            var endDate = attempt.CodingTest?.EndDate ?? DateTime.UtcNow;
            var request = new SubmitWholeCodingTestRequest
            {
                UserId = attempt.UserId,
                CodingTestId = attempt.CodingTestId,
                AttemptNumber = attempt.AttemptNumber,
                TotalTimeSpentMinutes = activeSeconds > 0
                    ? Math.Max(1, (activeSeconds + 59) / 60)
                    : Math.Max(1, (int)(DateTime.UtcNow - attempt.StartedAt).TotalMinutes),
                IsLateSubmission = DateTime.UtcNow > endDate,
                QuestionSubmissions = questionSubmissions
            };

            try
            {
                await SubmitWholeCodingTestAsync(request);
            }
            catch (Exception)
            {
                await FinalizeAttemptWithoutSubmitAsync(attempt, reason);
                return;
            }

            var updated = await _context.CodingTestAttempts.FindAsync(codingTestAttemptId);
            if (updated != null)
            {
                updated.SubmissionReason = reason == AutoSubmitReason.Disqualified
                    ? SubmissionReasons.AutoDQ
                    : SubmissionReasons.NetworkLoss;
                if (reason == AutoSubmitReason.Disqualified)
                    updated.IntegrityStatus = "Disqualified";
                updated.UpdatedAt = DateTime.UtcNow;

                var assignment = await _context.AssignedCodingTests
                    .FirstOrDefaultAsync(a => a.AssignedToUserId == updated.UserId
                                           && a.CodingTestId == updated.CodingTestId
                                           && !a.IsDeleted);
                await ApplyActiveTimeToAttemptAsync(updated, assignment);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<List<QuestionSubmission>> BuildPartialQuestionSubmissionsAsync(
            CodingTestAttempt attempt, AutoSubmitReason reason)
        {
            var attemptQuestions = await _context.CodingTestAttemptQuestions
                .Where(q => q.CodingTestAttemptId == attempt.Id)
                .OrderBy(q => q.QuestionOrder)
                .ToListAsync();

            if (attemptQuestions.Count == 0)
            {
                var fixedQuestions = await _context.CodingTestQuestions
                    .Where(q => q.CodingTestId == attempt.CodingTestId)
                    .OrderBy(q => q.QuestionOrder)
                    .ToListAsync();

                return fixedQuestions.Select(fq =>
                {
                    var qa = attempt.QuestionAttempts.FirstOrDefault(x => x.ProblemId == fq.ProblemId);
                    return BuildPartialQuestionSubmission(fq.ProblemId, qa, reason);
                }).ToList();
            }

            return attemptQuestions.Select(aq =>
            {
                var qa = attempt.QuestionAttempts.FirstOrDefault(x => x.ProblemId == aq.ProblemId);
                return BuildPartialQuestionSubmission(aq.ProblemId, qa, reason);
            }).ToList();
        }

        private static QuestionSubmission BuildPartialQuestionSubmission(
            int problemId, CodingTestQuestionAttempt? qa, AutoSubmitReason reason)
        {
            var placeholder = reason == AutoSubmitReason.Disqualified
                ? "# auto-submitted after disqualification\npass\n"
                : "# auto-submitted after network disconnect\npass\n";
            var code = qa?.CodeSubmitted;
            if (string.IsNullOrWhiteSpace(code))
                code = placeholder;

            return new QuestionSubmission
            {
                ProblemId = problemId,
                LanguageUsed = string.IsNullOrWhiteSpace(qa?.LanguageUsed) ? "python" : qa!.LanguageUsed,
                FinalCodeSnapshot = code,
                IsSessionAbandoned = true
            };
        }

        private async Task FinalizeAttemptWithoutSubmitAsync(CodingTestAttempt attempt, AutoSubmitReason reason)
        {
            attempt.Status = "Submitted";
            attempt.SubmittedAt = DateTime.UtcNow;
            attempt.CompletedAt = DateTime.UtcNow;
            attempt.SubmissionReason = reason == AutoSubmitReason.Disqualified
                ? SubmissionReasons.AutoDQ
                : SubmissionReasons.NetworkLoss;
            if (reason == AutoSubmitReason.Disqualified)
                attempt.IntegrityStatus = "Disqualified";
            attempt.UpdatedAt = DateTime.UtcNow;

            var assignment = await _context.AssignedCodingTests
                .FirstOrDefaultAsync(a => a.AssignedToUserId == attempt.UserId
                                       && a.CodingTestId == attempt.CodingTestId
                                       && !a.IsDeleted);

            if (assignment != null)
            {
                assignment.Status = "Submitted";
                assignment.CompletedAt = attempt.CompletedAt;
                assignment.IsLateSubmission = attempt.IsLateSubmission;
                assignment.UpdatedAt = DateTime.UtcNow;
            }

            await ApplyActiveTimeToAttemptAsync(attempt, assignment);
            await _context.SaveChangesAsync();
        }

        public async Task<List<CodingTestSubmissionSummaryResponse>> GetCodingTestSubmissionsAsync(GetCodingTestSubmissionsRequest request)
        {
            var query = _context.CodingTestSubmissions
                .Include(s => s.CodingTest)
                .Include(s => s.Problem)
                .AsQueryable();

            // Apply filters
            if (request.UserId.HasValue)
                query = query.Where(s => s.UserId == request.UserId.Value);
            
            if (request.CodingTestId.HasValue)
                query = query.Where(s => s.CodingTestId == request.CodingTestId.Value);
            
            if (request.ProblemId.HasValue)
                query = query.Where(s => s.ProblemId == request.ProblemId.Value);
            
            if (request.StartDate.HasValue)
                query = query.Where(s => s.SubmissionTime >= request.StartDate.Value);
            
            if (request.EndDate.HasValue)
                query = query.Where(s => s.SubmissionTime <= request.EndDate.Value);

            // Apply pagination
            var submissions = await query
                .OrderByDescending(s => s.SubmissionTime)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return submissions.Select(MapToSubmissionSummaryResponse).ToList();
        }

        public async Task<PagedResult<CodingTestSubmissionSummaryResponse>> GetCodingTestSubmissionsPagedAsync(GetCodingTestSubmissionsRequest request)
        {
            var query = _context.CodingTestSubmissions
                .Include(s => s.CodingTest)
                .Include(s => s.Problem)
                .AsQueryable();

            // Apply filters
            if (request.UserId.HasValue)
                query = query.Where(s => s.UserId == request.UserId.Value);
            
            if (request.CodingTestId.HasValue)
                query = query.Where(s => s.CodingTestId == request.CodingTestId.Value);
            
            if (request.ProblemId.HasValue)
                query = query.Where(s => s.ProblemId == request.ProblemId.Value);
            
            if (request.StartDate.HasValue)
                query = query.Where(s => s.SubmissionTime >= request.StartDate.Value);
            
            if (request.EndDate.HasValue)
                query = query.Where(s => s.SubmissionTime <= request.EndDate.Value);

            var totalCount = await query.CountAsync();

            // Apply pagination
            var submissions = await query
                .OrderByDescending(s => s.SubmissionTime)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<CodingTestSubmissionSummaryResponse>
            {
                Items = submissions.Select(MapToSubmissionSummaryResponse).ToList(),
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<SubmitCodingTestResponse> GetCodingTestSubmissionByIdAsync(long submissionId)
        {
            var submission = await _context.CodingTestSubmissions
                .Include(s => s.CodingTest)
                .Include(s => s.Problem)
                .Include(s => s.SubmissionResults)
                    .ThenInclude(r => r.TestCase)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);

            if (submission == null)
                throw new ArgumentException($"Submission with ID {submissionId} not found");

            return new SubmitCodingTestResponse
            {
                SubmissionId = submission.SubmissionId,
                CodingTestId = submission.CodingTestId,
                TestName = submission.CodingTest.TestName,
                ProblemId = submission.ProblemId ?? 0, // Handle nullable ProblemId
                ProblemTitle = submission.Problem?.Title ?? "Unknown Problem",
                UserId = (int)submission.UserId,
                AttemptNumber = submission.AttemptNumber,
                LanguageUsed = submission.LanguageUsed,
                TotalTestCases = submission.TotalTestCases,
                PassedTestCases = submission.PassedTestCases,
                FailedTestCases = submission.FailedTestCases,
                Score = submission.Score,
                MaxScore = submission.MaxScore,
                IsCorrect = submission.IsCorrect,
                IsLateSubmission = submission.IsLateSubmission,
                SubmissionTime = submission.SubmissionTime,
                ExecutionTimeMs = submission.ExecutionTimeMs,
                MemoryUsedKB = submission.MemoryUsedKB,
                ErrorMessage = submission.ErrorMessage,
                ErrorType = submission.ErrorType,
                TestCaseResults = submission.SubmissionResults.Select(MapToSubmissionTestCaseResult).ToList(),
                CreatedAt = submission.CreatedAt
            };
        }

        public async Task<CodingTestStatisticsResponse> GetCodingTestStatisticsAsync(int codingTestId)
        {
            var submissions = await _context.CodingTestSubmissions
                .Where(s => s.CodingTestId == codingTestId)
                .ToListAsync();

            var test = await _context.CodingTests.FindAsync(codingTestId);
            if (test == null)
                throw new ArgumentException($"Coding test with ID {codingTestId} not found");

            var stats = new CodingTestStatisticsResponse
            {
                CodingTestId = codingTestId,
                TestName = test.TestName,
                TotalSubmissions = submissions.Count,
                UniqueUsers = submissions.Select(s => s.UserId).Distinct().Count(),
                UniqueProblems = submissions.Select(s => s.ProblemId).Distinct().Count(),
                AverageSuccessRate = submissions.Any() ? submissions.Average(s => (double)s.PassedTestCases / Math.Max(s.TotalTestCases, 1)) : 0,
                AverageScoreRate = submissions.Any() ? submissions.Average(s => (double)s.Score / (double)Math.Max(s.MaxScore, 1m)) : 0,
                AverageExecutionTimeMs = submissions.Any() ? submissions.Average(s => s.ExecutionTimeMs) : 0,
                AverageMemoryUsedKB = submissions.Any() ? submissions.Average(s => s.MemoryUsedKB) : 0,
                TotalLanguageSwitches = submissions.Sum(s => s.LanguageSwitchCount),
                TotalRunClicks = submissions.Sum(s => s.RunClickCount),
                TotalSubmitClicks = submissions.Sum(s => s.SubmitClickCount),
                TotalEraseCount = submissions.Sum(s => s.EraseCount),
                TotalSaveCount = submissions.Sum(s => s.SaveCount),
                TotalLoginLogoutCount = submissions.Sum(s => s.LoginLogoutCount),
                AbandonedSessions = submissions.Count(s => s.IsSessionAbandoned),
                LateSubmissions = submissions.Count(s => s.IsLateSubmission)
            };

            return stats;
        }

        public async Task<List<SubmissionTestCaseResult>> GetSubmissionTestCaseResultsAsync(long submissionId)
        {
            var results = await _context.CodingTestSubmissionResults
                .Where(r => r.SubmissionId == submissionId)
                .OrderBy(r => r.TestCaseOrder)
                .ToListAsync();

            return results.Select(MapToSubmissionTestCaseResult).ToList();
        }

        private CodingTestSubmissionSummaryResponse MapToSubmissionSummaryResponse(CodingTestSubmission submission)
        {
            return new CodingTestSubmissionSummaryResponse
            {
                SubmissionId = submission.SubmissionId,
                CodingTestId = submission.CodingTestId,
                TestName = submission.CodingTest.TestName,
                ProblemId = submission.ProblemId ?? 0, // Handle nullable ProblemId
                ProblemTitle = submission.Problem?.Title ?? "Unknown Problem",
                UserId = (int)submission.UserId,
                AttemptNumber = submission.AttemptNumber,
                LanguageUsed = submission.LanguageUsed,
                TotalTestCases = submission.TotalTestCases,
                PassedTestCases = submission.PassedTestCases,
                FailedTestCases = submission.FailedTestCases,
                Score = submission.Score,
                MaxScore = submission.MaxScore,
                IsCorrect = submission.IsCorrect,
                IsLateSubmission = submission.IsLateSubmission,
                SubmissionTime = submission.SubmissionTime,
                ExecutionTimeMs = submission.ExecutionTimeMs,
                MemoryUsedKB = submission.MemoryUsedKB,
                ErrorMessage = submission.ErrorMessage,
                ErrorType = submission.ErrorType,
                LanguageSwitchCount = submission.LanguageSwitchCount,
                RunClickCount = submission.RunClickCount,
                SubmitClickCount = submission.SubmitClickCount,
                EraseCount = submission.EraseCount,
                SaveCount = submission.SaveCount,
                LoginLogoutCount = submission.LoginLogoutCount,
                IsSessionAbandoned = submission.IsSessionAbandoned,
                ClassId = submission.ClassId,
                CreatedAt = submission.CreatedAt
            };
        }

        private SubmissionTestCaseResult MapToSubmissionTestCaseResult(CodingTestSubmissionResult result)
        {
            return new SubmissionTestCaseResult
            {
                ResultId = result.ResultId,
                TestCaseId = result.TestCaseId,
                TestCaseOrder = result.TestCaseOrder,
                Input = result.Input,
                ExpectedOutput = result.ExpectedOutput,
                ActualOutput = result.ActualOutput,
                IsPassed = result.IsPassed,
                ExecutionTimeMs = result.ExecutionTimeMs,
                MemoryUsedKB = result.MemoryUsedKB,
                ErrorMessage = result.ErrorMessage,
                ErrorType = result.ErrorType
            };
        }

        // =============================================
        // Test Status Management Methods
        // =============================================

        public async Task<TestStatusResponse> EndTestAsync(EndTestRequest request)
        {
            // First try to find existing assignment
            var assignment = await _context.AssignedCodingTests
                .Include(act => act.CodingTest)
                .FirstOrDefaultAsync(act => act.AssignedToUserId == request.UserId &&
                                          act.CodingTestId == request.CodingTestId &&
                                          !act.IsDeleted);

            // If no assignment found, check if it's a global test that can be accessed without assignment
            if (assignment == null)
            {
                var codingTest = await _context.CodingTests.FindAsync(request.CodingTestId);
                if (codingTest == null)
                    throw new ArgumentException($"Test {request.CodingTestId} not found");

                // For global tests, create assignment record on-the-fly
                if (codingTest.IsGlobal)
                {
                    assignment = new AssignedCodingTest
                    {
                        CodingTestId = request.CodingTestId,
                        AssignedToUserId = request.UserId,
                        AssignedToUserType = 1, // Default user type
                        AssignedByUserId = request.UserId, // Self-assigned for global tests
                        AssignedDate = DateTime.UtcNow,
                        TestType = 1002, // Default test type
                        TestMode = 5, // Default test mode
                        CreatedAt = DateTime.UtcNow,
                        CodingTest = codingTest // Include the test data
                    };
                    _context.AssignedCodingTests.Add(assignment);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new ArgumentException($"Test assignment not found for user {request.UserId} and test {request.CodingTestId}");
                }
            }

            var now = DateTime.UtcNow;

            // Update all fields from request
            assignment.Status = request.Status;
            assignment.StartedAt = request.StartedAt ?? assignment.StartedAt;
            assignment.CompletedAt = request.CompletedAt ?? now;
            assignment.TimeSpentMinutes = request.TimeSpentMinutes;
            assignment.IsLateSubmission = request.IsLateSubmission;
            assignment.UpdatedAt = now;

            await _context.SaveChangesAsync();

            return MapToTestStatusResponse(assignment, now);
        }

        private TestStatusResponse MapToTestStatusResponse(AssignedCodingTest assignment, DateTime now)
        {
            var isExpired = now > assignment.CodingTest.EndDate;
            var canStart = !isExpired && 
                          now >= assignment.CodingTest.StartDate && 
                          assignment.Status == "Assigned";
            var canEnd = assignment.Status == "InProgress";

            var status = assignment.Status;
            if (isExpired && status != "Completed")
                status = "Expired";

            var message = status switch
            {
                "Assigned" => canStart ? "Test can be started" : "Test is not yet available",
                "InProgress" => "Test is in progress",
                "Completed" => assignment.IsLateSubmission ? "Test completed (late submission)" : "Test completed",
                "Expired" => "Test period has expired",
                _ => ""
            };

            return new TestStatusResponse
            {
                AssignedId = assignment.AssignedId,
                CodingTestId = assignment.CodingTestId,
                TestName = assignment.CodingTest.TestName,
                UserId = assignment.AssignedToUserId,
                Status = status,
                AssignedDate = assignment.AssignedDate,
                StartedAt = assignment.StartedAt,
                CompletedAt = assignment.CompletedAt,
                TimeSpentMinutes = assignment.TimeSpentMinutes,
                IsLateSubmission = assignment.IsLateSubmission,
                StartDate = assignment.CodingTest.StartDate,
                EndDate = assignment.CodingTest.EndDate,
                DurationMinutes = assignment.CodingTest.DurationMinutes,
                TotalQuestions = assignment.CodingTest.TotalQuestions,
                TotalMarks = assignment.CodingTest.TotalMarks,
                CanStart = canStart,
                CanEnd = canEnd,
                IsExpired = isExpired,
                Message = message
            };
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

            var judgeResult = await _judgeService.EvaluateAsync(
                questionAttempt.ProblemId,
                request.LanguageUsed,
                request.CodeSubmitted);

            var resolved = await _questionPoolService.ResolveQuestionForAttemptAsync(
                questionAttempt.CodingTestAttemptId, questionAttempt.ProblemId);
            questionAttempt.MaxScore = resolved.Snapshot?.Marks ?? resolved.FixedQuestion?.Marks ?? questionAttempt.MaxScore;
            questionAttempt.Score = ScoreCalculator.DecimalScore(
                judgeResult.PassedTestCases, judgeResult.TotalTestCases, questionAttempt.MaxScore);
            questionAttempt.TestCasesPassed = judgeResult.PassedTestCases;
            questionAttempt.TotalTestCases = judgeResult.TotalTestCases;
            questionAttempt.IsCorrect = judgeResult.IsCorrect;

            await _context.SaveChangesAsync();

            var attempt = await _context.CodingTestAttempts.FindAsync(questionAttempt.CodingTestAttemptId);
            if (attempt != null)
            {
                var passedIds = string.Join(",", judgeResult.TestCaseResults.Where(r => r.IsPassed).Select(r => r.TestCaseId));
                var failedIds = string.Join(",", judgeResult.TestCaseResults.Where(r => !r.IsPassed).Select(r => r.TestCaseId));

                await _activityTrackingService.CompleteAssessmentSessionAsync(
                    request.UserId,
                    questionAttempt.ProblemId,
                    attempt.Id,
                    null,
                    new AssessmentActivityMetrics
                    {
                        RunClickCount = request.RunCount,
                        SubmitClickCount = request.SubmitCount,
                        PassedTestCaseIDs = passedIds,
                        FailedTestCaseIDs = failedIds,
                        StartTime = questionAttempt.StartedAt,
                        EndTime = DateTime.UtcNow,
                        TimeTakenSeconds = (int)(DateTime.UtcNow - questionAttempt.StartedAt).TotalSeconds
                    },
                    "submit",
                    questionAttempt.Id);

                await _integrityAnalysisService.RunSubmitHeuristicsAsync(
                    attempt.Id,
                    null,
                    questionAttempt.ProblemId,
                    request.CodeSubmitted,
                    judgeResult.IsCorrect,
                    attempt.StartedAt);
            }

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

        public async Task<PagedResult<CodingTestSummaryResponse>> GetCodingTestsByStatusPagedAsync(string status, int pageNumber, int pageSize)
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

            var totalCount = await query.CountAsync();

            var codingTests = await query
                .OrderByDescending(ct => ct.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<CodingTestSummaryResponse>
            {
                Items = codingTests.Select(ct => MapToSummaryResponse(ct)).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
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

            var results = new List<CodingTestAttemptResponse>();
            foreach (var attempt in attempts)
                results.Add(await BuildAttemptResponseAsync(attempt));
            return results;
        }

        public async Task<PagedResult<CodingTestAttemptResponse>> GetCodingTestResultsPagedAsync(int codingTestId, int pageNumber, int pageSize)
        {
            var query = _context.CodingTestAttempts
                .Include(cta => cta.CodingTest)
                .Include(cta => cta.QuestionAttempts)
                    .ThenInclude(qa => qa.CodingTestQuestion)
                        .ThenInclude(q => q.Problem)
                .Where(cta => cta.CodingTestId == codingTestId && cta.Status == "Submitted");

            var totalCount = await query.CountAsync();

            var attempts = await query
                .OrderByDescending(cta => cta.Percentage)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = new List<CodingTestAttemptResponse>();
            foreach (var attempt in attempts)
                items.Add(await BuildAttemptResponseAsync(attempt));

            return new PagedResult<CodingTestAttemptResponse>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        // Validation methods
        public async Task<bool> ValidateAccessCodeAsync(int codingTestId, string accessCode)
        {
            var codingTest = await _context.CodingTests.FindAsync(codingTestId);
            return codingTest != null && codingTest.AccessCode == accessCode;
        }

        public async Task<bool> CanUserAttemptTestAsync(int userId, int codingTestId, string? accessCode = null)
        {
            var codingTest = await _context.CodingTests.FindAsync(codingTestId);
            if (codingTest == null || !codingTest.IsActive || !codingTest.IsPublished)
                return false;

            var now = DateTime.UtcNow;
            if (now < codingTest.StartDate || now > codingTest.EndDate)
                return false;

            // Check access based on test type
            if (codingTest.IsGlobal)
            {
                // Global test: if the test has an access code set, the caller must supply the matching code
                if (!string.IsNullOrEmpty(codingTest.AccessCode))
                {
                    if (string.IsNullOrEmpty(accessCode) || codingTest.AccessCode != accessCode)
                        return false;
                }
                // IsGlobal with no access code set = open to all authenticated users
            }
            else
            {
                // College-specific test: Check for assignment record
                var isAssigned = await _context.AssignedCodingTests
                    .AnyAsync(act => act.CodingTestId == codingTestId
                                 && act.AssignedToUserId == userId
                                 && !act.IsDeleted);

                if (!isAssigned)
                    return false;
            }

            if (!codingTest.AllowMultipleAttempts)
            {
                var existingAttempt = await _context.CodingTestAttempts
                    .AnyAsync(cta => cta.CodingTestId == codingTestId && cta.UserId == userId);
                if (existingAttempt)
                {
                    var pendingGrant = await GetPendingResumeGrantAsync(userId, codingTestId);
                    if (pendingGrant == null)
                        return false;
                }
            }
            else
            {
                var attemptCount = await _context.CodingTestAttempts
                    .CountAsync(cta => cta.CodingTestId == codingTestId && cta.UserId == userId);
                if (attemptCount >= codingTest.MaxAttempts)
                {
                    var pendingGrant = await GetPendingResumeGrantAsync(userId, codingTestId);
                    if (pendingGrant == null)
                        return false;
                }
            }

            var latestAttempt = await _context.CodingTestAttempts
                .Where(cta => cta.CodingTestId == codingTestId && cta.UserId == userId)
                .OrderByDescending(cta => cta.AttemptNumber)
                .FirstOrDefaultAsync();

            if (latestAttempt != null
                && latestAttempt.Status is "Submitted" or "Completed"
                && (latestAttempt.IntegrityStatus == "Disqualified"
                    || latestAttempt.SubmissionReason == SubmissionReasons.NetworkLoss))
            {
                var pendingGrant = await GetPendingResumeGrantAsync(userId, codingTestId);
                if (pendingGrant == null)
                    return false;
            }

            return true;
        }

        private async Task<CodingTestResumeGrant?> GetPendingResumeGrantAsync(int userId, int codingTestId)
        {
            var now = DateTime.UtcNow;
            return await _context.CodingTestResumeGrants
                .Where(g => g.CodingTestId == codingTestId
                         && g.UserId == userId
                         && g.Status == ResumeGrantStatuses.Pending
                         && g.AllowedEndAt > now)
                .OrderByDescending(g => g.GrantedAt)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> CanUserResumeTestAsync(
            int userId, int codingTestId, string? accessCode, CodingTestResumeGrant grant)
        {
            var codingTest = await _context.CodingTests.FindAsync(codingTestId);
            if (codingTest == null || !codingTest.IsActive || !codingTest.IsPublished)
                return false;

            var now = DateTime.UtcNow;
            if (now < codingTest.StartDate || now > codingTest.EndDate || grant.AllowedEndAt <= now)
                return false;

            if (!codingTest.IsGlobal)
            {
                var isAssigned = await _context.AssignedCodingTests
                    .AnyAsync(act => act.CodingTestId == codingTestId
                                  && act.AssignedToUserId == userId
                                  && !act.IsDeleted);
                if (!isAssigned)
                    return false;
            }
            else if (!string.IsNullOrEmpty(codingTest.AccessCode))
            {
                if (string.IsNullOrEmpty(accessCode) || codingTest.AccessCode != accessCode)
                    return false;
            }

            return true;
        }

        private async Task ApplyActiveTimeToAttemptAsync(CodingTestAttempt attempt, AssignedCodingTest? assignment = null)
        {
            var activeSeconds = await _activityTrackingService.GetAttemptActiveTimeSecondsAsync(attempt.Id);
            if (activeSeconds > 0)
                attempt.TimeSpentMinutes = Math.Max(1, (activeSeconds + 59) / 60);

            if (assignment != null)
                assignment.TimeSpentMinutes = attempt.TimeSpentMinutes;
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
                WarningThreshold = codingTest.WarningThreshold,
                FlagThreshold = codingTest.FlagThreshold,
                RequireFullscreen = codingTest.RequireFullscreen,
                BlockPaste = codingTest.BlockPaste,
                EnableProctoring = codingTest.EnableProctoring,
                EnablePlagiarismCheck = codingTest.EnablePlagiarismCheck,
                HostIP = codingTest.HostIP,
                ClassId = codingTest.ClassId,
                IsGlobal = codingTest.IsGlobal,
                CollegeId = codingTest.CollegeId,
                TopicData = codingTest.TopicData.Select(MapToTopicDataResponse).ToList(),
                Questions = codingTest.Questions?.Select(MapToQuestionResponse).ToList() ?? new List<CodingTestQuestionResponse>(),
                PoolSections = codingTest.PoolSections?.Select(s => new PoolSectionRequest
                {
                    PoolId = s.PoolId,
                    QuestionsToPick = s.QuestionsToPick,
                    SectionOrder = s.SectionOrder,
                    MarksPerQuestion = s.MarksPerQuestion,
                    TimeLimitMinutes = s.TimeLimitMinutes,
                    CustomInstructions = s.CustomInstructions
                }).ToList() ?? new List<PoolSectionRequest>(),
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
                Difficulty = problem.Difficulty.ToString(),
                TestCases = problem.TestCases?.Select(MapToTestCaseResponse).ToList() ?? new List<TestCaseResponse>(),
                StarterCodes = problem.StarterCodes?.Select(MapToStarterCodeResponse).ToList() ?? new List<StarterCodeResponse>()
            };
        }

        private StarterCodeResponse MapToStarterCodeResponse(StarterCode starterCode)
        {
            return new StarterCodeResponse
            {
                Id = starterCode.Id,
                ProblemId = starterCode.ProblemId,
                Language = starterCode.Language,
                Code = starterCode.Code
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
                IsEnabled = isEnabled,
                IsGlobal = codingTest.IsGlobal,
                CollegeId = codingTest.CollegeId
            };
        }

        private async Task<CodingTestAttemptResponse> BuildAttemptResponseAsync(CodingTestAttempt attempt)
        {
            var questions = await _questionPoolService.GetAttemptQuestionsAsync(attempt.Id);
            var test = attempt.CodingTest;
            var activeSeconds = await _activityTrackingService.GetAttemptActiveTimeSecondsAsync(attempt.Id);

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
                IntegrityStatus = attempt.IntegrityStatus,
                RequireFullscreen = test?.RequireFullscreen ?? false,
                BlockPaste = test?.BlockPaste ?? false,
                WarningThreshold = test?.WarningThreshold ?? 3,
                FlagThreshold = test?.FlagThreshold ?? 5,
                BreachRuleLimit = test?.BreachRuleLimit ?? 0,
                DurationMinutes = test?.DurationMinutes ?? 0,
                TestEndDate = test?.EndDate,
                ActiveTimeSpentSeconds = activeSeconds,
                ParentAttemptId = attempt.ParentAttemptId,
                AllowedEndAt = attempt.AllowedEndAt,
                RemainingSeconds = AttemptTimeBudgetService.ComputeRemainingSeconds(attempt.AllowedEndAt),
                IsResumeAttempt = attempt.ParentAttemptId.HasValue,
                SubmissionReason = attempt.SubmissionReason,
                Questions = questions,
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
                TestName = codingTest.TestName
            };
        }

        public async Task<List<AssignedCodingTestSummaryResponse>> GetAssignedTestsByUserAsync(long userId, int? testType = null, long? classId = null)
        {
            var query = _context.AssignedCodingTests
                .Include(act => act.CodingTest)
                .ThenInclude(ct => ct.TopicData)
                .Where(act => act.AssignedToUserId == userId && !act.IsDeleted);

            // Apply test type filter only when testType was explicitly provided by the caller
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

            return await MapAssignedSummariesAsync(assignments);
        }

        public async Task<PagedResult<AssignedCodingTestSummaryResponse>> GetAssignedTestsByUserPagedAsync(long userId, int pageNumber, int pageSize, int? testType = null, long? classId = null)
        {
            var query = _context.AssignedCodingTests
                .Include(act => act.CodingTest)
                .ThenInclude(ct => ct.TopicData)
                .Where(act => act.AssignedToUserId == userId && !act.IsDeleted);

            if (testType.HasValue)
            {
                query = query.Where(act => act.TestType == testType.Value);
            }

            if (classId.HasValue)
            {
                query = query.Where(act => act.CodingTest.ClassId == classId.Value || act.CodingTest.ClassId == 0);
            }

            var totalCount = await query.CountAsync();

            var assignments = await query
                .OrderByDescending(act => act.AssignedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AssignedCodingTestSummaryResponse>
            {
                Items = await MapAssignedSummariesAsync(assignments),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
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

            return await MapAssignedSummariesAsync(assignments);
        }

        public async Task<PagedResult<AssignedCodingTestSummaryResponse>> GetAssignedTestsByTestPagedAsync(int codingTestId, int pageNumber, int pageSize)
        {
            var query = _context.AssignedCodingTests
                .Include(act => act.CodingTest)
                .ThenInclude(ct => ct.TopicData)
                .Where(act => act.CodingTestId == codingTestId && !act.IsDeleted);

            var totalCount = await query.CountAsync();

            var assignments = await query
                .OrderByDescending(act => act.AssignedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AssignedCodingTestSummaryResponse>
            {
                Items = await MapAssignedSummariesAsync(assignments),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<List<AssignedCodingTestResponse>> GetAssignmentsByTestIdAsync(int codingTestId)
        {
            var assignments = await _context.AssignedCodingTests
                .Where(act => act.CodingTestId == codingTestId && !act.IsDeleted)
                .OrderByDescending(act => act.AssignedDate)
                .ToListAsync();

            return assignments.Select(act => new AssignedCodingTestResponse
            {
                AssignedId = act.AssignedId,
                CodingTestId = act.CodingTestId,
                AssignedToUserId = act.AssignedToUserId,
                AssignedToUserType = act.AssignedToUserType,
                AssignedByUserId = act.AssignedByUserId,
                AssignedDate = act.AssignedDate,
                TestType = act.TestType,
                TestMode = act.TestMode,
                IsDeleted = act.IsDeleted,
                CreatedAt = act.CreatedAt,
                UpdatedAt = act.UpdatedAt,
                Status = act.Status,
                StartedAt = act.StartedAt,
                CompletedAt = act.CompletedAt,
                TimeSpentMinutes = act.TimeSpentMinutes,
                IsLateSubmission = act.IsLateSubmission
            }).ToList();
        }

        public async Task<PagedResult<AssignedCodingTestResponse>> GetAssignmentsByTestIdPagedAsync(int codingTestId, int pageNumber, int pageSize)
        {
            var query = _context.AssignedCodingTests
                .Where(act => act.CodingTestId == codingTestId && !act.IsDeleted);

            var totalCount = await query.CountAsync();

            var assignments = await query
                .OrderByDescending(act => act.AssignedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<AssignedCodingTestResponse>
            {
                Items = assignments.Select(act => new AssignedCodingTestResponse
                {
                    AssignedId = act.AssignedId,
                    CodingTestId = act.CodingTestId,
                    AssignedToUserId = act.AssignedToUserId,
                    AssignedToUserType = act.AssignedToUserType,
                    AssignedByUserId = act.AssignedByUserId,
                    AssignedDate = act.AssignedDate,
                    TestType = act.TestType,
                    TestMode = act.TestMode,
                    IsDeleted = act.IsDeleted,
                    CreatedAt = act.CreatedAt,
                    UpdatedAt = act.UpdatedAt,
                    Status = act.Status,
                    StartedAt = act.StartedAt,
                    CompletedAt = act.CompletedAt,
                    TimeSpentMinutes = act.TimeSpentMinutes,
                    IsLateSubmission = act.IsLateSubmission
                }).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        private async Task<List<AssignedCodingTestSummaryResponse>> MapAssignedSummariesAsync(
            List<AssignedCodingTest> assignments)
        {
            if (assignments.Count == 0)
                return new List<AssignedCodingTestSummaryResponse>();

            var now = DateTime.UtcNow;
            var userIds = assignments.Select(a => (int)a.AssignedToUserId).Distinct().ToList();
            var testIds = assignments.Select(a => a.CodingTestId).Distinct().ToList();

            var attempts = await _context.CodingTestAttempts
                .Where(a => userIds.Contains(a.UserId) && testIds.Contains(a.CodingTestId))
                .ToListAsync();

            var latestAttempts = attempts
                .GroupBy(a => (a.UserId, a.CodingTestId))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AttemptNumber).First());

            var grants = await _context.CodingTestResumeGrants
                .Where(g => userIds.Contains(g.UserId)
                         && testIds.Contains(g.CodingTestId)
                         && g.Status == ResumeGrantStatuses.Pending
                         && g.AllowedEndAt > now)
                .ToListAsync();

            var pendingGrants = grants
                .GroupBy(g => (g.UserId, g.CodingTestId))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.GrantedAt).First());

            return assignments.Select(a =>
            {
                latestAttempts.TryGetValue(((int)a.AssignedToUserId, a.CodingTestId), out var latest);
                pendingGrants.TryGetValue(((int)a.AssignedToUserId, a.CodingTestId), out var grant);
                return MapToAssignedSummaryResponse(a, latest, grant, now);
            }).ToList();
        }

        private AssignedCodingTestSummaryResponse MapToAssignedSummaryResponse(
            AssignedCodingTest assignment,
            CodingTestAttempt? latestAttempt = null,
            CodingTestResumeGrant? pendingGrant = null,
            DateTime? nowUtc = null)
        {
            var codingTest = assignment.CodingTest;
            var now = nowUtc ?? DateTime.UtcNow;
            
            // Use the actual Status column from AssignedCodingTests table
            string status = assignment.Status;

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

            var studentAction = StudentTestActionBuilder.Compute(codingTest, latestAttempt, pendingGrant, now);
            var canResume = pendingGrant != null
                         && pendingGrant.Status == ResumeGrantStatuses.Pending
                         && pendingGrant.AllowedEndAt > now
                         && now <= codingTest.EndDate;

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
                AssignedByUserId = assignment.AssignedByUserId,
                SubjectName = subjectName,
                TopicName = topicName,
                LatestAttemptId = latestAttempt?.Id,
                LatestAttemptNumber = latestAttempt?.AttemptNumber,
                AttemptStatus = latestAttempt?.Status,
                IntegrityStatus = latestAttempt?.IntegrityStatus,
                SubmissionReason = latestAttempt?.SubmissionReason,
                CanResume = canResume,
                StudentAction = studentAction,
                ResumeGrant = canResume && pendingGrant != null
                    ? new ResumeGrantSummaryResponse
                    {
                        PriorAttemptId = pendingGrant.PriorAttemptId,
                        AllowedEndAt = pendingGrant.AllowedEndAt,
                        RemainingSeconds = AttemptTimeBudgetService.ComputeRemainingSeconds(pendingGrant.AllowedEndAt)
                    }
                    : null,
                RequireFullscreen = codingTest.RequireFullscreen
            };
        }

        // =============================================
        // Comprehensive Test Results Methods
        // =============================================

        public async Task<ComprehensiveTestResultResponse> GetComprehensiveTestResultsAsync(GetTestResultsRequest request)
        {
            try
            {
                // Get the coding test details with full problem data
                var codingTest = await _context.CodingTests
                    .Include(ct => ct.Questions)
                        .ThenInclude(q => q.Problem)
                            .ThenInclude(p => p.TestCases)
                    .Include(ct => ct.Questions)
                        .ThenInclude(q => q.Problem)
                            .ThenInclude(p => p.StarterCodes)
                    .FirstOrDefaultAsync(ct => ct.Id == request.CodingTestId);

            // If the problem data is not fully loaded, fetch it separately
            if (codingTest != null)
            {
                var problemIds = codingTest.Questions?.Select(q => q.ProblemId).ToList() ?? new List<int>();
                var problems = await _context.Problems
                    .Include(p => p.TestCases)
                    .Include(p => p.StarterCodes)
                    .Where(p => problemIds.Contains(p.Id))
                    .ToListAsync();

                // Update the problem references in the questions
                foreach (var question in codingTest.Questions)
                {
                    var problem = problems.FirstOrDefault(p => p.Id == question.ProblemId);
                    if (problem != null)
                    {
                        question.Problem = problem;
                    }
                }
            }

            if (codingTest == null)
                throw new ArgumentException($"Coding test with ID {request.CodingTestId} not found");

            // 🚀 NON-BLOCKING: Start fetching student profile data in parallel (won't fail the main operation)
            var studentProfileTask = _studentProfileService.GetStudentProfileAsync(request.UserId);

            // Get all submissions for this user and test
            var submissionsQuery = _context.CodingTestSubmissions
                .Include(s => s.Problem)
                .Where(s => s.UserId == request.UserId && s.CodingTestId == request.CodingTestId);

            // Filter by attempt number if specified
            if (request.AttemptNumber.HasValue)
            {
                submissionsQuery = submissionsQuery.Where(s => s.AttemptNumber == request.AttemptNumber.Value);
            }

            var submissions = await submissionsQuery.ToListAsync();

            // Get all test case results for these submissions
            var submissionIds = submissions.Select(s => s.SubmissionId).ToList();
            var testCaseResults = await _context.CodingTestSubmissionResults
                .Where(r => submissionIds.Contains(r.SubmissionId))
                .OrderBy(r => r.SubmissionId)
                .ThenBy(r => r.TestCaseOrder)
                .ToListAsync();

            // Get question attempts for additional code snapshots
            var submissionProblemIds = submissions.Select(s => s.ProblemId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            
            // Get question attempts - include attempt number filter if specified
            var questionAttemptsQuery = _context.CodingTestQuestionAttempts
                .Include(qa => qa.CodingTestAttempt)
                .Where(qa => qa.UserId == request.UserId && submissionProblemIds.Contains(qa.ProblemId));
            
            if (request.AttemptNumber.HasValue)
            {
                questionAttemptsQuery = questionAttemptsQuery.Where(qa => qa.CodingTestAttempt.AttemptNumber == request.AttemptNumber.Value);
            }
            
            var questionAttempts = await questionAttemptsQuery.ToListAsync();

            // Get core question results for additional code snapshots - include attempt number filter if specified
            var coreQuestionResultsQuery = _context.CoreQuestionResults
                .Where(cqr => cqr.UserId == request.UserId && submissionProblemIds.Contains(cqr.ProblemId));
            
            if (request.AttemptNumber.HasValue)
            {
                coreQuestionResultsQuery = coreQuestionResultsQuery.Where(cqr => cqr.AttemptNumber == request.AttemptNumber.Value);
            }
            
            var coreQuestionResults = await coreQuestionResultsQuery.ToListAsync();

            // Group test case results by problem ID (question-wise grouping)
            var testCaseResultsByProblem = (testCaseResults ?? new List<CodingTestSubmissionResult>())
                .GroupBy(r => r.ProblemId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Group submissions by problem ID to get the latest submission for each problem
            var submissionsByProblem = submissions
                .Where(s => s.ProblemId.HasValue)
                .GroupBy(s => s.ProblemId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.SubmissionTime).First());

            // Create problem results grouped by question/problem
            var problemResults = new List<ProblemTestResult>();
            var allTestCaseResults = new List<DetailedTestCaseResult>();

            // Get all unique problem IDs from both submissions and test case results
            var submissionProblemIdsForUnion = submissions.Where(s => s.ProblemId.HasValue).Select(s => s.ProblemId!.Value);
            var testCaseProblemIds = testCaseResults.Select(r => r.ProblemId);
            var allProblemIds = submissionProblemIdsForUnion.Union(testCaseProblemIds).Distinct().ToList();

            foreach (var problemId in allProblemIds)
            {
                // Get the latest submission for this problem
                var submission = submissionsByProblem.GetValueOrDefault(problemId);
                
                // Get all test case results for this problem
                var testCaseResultsForProblem = testCaseResultsByProblem.GetValueOrDefault(problemId, new List<CodingTestSubmissionResult>());
                
                // Get the best available code snapshot from multiple sources
                var questionAttempt = questionAttempts.FirstOrDefault(qa => qa.ProblemId == problemId);
                var coreQuestionResult = coreQuestionResults.FirstOrDefault(cqr => cqr.ProblemId == problemId);
                
                // ISSUE FIX: If no code found with attempt number filter, try without filter as fallback
                // This handles cases where:
                // 1. Code was submitted but attempt number doesn't match exactly
                // 2. Code was stored in a different attempt
                // 3. Code was stored in question attempts or core results instead of submissions
                if (string.IsNullOrEmpty(submission?.FinalCodeSnapshot) && 
                    string.IsNullOrEmpty(questionAttempt?.CodeSubmitted) && 
                    string.IsNullOrEmpty(coreQuestionResult?.FinalCodeSnapshot))
                {
                    // Aggressive fallback: Get code from any attempt for this problem, regardless of attempt number
                    var fallbackQuestionAttempt = await _context.CodingTestQuestionAttempts
                        .Where(qa => qa.UserId == request.UserId && qa.ProblemId == problemId && !string.IsNullOrEmpty(qa.CodeSubmitted))
                        .OrderByDescending(qa => qa.CreatedAt)
                        .FirstOrDefaultAsync();
                    
                    var fallbackCoreResult = await _context.CoreQuestionResults
                        .Where(cqr => cqr.UserId == request.UserId && cqr.ProblemId == problemId && !string.IsNullOrEmpty(cqr.FinalCodeSnapshot))
                        .OrderByDescending(cqr => cqr.CreatedAt)
                        .FirstOrDefaultAsync();
                    
                    // Also try to get any submission for this problem, regardless of attempt number
                    var fallbackSubmission = await _context.CodingTestSubmissions
                        .Where(s => s.UserId == request.UserId && s.ProblemId == problemId && !string.IsNullOrEmpty(s.FinalCodeSnapshot))
                        .OrderByDescending(s => s.SubmissionTime)
                        .FirstOrDefaultAsync();
                    
                    if (fallbackSubmission != null)
                    {
                        submission = fallbackSubmission;
                    }
                    if (fallbackQuestionAttempt != null)
                    {
                        questionAttempt = fallbackQuestionAttempt;
                    }
                    if (fallbackCoreResult != null)
                    {
                        coreQuestionResult = fallbackCoreResult;
                    }
                }
                
                // Priority: Submission > Question Attempt > Core Question Result
                var finalCodeSnapshot = "";
                var codeSource = "none";
                
                if (!string.IsNullOrEmpty(submission?.FinalCodeSnapshot))
                {
                    finalCodeSnapshot = submission.FinalCodeSnapshot;
                    codeSource = "submission";
                }
                else if (!string.IsNullOrEmpty(questionAttempt?.CodeSubmitted))
                {
                    finalCodeSnapshot = questionAttempt.CodeSubmitted;
                    codeSource = "question_attempt";
                }
                else if (!string.IsNullOrEmpty(coreQuestionResult?.FinalCodeSnapshot))
                {
                    finalCodeSnapshot = coreQuestionResult.FinalCodeSnapshot;
                    codeSource = "core_result";
                }
                
                // Get the language used from the best available source
                var languageUsed = submission?.LanguageUsed ?? 
                                 questionAttempt?.LanguageUsed ?? 
                                 coreQuestionResult?.LanguageUsed ?? "";
                
                // Create comprehensive debug information
                var totalSubmissions = submissions.Count;
                var totalQuestionAttempts = questionAttempts.Count;
                var totalCoreResults = coreQuestionResults.Count;
                var submissionsForThisProblem = submissions.Count(s => s.ProblemId == problemId);
                var attemptsForThisProblem = questionAttempts.Count(qa => qa.ProblemId == problemId);
                var coreResultsForThisProblem = coreQuestionResults.Count(cqr => cqr.ProblemId == problemId);
                
                // Additional debug queries to check if ANY data exists for this user/problem
                var anySubmissionsForUser = await _context.CodingTestSubmissions
                    .CountAsync(s => s.UserId == request.UserId);
                var anyAttemptsForUser = await _context.CodingTestQuestionAttempts
                    .CountAsync(qa => qa.UserId == request.UserId);
                var anyCoreResultsForUser = await _context.CoreQuestionResults
                    .CountAsync(cqr => cqr.UserId == request.UserId);
                var anySubmissionsForProblem = await _context.CodingTestSubmissions
                    .CountAsync(s => s.ProblemId == problemId);
                var anyAttemptsForProblem = await _context.CodingTestQuestionAttempts
                    .CountAsync(qa => qa.ProblemId == problemId);
                var anyCoreResultsForProblem = await _context.CoreQuestionResults
                    .CountAsync(cqr => cqr.ProblemId == problemId);
                
                var debugInfo = $"TotalSubmissions: {totalSubmissions}, TotalAttempts: {totalQuestionAttempts}, TotalCoreResults: {totalCoreResults}, " +
                               $"SubmissionsForProblem: {submissionsForThisProblem}, AttemptsForProblem: {attemptsForThisProblem}, CoreResultsForProblem: {coreResultsForThisProblem}, " +
                               $"AnySubmissionsForUser: {anySubmissionsForUser}, AnyAttemptsForUser: {anyAttemptsForUser}, AnyCoreResultsForUser: {anyCoreResultsForUser}, " +
                               $"AnySubmissionsForProblem: {anySubmissionsForProblem}, AnyAttemptsForProblem: {anyAttemptsForProblem}, AnyCoreResultsForProblem: {anyCoreResultsForProblem}, " +
                               $"Submission: {(submission != null ? "Found" : "Not found")}, " +
                               $"QuestionAttempt: {(questionAttempt != null ? "Found" : "Not found")}, " +
                               $"CoreResult: {(coreQuestionResult != null ? "Found" : "Not found")}, " +
                               $"SubmissionCode: {(!string.IsNullOrEmpty(submission?.FinalCodeSnapshot) ? "Has code" : "No code")}, " +
                               $"AttemptCode: {(!string.IsNullOrEmpty(questionAttempt?.CodeSubmitted) ? "Has code" : "No code")}, " +
                               $"CoreCode: {(!string.IsNullOrEmpty(coreQuestionResult?.FinalCodeSnapshot) ? "Has code" : "No code")}, " +
                               $"UserId: {request.UserId}, CodingTestId: {request.CodingTestId}, ProblemId: {problemId}, AttemptNumber: {request.AttemptNumber}";
                
                // Get the problem details from the coding test questions
                var problem = codingTest.Questions?.FirstOrDefault(q => q.ProblemId == problemId)?.Problem;
                
                // If problem is still null, fetch it directly from the database
                if (problem == null)
                {
                    problem = await _context.Problems
                        .Include(p => p.TestCases)
                        .Include(p => p.StarterCodes)
                        .FirstOrDefaultAsync(p => p.Id == problemId);
                }
                
                // Calculate score based on test case results if no submission score is available
                var maxScore = codingTest.Questions?.FirstOrDefault(q => q.ProblemId == problemId)?.Marks ?? 0;
                var totalTestCases = submission?.TotalTestCases ?? testCaseResultsForProblem.Count;
                var passedTestCases = submission?.PassedTestCases ?? testCaseResultsForProblem.Count(tc => tc.IsPassed);
                var calculatedScore = 0m;
                var isCorrect = false;

                if (totalTestCases > 0)
                {
                    calculatedScore = ScoreCalculator.DecimalScore(passedTestCases, totalTestCases, maxScore);
                    isCorrect = passedTestCases == totalTestCases;
                }
                
                var problemResult = new ProblemTestResult
                {
                    ProblemId = problemId,
                    ProblemTitle = problem?.Title ?? "Unknown Problem",
                    QuestionOrder = codingTest.Questions?.FirstOrDefault(q => q.ProblemId == problemId)?.QuestionOrder ?? 0,
                    MaxScore = maxScore,
                    LanguageUsed = languageUsed, // Use the best available language
                    FinalCodeSnapshot = finalCodeSnapshot, // Use the best available code snapshot
                    CodeSource = codeSource, // Track the source of the code
                    DebugInfo = debugInfo, // Debug information
                    TotalTestCases = totalTestCases,
                    PassedTestCases = passedTestCases,
                    FailedTestCases = submission?.FailedTestCases ?? testCaseResultsForProblem.Count(tc => !tc.IsPassed),
                    Score = submission?.Score ?? calculatedScore, // Use submission score if available, otherwise calculated score
                    IsCorrect = submission?.IsCorrect ?? isCorrect, // Use submission result if available, otherwise calculated result
                    IsLateSubmission = submission?.IsLateSubmission ?? false,
                    SubmissionTime = submission?.SubmissionTime ?? DateTime.MinValue,
                    ExecutionTimeMs = submission?.ExecutionTimeMs ?? 0,
                    MemoryUsedKB = submission?.MemoryUsedKB ?? 0,
                    ErrorMessage = submission?.ErrorMessage,
                    ErrorType = submission?.ErrorType,
                    LanguageSwitchCount = submission?.LanguageSwitchCount ?? 0,
                    RunClickCount = submission?.RunClickCount ?? 0,
                    SubmitClickCount = submission?.SubmitClickCount ?? 0,
                    EraseCount = submission?.EraseCount ?? 0,
                    SaveCount = submission?.SaveCount ?? 0,
                    LoginLogoutCount = submission?.LoginLogoutCount ?? 0,
                    IsSessionAbandoned = submission?.IsSessionAbandoned ?? false,
                    QuestionDetails = MapToQuestionDetails(problem),
                    TestCaseResults = testCaseResultsForProblem.Select(MapToDetailedTestCaseResult).ToList()
                };

                problemResults.Add(problemResult);
                allTestCaseResults.AddRange(problemResult.TestCaseResults);
            }

            // Calculate summary
            var summary = CalculateTestSummary(problemResults, allTestCaseResults, codingTest);

            // 🚀 NON-BLOCKING: Wait for student profile data (with timeout)
            StudentProfileData? studentProfile = null;
            try
            {
                // Create a timeout task
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(3));
                var completedTask = await Task.WhenAny(studentProfileTask, timeoutTask);

                if (completedTask == studentProfileTask)
                {
                    studentProfile = await studentProfileTask;
                }
                else
                {
                    // Timeout occurred - continue without student data
                    Console.WriteLine($"Timeout waiting for student profile data for UserId: {request.UserId}. Continuing without student data.");
                }
            }
            catch (Exception ex)
            {
                // Any exception occurred - log and continue without student data
                Console.WriteLine($"Failed to fetch student profile for UserId: {request.UserId}. Continuing without student data. Error: {ex.Message}");
            }

            return new ComprehensiveTestResultResponse
            {
                CodingTestId = codingTest.Id,
                TestName = codingTest.TestName,
                UserId = request.UserId,
                TotalQuestions = codingTest.TotalQuestions,
                TotalMarks = codingTest.TotalMarks,
                TotalScore = summary.TotalScore, // User's total score
                Percentage = summary.Percentage, // Percentage score
                StartDate = codingTest.StartDate,
                EndDate = codingTest.EndDate,
                DurationMinutes = codingTest.DurationMinutes,
                ProblemResults = problemResults?.OrderBy(p => p.QuestionOrder).ToList() ?? new List<ProblemTestResult>(),
                Summary = summary,
                StudentProfile = studentProfile // Include student data (may be null)
            };
            }
            catch (Exception ex)
            {
                // Log the error and return a basic response
                Console.WriteLine($"Error in GetComprehensiveTestResultsAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Return a minimal response to prevent the API from crashing
                return new ComprehensiveTestResultResponse
                {
                    CodingTestId = request.CodingTestId,
                    TestName = "Error retrieving test data",
                    UserId = request.UserId,
                    TotalQuestions = 0,
                    TotalMarks = 0,
                    TotalScore = 0,
                    Percentage = 0,
                    StartDate = DateTime.MinValue,
                    EndDate = DateTime.MinValue,
                    DurationMinutes = 0,
                    ProblemResults = new List<ProblemTestResult>(),
                    Summary = new TestSummary(),
                    StudentProfile = null
                };
            }
        }

        public async Task<object> GetDebugDataAsync(long userId, int codingTestId, int? problemId = null)
        {
            try
            {
                var debugData = new
                {
                    UserId = userId,
                    CodingTestId = codingTestId,
                    ProblemId = problemId,
                    Submissions = await _context.CodingTestSubmissions
                        .Where(s => s.UserId == userId && s.CodingTestId == codingTestId)
                        .Select(s => new { s.SubmissionId, s.ProblemId, s.AttemptNumber, s.LanguageUsed, HasCode = !string.IsNullOrEmpty(s.FinalCodeSnapshot), s.SubmissionTime })
                        .ToListAsync(),
                    QuestionAttempts = await _context.CodingTestQuestionAttempts
                        .Where(qa => qa.UserId == userId)
                        .Select(qa => new { qa.Id, qa.ProblemId, qa.CodingTestAttemptId, qa.LanguageUsed, HasCode = !string.IsNullOrEmpty(qa.CodeSubmitted), qa.CreatedAt })
                        .ToListAsync(),
                    CoreResults = await _context.CoreQuestionResults
                        .Where(cqr => cqr.UserId == userId)
                        .Select(cqr => new { cqr.Id, cqr.ProblemId, cqr.AttemptNumber, cqr.LanguageUsed, HasCode = !string.IsNullOrEmpty(cqr.FinalCodeSnapshot), cqr.CreatedAt })
                        .ToListAsync(),
                    TestCaseResults = await _context.CodingTestSubmissionResults
                        .Select(r => new { r.ResultId, r.ProblemId, r.SubmissionId, r.IsPassed, r.CreatedAt })
                        .ToListAsync()
                };

                return debugData;
            }
            catch (Exception ex)
            {
                return new
                {
                    Error = "Failed to retrieve debug data",
                    Details = ex.Message,
                    UserId = userId,
                    CodingTestId = codingTestId,
                    ProblemId = problemId
                };
            }
        }

        private DetailedTestCaseResult MapToDetailedTestCaseResult(CodingTestSubmissionResult result)
        {
            return new DetailedTestCaseResult
            {
                ResultId = result.ResultId,
                TestCaseId = result.TestCaseId,
                TestCaseOrder = result.TestCaseOrder,
                Input = result.Input,
                ExpectedOutput = result.ExpectedOutput,
                ActualOutput = result.ActualOutput,
                IsPassed = result.IsPassed,
                ExecutionTimeMs = result.ExecutionTimeMs,
                MemoryUsedKB = result.MemoryUsedKB,
                ErrorMessage = result.ErrorMessage,
                ErrorType = result.ErrorType,
                CreatedAt = result.CreatedAt
            };
        }

        private QuestionDetails MapToQuestionDetails(Problem? problem)
        {
            if (problem == null)
            {
                return new QuestionDetails
                {
                    ProblemId = 0,
                    Title = "Unknown Problem",
                    Description = "",
                    Examples = "",
                    Constraints = "",
                    Hints = null,
                    TimeLimit = null,
                    MemoryLimit = null,
                    SubdomainId = null,
                    Difficulty = null,
                    TestCases = new List<TestCaseDetails>(),
                    StarterCodes = new List<StarterCodeDetails>()
                };
            }

            return new QuestionDetails
            {
                ProblemId = problem.Id,
                Title = problem.Title,
                Description = problem.Description,
                Examples = problem.Examples,
                Constraints = problem.Constraints,
                Hints = problem.Hints?.ToString() ?? "",
                TimeLimit = problem.TimeLimit,
                MemoryLimit = problem.MemoryLimit,
                SubdomainId = problem.SubdomainId,
                Difficulty = problem.Difficulty,
                TestCases = problem.TestCases?.Select(MapToTestCaseDetails).ToList() ?? new List<TestCaseDetails>(),
                StarterCodes = problem.StarterCodes?.Select(MapToStarterCodeDetails).ToList() ?? new List<StarterCodeDetails>()
            };
        }

        private TestCaseDetails MapToTestCaseDetails(TestCase testCase)
        {
            return new TestCaseDetails
            {
                Id = testCase.Id,
                ProblemId = testCase.ProblemId,
                Input = testCase.Input,
                ExpectedOutput = testCase.ExpectedOutput
            };
        }

        private StarterCodeDetails MapToStarterCodeDetails(StarterCode starterCode)
        {
            return new StarterCodeDetails
            {
                Id = starterCode.Id,
                ProblemId = starterCode.ProblemId,
                Language = starterCode.Language,
                Code = starterCode.Code
            };
        }

        private TestSummary CalculateTestSummary(List<ProblemTestResult> problemResults, List<DetailedTestCaseResult> allTestCaseResults, CodingTest codingTest)
        {
            var totalScore = problemResults.Sum(p => p.Score);
            var maxPossibleScore = problemResults.Sum(p => p.MaxScore);
            var percentage = maxPossibleScore > 0 ? (double)totalScore / (double)maxPossibleScore * 100 : 0;

            var totalTestCases = allTestCaseResults.Count;
            var passedTestCases = allTestCaseResults.Count(tc => tc.IsPassed);
            var failedTestCases = totalTestCases - passedTestCases;

            var correctProblems = problemResults.Count(p => p.IsCorrect);
            var totalProblems = problemResults.Count;

            var averageExecutionTime = allTestCaseResults.Any() ? allTestCaseResults.Average(tc => tc.ExecutionTimeMs) : 0;
            var averageMemoryUsed = allTestCaseResults.Any() ? allTestCaseResults.Average(tc => tc.MemoryUsedKB) : 0;

            var totalLanguageSwitches = problemResults.Sum(p => p.LanguageSwitchCount);
            var totalRunClicks = problemResults.Sum(p => p.RunClickCount);
            var totalSubmitClicks = problemResults.Sum(p => p.SubmitClickCount);
            var totalEraseCount = problemResults.Sum(p => p.EraseCount);
            var totalSaveCount = problemResults.Sum(p => p.SaveCount);
            var totalLoginLogoutCount = problemResults.Sum(p => p.LoginLogoutCount);
            var abandonedSessions = problemResults.Count(p => p.IsSessionAbandoned);
            var lateSubmissions = problemResults.Count(p => p.IsLateSubmission);

            var submissionTimes = problemResults.Select(p => p.SubmissionTime).Where(t => t != default).ToList();
            var firstSubmissionTime = submissionTimes.Any() ? submissionTimes.Min() : (DateTime?)null;
            var lastSubmissionTime = submissionTimes.Any() ? submissionTimes.Max() : (DateTime?)null;

            return new TestSummary
            {
                TotalScore = totalScore,
                MaxPossibleScore = maxPossibleScore,
                Percentage = Math.Round(percentage, 2),
                TotalTestCases = totalTestCases,
                PassedTestCases = passedTestCases,
                FailedTestCases = failedTestCases,
                CorrectProblems = correctProblems,
                TotalProblems = totalProblems,
                AverageExecutionTimeMs = Math.Round(averageExecutionTime, 2),
                AverageMemoryUsedKB = Math.Round(averageMemoryUsed, 2),
                TotalLanguageSwitches = totalLanguageSwitches,
                TotalRunClicks = totalRunClicks,
                TotalSubmitClicks = totalSubmitClicks,
                TotalEraseCount = totalEraseCount,
                TotalSaveCount = totalSaveCount,
                TotalLoginLogoutCount = totalLoginLogoutCount,
                AbandonedSessions = abandonedSessions,
                LateSubmissions = lateSubmissions,
                FirstSubmissionTime = firstSubmissionTime,
                LastSubmissionTime = lastSubmissionTime
            };
        }

        private static List<SubmissionTestCaseResult> MapJudgeToSubmissionTestCaseResults(JudgeResult judge)
        {
            return judge.TestCaseResults.Select(r => new SubmissionTestCaseResult
            {
                ResultId = 0,
                TestCaseId = r.TestCaseId,
                TestCaseOrder = r.TestCaseOrder,
                Input = r.Input,
                ExpectedOutput = r.ExpectedOutput,
                ActualOutput = r.ActualOutput,
                IsPassed = r.IsPassed,
                ExecutionTimeMs = r.ExecutionTimeMs,
                MemoryUsedKB = r.MemoryUsedKB,
                ErrorMessage = r.ErrorMessage,
                ErrorType = r.ErrorType
            }).ToList();
        }

        private static List<TestCaseSubmissionResult> MapJudgeToTestCaseSubmissionResults(JudgeResult judge)
        {
            return judge.TestCaseResults.Select(r => new TestCaseSubmissionResult
            {
                TestCaseId = r.TestCaseId,
                TestCaseOrder = r.TestCaseOrder,
                Input = r.Input,
                ExpectedOutput = r.ExpectedOutput,
                ActualOutput = r.ActualOutput,
                IsPassed = r.IsPassed,
                ExecutionTimeMs = r.ExecutionTimeMs,
                MemoryUsedKB = r.MemoryUsedKB,
                ErrorMessage = r.ErrorMessage,
                ErrorType = r.ErrorType
            }).ToList();
        }

        private static TestCaseSubmissionResult MapStoredToTestCaseSubmissionResult(CodingTestSubmissionResult r)
        {
            return new TestCaseSubmissionResult
            {
                TestCaseId = r.TestCaseId,
                TestCaseOrder = r.TestCaseOrder,
                Input = r.Input,
                ExpectedOutput = r.ExpectedOutput,
                ActualOutput = r.ActualOutput,
                IsPassed = r.IsPassed,
                ExecutionTimeMs = r.ExecutionTimeMs,
                MemoryUsedKB = r.MemoryUsedKB,
                ErrorMessage = r.ErrorMessage,
                ErrorType = r.ErrorType
            };
        }

        private async Task ValidateCodingTestMarksAlignmentAsync(int codingTestId)
        {
            var test = await _context.CodingTests
                .AsNoTracking()
                .Include(ct => ct.Questions)
                .FirstOrDefaultAsync(ct => ct.Id == codingTestId);

            if (test == null)
                return;

            var sumQuestionMarks = test.Questions.Sum(q => q.Marks);
            if (sumQuestionMarks != test.TotalMarks)
            {
                throw new ArgumentException(
                    $"Total marks ({test.TotalMarks}) must equal sum of question marks ({sumQuestionMarks})");
            }
        }
    }
}
