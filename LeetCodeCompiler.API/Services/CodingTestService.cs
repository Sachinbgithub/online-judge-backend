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
                IsPublished = request.IsPublished,
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

    // Get question attempts with test case results using JOIN queries (similar to comprehensive results)
    var questionAttempts = await (from qa in _context.CodingTestQuestionAttempts
                                 join cta in _context.CodingTestAttempts on qa.CodingTestAttemptId equals cta.Id
                                 join ct in _context.CodingTests on cta.CodingTestId equals ct.Id
                                 join cq in _context.CodingTestQuestions on qa.CodingTestQuestionId equals cq.Id
                                 join p in _context.Problems on cq.ProblemId equals p.Id into problemJoin
                                 from p in problemJoin.DefaultIfEmpty()
                                 where qa.UserId == userId && cta.CodingTestId == codingTestId
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

        // Create test case results from question attempt data (since we don't have CodingTestSubmissionResults)
        var testCaseResults = new List<TestCaseSubmissionResult>();
        // Note: For whole-test submissions, we don't have individual test case results stored
        // We can only show the summary from the question attempt

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
            TestCaseResults = testCaseResults // Empty for now, as we don't have individual test case results
        });
    }

    // Calculate totals from question attempts
    var totalScore = questionSubmissionResponses.Sum(q => q.Score);
    var maxScore = questionSubmissionResponses.Sum(q => q.MaxScore);
    var percentage = maxScore > 0 ? (totalScore * 100.0) / maxScore : 0;

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
        TestCaseAccuracy = testCaseAccuracy
    };
}
















            
        public async Task<CodingTestResponse> GetCodingTestByIdAsync(int id)
        {
            var codingTest = await _context.CodingTests
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
                TestType = ct.TestType
            }).ToList();
        }

        public async Task<CodingTestResponse> UpdateCodingTestAsync(UpdateCodingTestRequest request)
        {
            var codingTest = await _context.CodingTests
                .Include(ct => ct.Questions)
                .FirstOrDefaultAsync(ct => ct.Id == request.Id);
            
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

            // Handle question updates if provided
            if (request.Questions != null && request.Questions.Any())
            {
                await UpdateCodingTestQuestionsAsync(codingTest.Id, request.Questions);
            }

            codingTest.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

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
                        Marks = questionUpdate.Marks ?? 10,
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
            // Validate access
            if (!await CanUserAttemptTestAsync(request.UserId, request.CodingTestId))
                throw new InvalidOperationException("User cannot attempt this test");

            // Access code validation removed - access code is now completely optional
            // Test can be started with just codingTestId and userId

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

        public async Task<SubmitCodingTestResponse> SubmitCodingTestAsync(SubmitCodingTestRequest request)
        {
            CodingTestQuestion? codingTestQuestion = null;
            int targetCodingTestId;

            // Determine which coding test to use
            if (request.CodingTestId.HasValue)
            {
                // Use the specified coding test
                targetCodingTestId = request.CodingTestId.Value;
                codingTestQuestion = await _context.CodingTestQuestions
                    .Include(ctq => ctq.CodingTest)
                    .FirstOrDefaultAsync(ctq => ctq.ProblemId == request.ProblemId && ctq.CodingTestId == targetCodingTestId);

                if (codingTestQuestion == null)
                    throw new ArgumentException($"Problem {request.ProblemId} is not part of coding test {targetCodingTestId}");
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
            var attempt = await _context.CodingTestAttempts
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

            // Find or create the specific question attempt for this problem
            var questionAttempt = attempt.QuestionAttempts
                .FirstOrDefault(qa => qa.CodingTestQuestion.ProblemId == request.ProblemId);

            if (questionAttempt == null)
            {
                // Create the question attempt automatically
                questionAttempt = new CodingTestQuestionAttempt
                {
                    CodingTestAttemptId = attempt.Id,
                    CodingTestQuestionId = codingTestQuestion?.Id ?? 0,
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

                // Set the navigation property
                if (codingTestQuestion != null)
                {
                    questionAttempt.CodingTestQuestion = codingTestQuestion;
                }
            }

            // Create the submission record
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
                TotalTestCases = request.TotalTestCases,
                PassedTestCases = request.PassedTestCases,
                FailedTestCases = request.FailedTestCases,
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
                CreatedAt = DateTime.UtcNow
            };

            // Calculate score based on passed test cases
            var maxScore = questionAttempt.CodingTestQuestion.Marks;
            var score = request.TotalTestCases > 0 ? (int)((double)request.PassedTestCases / request.TotalTestCases * maxScore) : 0;
            
            submission.Score = score;
            submission.MaxScore = maxScore;
            submission.IsCorrect = request.PassedTestCases == request.TotalTestCases && request.TotalTestCases > 0;

            // Note: Removed CodingTestSubmissions.Add(submission) as submissions should not be stored in CodingTestSubmissions table

            // Update the question attempt with the submitted code and results
            questionAttempt.Status = "Completed";
            questionAttempt.CompletedAt = DateTime.UtcNow;
            questionAttempt.LanguageUsed = request.LanguageUsed;
            questionAttempt.CodeSubmitted = request.FinalCodeSnapshot;
            questionAttempt.TestCasesPassed = request.PassedTestCases;
            questionAttempt.TotalTestCases = request.TotalTestCases;
            questionAttempt.RunCount = request.RunClickCount;
            questionAttempt.SubmitCount = request.SubmitClickCount;
            questionAttempt.IsCorrect = submission.IsCorrect;
            questionAttempt.Score = score;
            questionAttempt.MaxScore = maxScore;
            questionAttempt.UpdatedAt = DateTime.UtcNow;

            // Update the overall test attempt
            var totalScore = attempt.QuestionAttempts.Sum(qa => qa.Score);
            var totalMaxScore = attempt.QuestionAttempts.Sum(qa => qa.MaxScore);
            var percentage = totalMaxScore > 0 ? (double)totalScore / totalMaxScore * 100 : 0;

            attempt.Status = "Submitted";
            attempt.SubmittedAt = DateTime.UtcNow;
            attempt.CompletedAt = DateTime.UtcNow;
            attempt.TotalScore = totalScore;
            attempt.MaxScore = totalMaxScore;
            attempt.Percentage = percentage;
            attempt.TimeSpentMinutes = (int)(DateTime.UtcNow - attempt.StartedAt).TotalMinutes;
            attempt.IsLateSubmission = submission.IsLateSubmission;
            attempt.UpdatedAt = DateTime.UtcNow;

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
                assignment.TimeSpentMinutes = attempt.TimeSpentMinutes;
                assignment.IsLateSubmission = attempt.IsLateSubmission;
                assignment.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Get the problem details for response
            var problem = await _context.Problems.FindAsync(request.ProblemId);

            // Map to response using questionAttempt data instead of submission
            return new SubmitCodingTestResponse
            {
                SubmissionId = questionAttempt.Id, // Use question attempt ID as submission ID
                CodingTestId = attempt.CodingTestId,
                TestName = attempt.CodingTest?.TestName ?? "Unknown Test",
                ProblemId = request.ProblemId,
                ProblemTitle = problem?.Title ?? "Unknown Problem",
                UserId = request.UserId,
                AttemptNumber = request.AttemptNumber,
                LanguageUsed = request.LanguageUsed,
                TotalTestCases = request.TotalTestCases,
                PassedTestCases = request.PassedTestCases,
                FailedTestCases = request.FailedTestCases,
                Score = score,
                MaxScore = maxScore,
                IsCorrect = request.PassedTestCases == request.TotalTestCases && request.TotalTestCases > 0,
                IsLateSubmission = submission.IsLateSubmission,
                SubmissionTime = DateTime.UtcNow,
                ExecutionTimeMs = 0, // Not available without actual execution
                MemoryUsedKB = 0, // Not available without actual execution
                ErrorMessage = null, // Not available without actual execution
                ErrorType = null, // Not available without actual execution
                TestCaseResults = new List<SubmissionTestCaseResult>(), // Will be populated if needed
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

            // Create the main submission record for the whole test
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
                TotalTestCases = request.QuestionSubmissions.Sum(q => q.TotalTestCases),
                PassedTestCases = request.QuestionSubmissions.Sum(q => q.PassedTestCases),
                FailedTestCases = request.QuestionSubmissions.Sum(q => q.FailedTestCases),
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
                CreatedAt = DateTime.UtcNow
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
            var totalScore = 0;
            var totalMaxScore = 0;

            // Process each question submission
            foreach (var questionSubmission in request.QuestionSubmissions)
            {
                // Find the coding test question
                var codingTestQuestion = await _context.CodingTestQuestions
                    .Include(ctq => ctq.Problem)
                    .FirstOrDefaultAsync(ctq => ctq.ProblemId == questionSubmission.ProblemId && 
                                               ctq.CodingTestId == request.CodingTestId);

                if (codingTestQuestion == null)
                    throw new ArgumentException($"Problem {questionSubmission.ProblemId} is not part of coding test {request.CodingTestId}");

                // Find or create the question attempt
                var questionAttempt = attempt.QuestionAttempts?
                    .FirstOrDefault(qa => qa.CodingTestQuestion?.ProblemId == questionSubmission.ProblemId);

                if (questionAttempt == null)
                {
                    questionAttempt = new CodingTestQuestionAttempt
                    {
                        CodingTestAttemptId = attempt.Id,
                        CodingTestQuestionId = codingTestQuestion.Id,
                        ProblemId = questionSubmission.ProblemId,
                        UserId = request.UserId, // Keep as int for CodingTestQuestionAttempt
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

                    questionAttempt.CodingTestQuestion = codingTestQuestion;
                }

                // Calculate score for this question
                var maxScore = codingTestQuestion.Marks;
                // Use provided score if available, otherwise calculate based on test case results
                var score = questionSubmission.Score > 0 ? 
                    questionSubmission.Score : 
                    (questionSubmission.TotalTestCases > 0 ? 
                        (int)((double)questionSubmission.PassedTestCases / questionSubmission.TotalTestCases * maxScore) : 0);

                // Update the question attempt
                questionAttempt.Status = "Completed";
                questionAttempt.CompletedAt = DateTime.UtcNow;
                questionAttempt.LanguageUsed = questionSubmission.LanguageUsed;
                questionAttempt.CodeSubmitted = questionSubmission.FinalCodeSnapshot;
                questionAttempt.TestCasesPassed = questionSubmission.PassedTestCases;
                questionAttempt.TotalTestCases = questionSubmission.TotalTestCases;
                questionAttempt.RunCount = questionSubmission.RunClickCount;
                questionAttempt.SubmitCount = questionSubmission.SubmitClickCount;
                questionAttempt.IsCorrect = questionSubmission.PassedTestCases == questionSubmission.TotalTestCases && questionSubmission.TotalTestCases > 0;
                questionAttempt.Score = score;
                questionAttempt.MaxScore = maxScore;
                questionAttempt.UpdatedAt = DateTime.UtcNow;

                totalScore += score;
                totalMaxScore += maxScore;

                // Create test case results for this question
                foreach (var testCaseResult in questionSubmission.TestCaseResults)
                {
                    try
                    {
                        // Validate that the TestCase exists before creating the result
                        var testCaseExists = await _context.TestCases
                            .AnyAsync(tc => tc.Id == testCaseResult.TestCaseId);

                        if (!testCaseExists)
                        {
                            throw new ArgumentException($"TestCase with ID {testCaseResult.TestCaseId} does not exist in the database. Please check the TestCase IDs in your request.");
                        }

                        var submissionResult = new CodingTestSubmissionResult
                        {
                            SubmissionId = submission.SubmissionId,
                            TestCaseId = testCaseResult.TestCaseId,
                            ProblemId = questionSubmission.ProblemId, // Add ProblemId from the question submission
                            TestCaseOrder = testCaseResult.TestCaseOrder,
                            Input = testCaseResult.Input,
                            ExpectedOutput = testCaseResult.ExpectedOutput,
                            ActualOutput = testCaseResult.ActualOutput,
                            IsPassed = testCaseResult.IsPassed,
                            ExecutionTimeMs = testCaseResult.ExecutionTimeMs,
                            MemoryUsedKB = testCaseResult.MemoryUsedKB,
                            ErrorMessage = testCaseResult.ErrorMessage,
                            ErrorType = testCaseResult.ErrorType,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.CodingTestSubmissionResults.Add(submissionResult);
                    }
                    catch (ArgumentException)
                    {
                        // Re-throw ArgumentException as-is (it has the proper message)
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to create test case result for TestCaseId {testCaseResult.TestCaseId}: {ex.Message}. Inner exception: {ex.InnerException?.Message}", ex);
                    }
                }

                // Add to response
                questionSubmissionResponses.Add(new QuestionSubmissionResponse
                {
                    ProblemId = questionSubmission.ProblemId,
                    ProblemTitle = codingTestQuestion.Problem?.Title ?? "Unknown Problem",
                    LanguageUsed = questionSubmission.LanguageUsed,
                    TotalTestCases = questionSubmission.TotalTestCases,
                    PassedTestCases = questionSubmission.PassedTestCases,
                    FailedTestCases = questionSubmission.FailedTestCases,
                    Score = score,
                    MaxScore = maxScore,
                    IsCorrect = questionSubmission.PassedTestCases == questionSubmission.TotalTestCases && questionSubmission.TotalTestCases > 0,
                    TestCaseResults = questionSubmission.TestCaseResults
                });
            }

            // Update submission with calculated scores
            submission.Score = totalScore;
            submission.MaxScore = totalMaxScore;
            submission.IsCorrect = submission.PassedTestCases == submission.TotalTestCases && submission.TotalTestCases > 0;

            // Update the overall test attempt
            var percentage = totalMaxScore > 0 ? (double)totalScore / totalMaxScore * 100 : 0;

            attempt.Status = "Submitted";
            attempt.SubmittedAt = DateTime.UtcNow;
            attempt.CompletedAt = DateTime.UtcNow;
            attempt.TotalScore = totalScore;
            attempt.MaxScore = totalMaxScore;
            attempt.Percentage = percentage;
            attempt.TimeSpentMinutes = request.TotalTimeSpentMinutes;
            attempt.IsLateSubmission = request.IsLateSubmission;
            attempt.UpdatedAt = DateTime.UtcNow;

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
                assignment.TimeSpentMinutes = attempt.TimeSpentMinutes;
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

            // Return response
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
                AverageScoreRate = submissions.Any() ? submissions.Average(s => (double)s.Score / Math.Max(s.MaxScore, 1)) : 0,
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
            var assignment = await _context.AssignedCodingTests
                .Include(act => act.CodingTest)
                .FirstOrDefaultAsync(act => act.AssignedToUserId == request.UserId && 
                                          act.CodingTestId == request.CodingTestId &&
                                          !act.IsDeleted);

            if (assignment == null)
                throw new ArgumentException($"Test assignment not found for user {request.UserId} and test {request.CodingTestId}");

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
                Difficulty = problem.Difficulty.ToString(),
                TestCases = problem.TestCases.Select(MapToTestCaseResponse).ToList(),
                StarterCodes = problem.StarterCodes.Select(MapToStarterCodeResponse).ToList()
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

        private AssignedCodingTestSummaryResponse MapToAssignedSummaryResponse(AssignedCodingTest assignment)
        {
            var codingTest = assignment.CodingTest;
            
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
                Status = status, // Now using the actual Status column from AssignedCodingTests
                AssignedByName = "System", // You might want to get this from a user service
                SubjectName = subjectName,
                TopicName = topicName
            };
        }

        // =============================================
        // Comprehensive Test Results Methods
        // =============================================

        public async Task<ComprehensiveTestResultResponse> GetComprehensiveTestResultsAsync(GetTestResultsRequest request)
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
                var problemIds = codingTest.Questions.Select(q => q.ProblemId).ToList();
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
            var testCaseResultsByProblem = testCaseResults
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
                var problem = codingTest.Questions.FirstOrDefault(q => q.ProblemId == problemId)?.Problem;
                
                // If problem is still null, fetch it directly from the database
                if (problem == null)
                {
                    problem = await _context.Problems
                        .Include(p => p.TestCases)
                        .Include(p => p.StarterCodes)
                        .FirstOrDefaultAsync(p => p.Id == problemId);
                }
                
                // Calculate score based on test case results if no submission score is available
                var maxScore = codingTest.Questions.FirstOrDefault(q => q.ProblemId == problemId)?.Marks ?? 0;
                var totalTestCases = submission?.TotalTestCases ?? testCaseResultsForProblem.Count;
                var passedTestCases = submission?.PassedTestCases ?? testCaseResultsForProblem.Count(tc => tc.IsPassed);
                var calculatedScore = 0;
                var isCorrect = false;

                if (totalTestCases > 0)
                {
                    // Calculate score based on percentage of passed test cases
                    var passPercentage = (double)passedTestCases / totalTestCases;
                    calculatedScore = (int)Math.Round(maxScore * passPercentage);
                    isCorrect = passedTestCases == totalTestCases;
                }
                
                var problemResult = new ProblemTestResult
                {
                    ProblemId = problemId,
                    ProblemTitle = problem?.Title ?? "Unknown Problem",
                    QuestionOrder = codingTest.Questions.FirstOrDefault(q => q.ProblemId == problemId)?.QuestionOrder ?? 0,
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
                ProblemResults = problemResults.OrderBy(p => p.QuestionOrder).ToList(),
                Summary = summary
            };
        }

        public async Task<object> GetDebugDataAsync(long userId, int codingTestId, int? problemId = null)
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
            var percentage = maxPossibleScore > 0 ? (double)totalScore / maxPossibleScore * 100 : 0;

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
    }
}
