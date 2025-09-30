-- =============================================
-- Sample Data Insertion Script for Coding Test API
-- =============================================
-- This script inserts sample data for testing the Coding Test API
-- Make sure the tables are created first using CreateCodingTestTables.sql

USE [LeetCode]
GO

-- =============================================
-- Insert Sample Coding Tests
-- =============================================

-- Sample Test 1: Data Structures Practice
INSERT INTO [dbo].[CodingTests] (
    [TestName], [Description], [CreatedBy], [StartDate], [EndDate], 
    [DurationMinutes], [TotalQuestions], [TotalMarks], [TestType], 
    [Difficulty], [Category], [Instructions], [AllowMultipleAttempts], 
    [MaxAttempts], [ShowResultsImmediately], [AllowCodeReview], 
    [AccessCode], [Tags]
) VALUES (
    'Data Structures Practice Test',
    'A comprehensive test covering arrays, linked lists, and trees',
    1, -- CreatedBy (replace with actual user ID)
    '2024-01-20 10:00:00',
    '2024-01-20 12:00:00',
    120,
    3,
    60,
    'Practice',
    'Medium',
    'Data Structures',
    'Solve all problems within the time limit. You can run your code multiple times.',
    1,
    3,
    1,
    0,
    'DS123',
    'arrays,linked-lists,trees'
)

-- Sample Test 2: Algorithm Fundamentals
INSERT INTO [dbo].[CodingTests] (
    [TestName], [Description], [CreatedBy], [StartDate], [EndDate], 
    [DurationMinutes], [TotalQuestions], [TotalMarks], [TestType], 
    [Difficulty], [Category], [Instructions], [AllowMultipleAttempts], 
    [MaxAttempts], [ShowResultsImmediately], [AllowCodeReview], 
    [AccessCode], [Tags]
) VALUES (
    'Algorithm Fundamentals Assessment',
    'Test your understanding of basic algorithms and problem-solving',
    1, -- CreatedBy (replace with actual user ID)
    '2024-01-25 14:00:00',
    '2024-01-25 16:00:00',
    120,
    4,
    80,
    'Assessment',
    'Hard',
    'Algorithms',
    'This is a timed assessment. Complete all problems within the allocated time.',
    0,
    1,
    0,
    1,
    'ALGO456',
    'sorting,searching,dynamic-programming'
)

-- Sample Test 3: Easy Practice Session
INSERT INTO [dbo].[CodingTests] (
    [TestName], [Description], [CreatedBy], [StartDate], [EndDate], 
    [DurationMinutes], [TotalQuestions], [TotalMarks], [TestType], 
    [Difficulty], [Category], [Instructions], [AllowMultipleAttempts], 
    [MaxAttempts], [ShowResultsImmediately], [AllowCodeReview], 
    [AccessCode], [Tags]
) VALUES (
    'Easy Practice Session',
    'Beginner-friendly problems to build confidence',
    1, -- CreatedBy (replace with actual user ID)
    '2024-01-22 09:00:00',
    '2024-01-22 23:59:59',
    60,
    2,
    40,
    'Practice',
    'Easy',
    'Basics',
    'Take your time and learn from each problem. Multiple attempts allowed.',
    1,
    5,
    1,
    0,
    '',
    'basics,loops,conditionals'
)

-- =============================================
-- Insert Sample Questions for Test 1 (Data Structures)
-- =============================================

-- Get the test ID for the first test
DECLARE @Test1Id INT = (SELECT Id FROM [dbo].[CodingTests] WHERE [TestName] = 'Data Structures Practice Test')

-- Insert questions for Test 1 (assuming problems with IDs 1, 2, 3 exist)
INSERT INTO [dbo].[CodingTestQuestions] (
    [CodingTestId], [ProblemId], [QuestionOrder], [Marks], 
    [TimeLimitMinutes], [CustomInstructions]
) VALUES 
    (@Test1Id, 1, 1, 20, 30, 'Focus on time complexity optimization'),
    (@Test1Id, 2, 2, 20, 30, 'Consider edge cases and boundary conditions'),
    (@Test1Id, 3, 3, 20, 30, 'Optimize for both time and space complexity')

-- =============================================
-- Insert Sample Questions for Test 2 (Algorithms)
-- =============================================

DECLARE @Test2Id INT = (SELECT Id FROM [dbo].[CodingTests] WHERE [TestName] = 'Algorithm Fundamentals Assessment')

INSERT INTO [dbo].[CodingTestQuestions] (
    [CodingTestId], [ProblemId], [QuestionOrder], [Marks], 
    [TimeLimitMinutes], [CustomInstructions]
) VALUES 
    (@Test2Id, 1, 1, 20, 25, 'Implement efficient sorting algorithm'),
    (@Test2Id, 2, 2, 20, 25, 'Use binary search approach'),
    (@Test2Id, 3, 3, 20, 25, 'Apply dynamic programming principles'),
    (@Test2Id, 4, 4, 20, 25, 'Consider greedy algorithm approach')

-- =============================================
-- Insert Sample Questions for Test 3 (Easy Practice)
-- =============================================

DECLARE @Test3Id INT = (SELECT Id FROM [dbo].[CodingTests] WHERE [TestName] = 'Easy Practice Session')

