using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Services
{
    public class PracticeTestService : IPracticeTestService
    {
        private readonly AppDbContext _context;

        public PracticeTestService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CreatePracticeTestResponse> CreatePracticeTestAsync(CreatePracticeTestRequest request)
        {
            try
            {
                // Validate domain and subdomain exist
                var domain = await _context.Domains.FindAsync(request.DomainId);
                if (domain == null)
                {
                    return new CreatePracticeTestResponse
                    {
                        Success = false,
                        Message = $"Domain with ID {request.DomainId} not found"
                    };
                }

                var subdomain = await _context.Subdomains.FindAsync(request.SubdomainId);
                if (subdomain == null)
                {
                    return new CreatePracticeTestResponse
                    {
                        Success = false,
                        Message = $"Subdomain with ID {request.SubdomainId} not found"
                    };
                }

                // Validate all problems exist
                var problemIds = request.Questions.Select(q => q.ProblemId).ToList();
                var existingProblems = await _context.Problems
                    .Where(p => problemIds.Contains(p.Id))
                    .Select(p => p.Id)
                    .ToListAsync();

                var missingProblems = problemIds.Except(existingProblems).ToList();
                if (missingProblems.Any())
                {
                    return new CreatePracticeTestResponse
                    {
                        Success = false,
                        Message = $"Problems not found: {string.Join(", ", missingProblems)}"
                    };
                }

                // Validate question order is unique
                var questionOrders = request.Questions.Select(q => q.QuestionOrder).ToList();
                if (questionOrders.Count != questionOrders.Distinct().Count())
                {
                    return new CreatePracticeTestResponse
                    {
                        Success = false,
                        Message = "Question order must be unique"
                    };
                }

                // Validate total marks match sum of question marks
                var totalQuestionMarks = request.Questions.Sum(q => q.Marks);
                if (totalQuestionMarks != request.TotalMarks)
                {
                    return new CreatePracticeTestResponse
                    {
                        Success = false,
                        Message = $"Total marks ({request.TotalMarks}) must equal sum of question marks ({totalQuestionMarks})"
                    };
                }

                // Generate test name if not provided
                var testName = $"Practice Test - {domain.DomainName} - {subdomain.SubdomainName} - {DateTime.UtcNow:yyyy-MM-dd}";

                // Create practice test
                var practiceTest = new PracticeTest
                {
                    TestName = testName,
                    Description = $"Practice test for {domain.DomainName} - {subdomain.SubdomainName}",
                    DomainId = request.DomainId,
                    SubdomainId = request.SubdomainId,
                    TotalMarks = request.TotalMarks,
                    DurationMinutes = request.DurationMinutes,
                    CreatedBy = request.CreatedBy,
                    IsActive = request.IsActive,
                    IsPublished = request.IsPublished,
                    AllowMultipleAttempts = request.AllowMultipleAttempts,
                    MaxAttempts = request.MaxAttempts,
                    ShowResultsImmediately = request.ShowResultsImmediately,
                    DifficultyLevel = request.DifficultyLevel,
                    Tags = request.Tags,
                    Instructions = request.Instructions,
                    PassingPercentage = request.PassingPercentage,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PracticeTests.Add(practiceTest);
                await _context.SaveChangesAsync();

                // Create practice test questions
                foreach (var questionRequest in request.Questions)
                {
                    var question = new PracticeTestQuestion
                    {
                        PracticeTestId = practiceTest.Id,
                        ProblemId = questionRequest.ProblemId,
                        QuestionOrder = questionRequest.QuestionOrder,
                        Marks = questionRequest.Marks,
                        TimeLimitMinutes = questionRequest.TimeLimitMinutes,
                        CustomInstructions = questionRequest.CustomInstructions,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PracticeTestQuestions.Add(question);
                }

                await _context.SaveChangesAsync();

                return new CreatePracticeTestResponse
                {
                    PracticeTestId = practiceTest.Id,
                    Success = true,
                    Message = "Practice test created successfully"
                };
            }
            catch (Exception ex)
            {
                return new CreatePracticeTestResponse
                {
                    Success = false,
                    Message = $"Error creating practice test: {ex.Message}"
                };
            }
        }

        public async Task<StartPracticeTestResponse> StartPracticeTestAsync(StartPracticeTestRequest request)
        {
            try
            {
                // Get practice test with questions
                var practiceTest = await _context.PracticeTests
                    .Include(pt => pt.Questions)
                        .ThenInclude(q => q.Problem)
                    .Include(pt => pt.Domain)
                    .Include(pt => pt.Subdomain)
                    .FirstOrDefaultAsync(pt => pt.Id == request.PracticeTestId);

                if (practiceTest == null)
                {
                    return new StartPracticeTestResponse
                    {
                        Success = false,
                        Message = $"Practice test with ID {request.PracticeTestId} not found"
                    };
                }

                if (!practiceTest.IsActive || !practiceTest.IsPublished)
                {
                    return new StartPracticeTestResponse
                    {
                        Success = false,
                        Message = "Practice test is not available"
                    };
                }

                // Check if user has exceeded max attempts
                var existingAttempts = await _context.PracticeTestResults
                    .Where(ptr => ptr.PracticeTestId == request.PracticeTestId && ptr.UserId == request.UserId)
                    .CountAsync();

                if (!practiceTest.AllowMultipleAttempts && existingAttempts > 0)
                {
                    return new StartPracticeTestResponse
                    {
                        Success = false,
                        Message = "Multiple attempts are not allowed for this practice test"
                    };
                }

                if (existingAttempts >= practiceTest.MaxAttempts)
                {
                    return new StartPracticeTestResponse
                    {
                        Success = false,
                        Message = $"Maximum attempts ({practiceTest.MaxAttempts}) exceeded"
                    };
                }

                // Get next attempt number
                var nextAttemptNumber = existingAttempts + 1;

                // Create practice test result (attempt)
                var practiceTestResult = new PracticeTestResult
                {
                    PracticeTestId = request.PracticeTestId,
                    UserId = request.UserId,
                    AttemptNumber = nextAttemptNumber,
                    StartedAt = DateTime.UtcNow,
                    TotalMarks = practiceTest.TotalMarks,
                    Status = "InProgress",
                    CreatedAt = DateTime.UtcNow
                };

                _context.PracticeTestResults.Add(practiceTestResult);
                await _context.SaveChangesAsync();

                // Get questions with problems, test cases, and starter codes
                var questions = new List<QuestionInfo>();
                foreach (var question in practiceTest.Questions.OrderBy(q => q.QuestionOrder))
                {
                    var problem = await _context.Problems
                        .Include(p => p.TestCases)
                        .Include(p => p.StarterCodes)
                        .FirstOrDefaultAsync(p => p.Id == question.ProblemId);

                    if (problem == null) continue;

                    var questionInfo = new QuestionInfo
                    {
                        QuestionOrder = question.QuestionOrder,
                        ProblemId = problem.Id,
                        ProblemTitle = problem.Title,
                        ProblemDescription = problem.Description,
                        Examples = problem.Examples,
                        Constraints = problem.Constraints,
                        Marks = question.Marks,
                        TimeLimitMinutes = question.TimeLimitMinutes,
                        CustomInstructions = question.CustomInstructions,
                        TestCases = problem.TestCases.Select(tc => new TestCaseInfo
                        {
                            Id = tc.Id,
                            Input = tc.Input,
                            ExpectedOutput = tc.ExpectedOutput
                        }).ToList(),
                        StarterCodes = problem.StarterCodes.Select(sc => new StarterCodeInfo
                        {
                            Id = sc.Id,
                            Language = sc.Language.ToString(),
                            Code = sc.Code
                        }).ToList()
                    };

                    questions.Add(questionInfo);
                }

                var endTime = DateTime.UtcNow.AddMinutes(practiceTest.DurationMinutes);

                return new StartPracticeTestResponse
                {
                    PracticeTestId = practiceTest.Id,
                    TestName = practiceTest.TestName,
                    UserId = request.UserId,
                    AttemptNumber = nextAttemptNumber,
                    StartedAt = practiceTestResult.StartedAt,
                    EndTime = endTime,
                    DurationMinutes = practiceTest.DurationMinutes,
                    Questions = questions,
                    Success = true,
                    Message = "Practice test started successfully"
                };
            }
            catch (Exception ex)
            {
                return new StartPracticeTestResponse
                {
                    Success = false,
                    Message = $"Error starting practice test: {ex.Message}"
                };
            }
        }

        public async Task<SubmitPracticeTestResultResponse> SubmitPracticeTestResultAsync(SubmitPracticeTestResultRequest request)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Get practice test result (attempt)
                    var practiceTestResult = await _context.PracticeTestResults
                        .Include(ptr => ptr.PracticeTest)
                        .FirstOrDefaultAsync(ptr => ptr.PracticeTestId == request.PracticeTestId &&
                                                  ptr.UserId == request.UserId &&
                                                  ptr.AttemptNumber == request.AttemptNumber);

                    if (practiceTestResult == null)
                    {
                        return new SubmitPracticeTestResultResponse
                        {
                            Success = false,
                            Message = $"Practice test attempt not found for user {request.UserId}, test {request.PracticeTestId}, attempt {request.AttemptNumber}"
                        };
                    }

                    if (practiceTestResult.Status != "InProgress")
                    {
                        return new SubmitPracticeTestResultResponse
                        {
                            Success = false,
                            Message = "Practice test attempt is not in progress"
                        };
                    }

                    // Calculate totals
                    var totalObtainedMarks = request.QuestionResults.Sum(q => q.ObtainedMarks);
                    var percentage = practiceTestResult.TotalMarks > 0 ? (totalObtainedMarks / practiceTestResult.TotalMarks) * 100 : 0;
                    var timeTakenMinutes = (int)(request.EndTime - request.StartTime).TotalMinutes;
                    var isPassed = percentage >= practiceTestResult.PracticeTest.PassingPercentage;

                    // Update practice test result
                    practiceTestResult.CompletedAt = DateTime.UtcNow;
                    practiceTestResult.ObtainedMarks = totalObtainedMarks;
                    practiceTestResult.Percentage = percentage;
                    practiceTestResult.IsPassed = isPassed;
                    practiceTestResult.TimeTakenMinutes = timeTakenMinutes;
                    practiceTestResult.Status = "Completed";
                    practiceTestResult.UpdatedAt = DateTime.UtcNow;

                    // Create question results
                    foreach (var questionResult in request.QuestionResults)
                    {
                        // Get the practice test question
                        var practiceTestQuestion = await _context.PracticeTestQuestions
                            .FirstOrDefaultAsync(ptq => ptq.PracticeTestId == request.PracticeTestId &&
                                                       ptq.ProblemId == questionResult.ProblemId &&
                                                       ptq.QuestionOrder == questionResult.QuestionOrder);

                        if (practiceTestQuestion == null)
                        {
                            continue; // Skip if question not found
                        }

                        var questionResultEntity = new PracticeTestQuestionResult
                        {
                            PracticeTestResultId = practiceTestResult.Id,
                            PracticeTestQuestionId = practiceTestQuestion.Id,
                            ProblemId = questionResult.ProblemId,
                            QuestionOrder = questionResult.QuestionOrder,
                            SubmittedCode = questionResult.SubmittedCode,
                            Language = questionResult.Language,
                            Marks = questionResult.Marks,
                            ObtainedMarks = questionResult.ObtainedMarks,
                            IsCorrect = questionResult.IsCorrect,
                            ExecutionTime = questionResult.ExecutionTime,
                            MemoryUsed = questionResult.MemoryUsed,
                            TestCasesPassed = questionResult.TestCasesPassed,
                            TotalTestCases = questionResult.TotalTestCases,
                            ErrorMessage = questionResult.ErrorMessage,
                            CompilationStatus = questionResult.CompilationStatus,
                            ExecutionStatus = questionResult.ExecutionStatus,
                            SubmittedAt = DateTime.UtcNow,
                            TimeTakenMinutes = questionResult.TimeTakenMinutes
                        };

                        _context.PracticeTestQuestionResults.Add(questionResultEntity);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new SubmitPracticeTestResultResponse
                    {
                        PracticeTestId = request.PracticeTestId,
                        UserId = request.UserId,
                        AttemptNumber = request.AttemptNumber,
                        TotalMarks = practiceTestResult.TotalMarks,
                        ObtainedMarks = totalObtainedMarks,
                        Percentage = Math.Round(percentage, 2),
                        IsPassed = isPassed,
                        TimeTakenMinutes = timeTakenMinutes,
                        Status = "Completed",
                        SubmittedAt = DateTime.UtcNow,
                        Success = true,
                        Message = "Practice test result submitted successfully"
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var detailedMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        detailedMessage += $" Inner Exception: {ex.InnerException.Message}";
                    }

                    return new SubmitPracticeTestResultResponse
                    {
                        Success = false,
                        Message = $"Error submitting practice test result: {detailedMessage}"
                    };
                }
            });
        }

        public async Task<GetPracticeTestResultResponse> GetPracticeTestResultAsync(GetPracticeTestResultRequest request)
        {
            try
            {
                // Get practice test result
                var practiceTestResult = await _context.PracticeTestResults
                    .Include(ptr => ptr.PracticeTest)
                    .Include(ptr => ptr.QuestionResults)
                        .ThenInclude(qr => qr.Problem)
                    .FirstOrDefaultAsync(ptr => ptr.PracticeTestId == request.PracticeTestId &&
                                              ptr.UserId == request.UserId &&
                                              (request.AttemptNumber == null || ptr.AttemptNumber == request.AttemptNumber));

                if (practiceTestResult == null)
                {
                    return new GetPracticeTestResultResponse
                    {
                        Success = false,
                        Message = "Practice test result not found"
                    };
                }

                // Get question results
                var questionResults = new List<QuestionResultDetail>();
                foreach (var questionResult in practiceTestResult.QuestionResults.OrderBy(qr => qr.QuestionOrder))
                {
                    var questionResultDetail = new QuestionResultDetail
                    {
                        QuestionOrder = questionResult.QuestionOrder,
                        ProblemId = questionResult.ProblemId,
                        ProblemTitle = questionResult.Problem?.Title ?? "",
                        ProblemDescription = questionResult.Problem?.Description ?? "",
                        Language = questionResult.Language,
                        SubmittedCode = questionResult.SubmittedCode,
                        Marks = questionResult.Marks,
                        ObtainedMarks = questionResult.ObtainedMarks,
                        IsCorrect = questionResult.IsCorrect,
                        ExecutionTime = questionResult.ExecutionTime,
                        MemoryUsed = questionResult.MemoryUsed,
                        TestCasesPassed = questionResult.TestCasesPassed,
                        TotalTestCases = questionResult.TotalTestCases,
                        ErrorMessage = questionResult.ErrorMessage,
                        CompilationStatus = questionResult.CompilationStatus,
                        ExecutionStatus = questionResult.ExecutionStatus,
                        SubmittedAt = questionResult.SubmittedAt,
                        TimeTakenMinutes = questionResult.TimeTakenMinutes
                    };

                    questionResults.Add(questionResultDetail);
                }

                return new GetPracticeTestResultResponse
                {
                    PracticeTestId = practiceTestResult.PracticeTestId,
                    TestName = practiceTestResult.PracticeTest.TestName,
                    UserId = practiceTestResult.UserId,
                    AttemptNumber = practiceTestResult.AttemptNumber,
                    StartedAt = practiceTestResult.StartedAt,
                    CompletedAt = practiceTestResult.CompletedAt,
                    Status = practiceTestResult.Status,
                    TotalMarks = practiceTestResult.TotalMarks,
                    ObtainedMarks = practiceTestResult.ObtainedMarks,
                    Percentage = practiceTestResult.Percentage,
                    IsPassed = practiceTestResult.IsPassed,
                    TimeTakenMinutes = practiceTestResult.TimeTakenMinutes,
                    QuestionResults = questionResults,
                    Success = true,
                    Message = "Practice test result retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new GetPracticeTestResultResponse
                {
                    Success = false,
                    Message = $"Error retrieving practice test result: {ex.Message}"
                };
            }
        }

        public async Task<object> ValidatePracticeTestAsync(int practiceTestId, int userId, int attemptNumber)
        {
            try
            {
                // Check if practice test exists
                var practiceTest = await _context.PracticeTests
                    .Include(pt => pt.Questions)
                    .FirstOrDefaultAsync(pt => pt.Id == practiceTestId);

                // Check if attempt exists
                var attempt = await _context.PracticeTestResults
                    .FirstOrDefaultAsync(ptr => ptr.PracticeTestId == practiceTestId &&
                                              ptr.UserId == userId &&
                                              ptr.AttemptNumber == attemptNumber);

                return new
                {
                    PracticeTestId = practiceTestId,
                    UserId = userId,
                    AttemptNumber = attemptNumber,
                    PracticeTestExists = practiceTest != null,
                    PracticeTestName = practiceTest?.TestName,
                    PracticeTestActive = practiceTest?.IsActive,
                    PracticeTestPublished = practiceTest?.IsPublished,
                    QuestionCount = practiceTest?.Questions.Count ?? 0,
                    Questions = practiceTest?.Questions.Select(q => new { q.Id, q.ProblemId, q.QuestionOrder, q.Marks }).ToList(),
                    AttemptExists = attempt != null,
                    AttemptStatus = attempt?.Status,
                    AttemptStartedAt = attempt?.StartedAt,
                    CanSubmit = practiceTest != null && attempt != null && practiceTest.IsActive && practiceTest.IsPublished && attempt.Status == "InProgress",
                    Message = "Validation completed"
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    Error = ex.Message,
                    InnerError = ex.InnerException?.Message
                };
            }
        }
    }
}