INSERT INTO [dbo].[CodingTestQuestions] (
    [CodingTestId], [ProblemId], [QuestionOrder], [Marks], 
    [TimeLimitMinutes], [CustomInstructions]
) VALUES 
    (@Test3Id, 1, 1, 20, 30, 'Start with simple approach, then optimize'),
    (@Test3Id, 2, 2, 20, 30, 'Practice basic programming concepts')

-- =============================================
-- Insert Sample Test Attempts
-- =============================================

-- Sample attempt for Test 1
INSERT INTO [dbo].[CodingTestAttempts] (
    [CodingTestId], [UserId], [AttemptNumber], [StartedAt], 
    [CompletedAt], [SubmittedAt], [Status], [TotalScore], 
    [MaxScore], [Percentage], [TimeSpentMinutes], [IsLateSubmission], 
    [Notes]
) VALUES (
    @Test1Id, 201, 1, '2024-01-20 10:05:00', 
    '2024-01-20 11:45:00', '2024-01-20 11:45:00', 'Submitted', 
    45, 60, 75.0, 100, 0, 
    'Completed all questions successfully'
)

-- Sample attempt for Test 2
INSERT INTO [dbo].[CodingTestAttempts] (
    [CodingTestId], [UserId], [AttemptNumber], [StartedAt], 
    [CompletedAt], [SubmittedAt], [Status], [TotalScore], 
    [MaxScore], [Percentage], [TimeSpentMinutes], [IsLateSubmission], 
    [Notes]
) VALUES (
    @Test2Id, 202, 1, '2024-01-25 14:10:00', 
    '2024-01-25 15:50:00', '2024-01-25 15:50:00', 'Submitted', 
    60, 80, 75.0, 100, 0, 
    'Challenging but completed'
)

-- =============================================
-- Insert Sample Question Attempts
-- =============================================

-- Get attempt IDs
DECLARE @Attempt1Id INT = (SELECT Id FROM [dbo].[CodingTestAttempts] WHERE [CodingTestId] = @Test1Id AND [UserId] = 201)
DECLARE @Attempt2Id INT = (SELECT Id FROM [dbo].[CodingTestAttempts] WHERE [CodingTestId] = @Test2Id AND [UserId] = 202)

-- Get question IDs for Test 1
DECLARE @Question1Id INT = (SELECT Id FROM [dbo].[CodingTestQuestions] WHERE [CodingTestId] = @Test1Id AND [QuestionOrder] = 1)
DECLARE @Question2Id INT = (SELECT Id FROM [dbo].[CodingTestQuestions] WHERE [CodingTestId] = @Test1Id AND [QuestionOrder] = 2)
DECLARE @Question3Id INT = (SELECT Id FROM [dbo].[CodingTestQuestions] WHERE [CodingTestId] = @Test1Id AND [QuestionOrder] = 3)

-- Sample question attempts for Test 1
INSERT INTO [dbo].[CodingTestQuestionAttempts] (
    [CodingTestAttemptId], [CodingTestQuestionId], [ProblemId], [UserId], 
    [StartedAt], [CompletedAt], [Status], [LanguageUsed], [CodeSubmitted], 
    [Score], [MaxScore], [TestCasesPassed], [TotalTestCases], 
    [ExecutionTime], [RunCount], [SubmitCount], [IsCorrect]
) VALUES 
    (@Attempt1Id, @Question1Id, 1, 201, '2024-01-20 10:05:00', '2024-01-20 10:25:00', 
     'Completed', 'python', 'def solution(nums): return sum(nums)', 
     20, 20, 5, 5, 0.15, 3, 1, 1),
    
    (@Attempt1Id, @Question2Id, 2, 201, '2024-01-20 10:30:00', '2024-01-20 10:55:00', 
     'Completed', 'python', 'def solution(s): return s[::-1]', 
     15, 20, 4, 5, 0.12, 2, 1, 0),
    
    (@Attempt1Id, @Question3Id, 3, 201, '2024-01-20 11:00:00', '2024-01-20 11:40:00', 
     'Completed', 'python', 'def solution(root): return max_depth(root)', 
     10, 20, 3, 5, 0.18, 4, 1, 0)

-- =============================================
-- Verification Queries
-- =============================================

PRINT '============================================='
PRINT 'Sample Data Insertion Complete!'
PRINT '============================================='

-- Show inserted data
SELECT 'Coding Tests' as TableName, COUNT(*) as RecordCount FROM [dbo].[CodingTests]
UNION ALL
SELECT 'Coding Test Questions', COUNT(*) FROM [dbo].[CodingTestQuestions]
UNION ALL
SELECT 'Coding Test Attempts', COUNT(*) FROM [dbo].[CodingTestAttempts]
UNION ALL
SELECT 'Coding Test Question Attempts', COUNT(*) FROM [dbo].[CodingTestQuestionAttempts]

PRINT ''
PRINT 'Sample data includes:'
PRINT '- 3 Coding Tests (Data Structures, Algorithms, Easy Practice)'
PRINT '- 9 Coding Test Questions (3 per test)'
PRINT '- 2 Coding Test Attempts (by users 201 and 202)'
PRINT '- 3 Coding Test Question Attempts (for Test 1)'
PRINT ''
PRINT 'You can now test the API endpoints with this sample data!'
PRINT '============================================='
