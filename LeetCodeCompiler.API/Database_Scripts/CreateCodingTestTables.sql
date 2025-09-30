-- =============================================
-- Coding Test API Database Tables Creation Script
-- =============================================
-- This script creates all the necessary tables for the Coding Test API system
-- Run this script in your SQL Server database

USE [LeetCode]
GO

-- =============================================
-- 1. Create CodingTests Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CodingTests' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[CodingTests](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [TestName] [nvarchar](200) NOT NULL,
        [CreatedBy] [int] NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] [datetime2](7) NULL,
        [StartDate] [datetime2](7) NOT NULL,
        [EndDate] [datetime2](7) NOT NULL,
        [DurationMinutes] [int] NOT NULL,
        [TotalQuestions] [int] NOT NULL,
        [TotalMarks] [int] NOT NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [IsPublished] [bit] NOT NULL DEFAULT 0,
        [TestType] [nvarchar](50) NOT NULL DEFAULT 'Practice',
        [AllowMultipleAttempts] [bit] NOT NULL DEFAULT 0,
        [MaxAttempts] [int] NOT NULL DEFAULT 1,
        [ShowResultsImmediately] [bit] NOT NULL DEFAULT 1,
        [AllowCodeReview] [bit] NOT NULL DEFAULT 0,
        [AccessCode] [nvarchar](100) NULL,
        [Tags] [nvarchar](200) NULL,
        [IsResultPublishAutomatically] [bit] NOT NULL DEFAULT 1,
        [ApplyBreachRule] [bit] NOT NULL DEFAULT 1,
        [BreachRuleLimit] [int] NOT NULL DEFAULT 0,
        [HostIP] [nvarchar](50) NULL,
        [ClassId] [int] NOT NULL DEFAULT 0,
        CONSTRAINT [PK_CodingTests] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    
    PRINT 'CodingTests table created successfully'
END
ELSE
BEGIN
    PRINT 'CodingTests table already exists'
END
GO

-- =============================================
-- 2. Create CodingTestQuestions Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CodingTestQuestions' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[CodingTestQuestions](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CodingTestId] [int] NOT NULL,
        [ProblemId] [int] NOT NULL,
        [QuestionOrder] [int] NOT NULL,
        [Marks] [int] NOT NULL,
        [TimeLimitMinutes] [int] NOT NULL,
        [CustomInstructions] [nvarchar](1000) NULL,
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_CodingTestQuestions] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    
    -- Add foreign key constraint
    ALTER TABLE [dbo].[CodingTestQuestions]
    ADD CONSTRAINT [FK_CodingTestQuestions_CodingTests] 
    FOREIGN KEY([CodingTestId]) REFERENCES [dbo].[CodingTests] ([Id]) ON DELETE CASCADE
    
    ALTER TABLE [dbo].[CodingTestQuestions]
    ADD CONSTRAINT [FK_CodingTestQuestions_Problems] 
    FOREIGN KEY([ProblemId]) REFERENCES [dbo].[Problems] ([Id])
    
    PRINT 'CodingTestQuestions table created successfully'
END
ELSE
BEGIN
    PRINT 'CodingTestQuestions table already exists'
END
GO

-- =============================================
-- 3. Create CodingTestAttempts Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CodingTestAttempts' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[CodingTestAttempts](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CodingTestId] [int] NOT NULL,
        [UserId] [int] NOT NULL,
        [AttemptNumber] [int] NOT NULL,
        [StartedAt] [datetime2](7) NOT NULL,
        [CompletedAt] [datetime2](7) NULL,
        [SubmittedAt] [datetime2](7) NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'InProgress',
        [TotalScore] [int] NOT NULL DEFAULT 0,
        [MaxScore] [int] NOT NULL DEFAULT 0,
        [Percentage] [float] NOT NULL DEFAULT 0.0,
        [TimeSpentMinutes] [int] NOT NULL DEFAULT 0,
        [IsLateSubmission] [bit] NOT NULL DEFAULT 0,
        [Notes] [nvarchar](500) NULL,
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] [datetime2](7) NULL,
        CONSTRAINT [PK_CodingTestAttempts] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    
    -- Add foreign key constraint
    ALTER TABLE [dbo].[CodingTestAttempts]
    ADD CONSTRAINT [FK_CodingTestAttempts_CodingTests] 
    FOREIGN KEY([CodingTestId]) REFERENCES [dbo].[CodingTests] ([Id]) ON DELETE CASCADE
    
    PRINT 'CodingTestAttempts table created successfully'
END
ELSE
BEGIN
    PRINT 'CodingTestAttempts table already exists'
END
GO

-- =============================================
-- 4. Create CodingTestQuestionAttempts Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CodingTestQuestionAttempts' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[CodingTestQuestionAttempts](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CodingTestAttemptId] [int] NOT NULL,
        [CodingTestQuestionId] [int] NOT NULL,
        [ProblemId] [int] NOT NULL,
        [UserId] [int] NOT NULL,
        [StartedAt] [datetime2](7) NOT NULL,
        [CompletedAt] [datetime2](7) NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'InProgress',
        [LanguageUsed] [nvarchar](50) NOT NULL DEFAULT '',
        [CodeSubmitted] [nvarchar](max) NOT NULL DEFAULT '',
        [Score] [int] NOT NULL DEFAULT 0,
        [MaxScore] [int] NOT NULL DEFAULT 0,
        [TestCasesPassed] [int] NOT NULL DEFAULT 0,
        [TotalTestCases] [int] NOT NULL DEFAULT 0,
        [ExecutionTime] [float] NOT NULL DEFAULT 0.0,
        [RunCount] [int] NOT NULL DEFAULT 0,
        [SubmitCount] [int] NOT NULL DEFAULT 0,
        [IsCorrect] [bit] NOT NULL DEFAULT 0,
        [ErrorMessage] [nvarchar](1000) NULL,
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] [datetime2](7) NULL,
        CONSTRAINT [PK_CodingTestQuestionAttempts] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    
    -- Add foreign key constraints
    ALTER TABLE [dbo].[CodingTestQuestionAttempts]
    ADD CONSTRAINT [FK_CodingTestQuestionAttempts_CodingTestAttempts] 
    FOREIGN KEY([CodingTestAttemptId]) REFERENCES [dbo].[CodingTestAttempts] ([Id]) ON DELETE CASCADE
    
    ALTER TABLE [dbo].[CodingTestQuestionAttempts]
    ADD CONSTRAINT [FK_CodingTestQuestionAttempts_CodingTestQuestions] 
    FOREIGN KEY([CodingTestQuestionId]) REFERENCES [dbo].[CodingTestQuestions] ([Id])
    
    ALTER TABLE [dbo].[CodingTestQuestionAttempts]
    ADD CONSTRAINT [FK_CodingTestQuestionAttempts_Problems] 
    FOREIGN KEY([ProblemId]) REFERENCES [dbo].[Problems] ([Id])
    
    PRINT 'CodingTestQuestionAttempts table created successfully'
END
ELSE
BEGIN
    PRINT 'CodingTestQuestionAttempts table already exists'
END
GO

-- =============================================
-- 5. Create CodingTestTopicData Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CodingTestTopicData' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[CodingTestTopicData](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CodingTestId] [int] NOT NULL,
        [SectionId] [int] NOT NULL,
        [DomainId] [int] NOT NULL,
        [SubdomainId] [int] NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_CodingTestTopicData] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    
    -- Add foreign key constraint
    ALTER TABLE [dbo].[CodingTestTopicData]
    ADD CONSTRAINT [FK_CodingTestTopicData_CodingTests] 
    FOREIGN KEY([CodingTestId]) REFERENCES [dbo].[CodingTests] ([Id]) ON DELETE CASCADE
    
    PRINT 'CodingTestTopicData table created successfully'
END
ELSE
BEGIN
    PRINT 'CodingTestTopicData table already exists'
END
GO

-- =============================================
-- 6. Create Indexes for Performance
-- =============================================

-- Index on CodingTests for common queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTests_CreatedBy')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTests_CreatedBy] ON [dbo].[CodingTests]
    (
        [CreatedBy] ASC
    )
    PRINT 'Index IX_CodingTests_CreatedBy created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTests_StartDate_EndDate')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTests_StartDate_EndDate] ON [dbo].[CodingTests]
    (
        [StartDate] ASC,
        [EndDate] ASC
    )
    PRINT 'Index IX_CodingTests_StartDate_EndDate created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTests_IsActive_IsPublished')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTests_IsActive_IsPublished] ON [dbo].[CodingTests]
    (
        [IsActive] ASC,
        [IsPublished] ASC
    )
    PRINT 'Index IX_CodingTests_IsActive_IsPublished created'
END

-- Index on CodingTestQuestions
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestQuestions_CodingTestId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestQuestions_CodingTestId] ON [dbo].[CodingTestQuestions]
    (
        [CodingTestId] ASC
    )
    PRINT 'Index IX_CodingTestQuestions_CodingTestId created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestQuestions_ProblemId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestQuestions_ProblemId] ON [dbo].[CodingTestQuestions]
    (
        [ProblemId] ASC
    )
    PRINT 'Index IX_CodingTestQuestions_ProblemId created'
END

-- Index on CodingTestAttempts
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestAttempts_CodingTestId_UserId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestAttempts_CodingTestId_UserId] ON [dbo].[CodingTestAttempts]
    (
        [CodingTestId] ASC,
        [UserId] ASC
    )
    PRINT 'Index IX_CodingTestAttempts_CodingTestId_UserId created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestAttempts_Status')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestAttempts_Status] ON [dbo].[CodingTestAttempts]
    (
        [Status] ASC
    )
    PRINT 'Index IX_CodingTestAttempts_Status created'
END

-- Index on CodingTestQuestionAttempts
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestQuestionAttempts_CodingTestAttemptId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestQuestionAttempts_CodingTestAttemptId] ON [dbo].[CodingTestQuestionAttempts]
    (
        [CodingTestAttemptId] ASC
    )
    PRINT 'Index IX_CodingTestQuestionAttempts_CodingTestAttemptId created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestQuestionAttempts_UserId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestQuestionAttempts_UserId] ON [dbo].[CodingTestQuestionAttempts]
    (
        [UserId] ASC
    )
    PRINT 'Index IX_CodingTestQuestionAttempts_UserId created'
END

-- Index on CodingTestTopicData
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestTopicData_CodingTestId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestTopicData_CodingTestId] ON [dbo].[CodingTestTopicData]
    (
        [CodingTestId] ASC
    )
    PRINT 'Index IX_CodingTestTopicData_CodingTestId created'
END

-- =============================================
-- 7. Add Check Constraints
-- =============================================

-- Check constraints for CodingTests
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_CodingTests_DurationMinutes')
BEGIN
    ALTER TABLE [dbo].[CodingTests]
    ADD CONSTRAINT [CK_CodingTests_DurationMinutes] 
    CHECK ([DurationMinutes] > 0 AND [DurationMinutes] <= 480)
    PRINT 'Check constraint CK_CodingTests_DurationMinutes added'
END

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_CodingTests_TotalQuestions')
BEGIN
    ALTER TABLE [dbo].[CodingTests]
    ADD CONSTRAINT [CK_CodingTests_TotalQuestions] 
    CHECK ([TotalQuestions] > 0 AND [TotalQuestions] <= 100)
    PRINT 'Check constraint CK_CodingTests_TotalQuestions added'
END

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_CodingTests_TotalMarks')
BEGIN
    ALTER TABLE [dbo].[CodingTests]
    ADD CONSTRAINT [CK_CodingTests_TotalMarks] 
    CHECK ([TotalMarks] > 0 AND [TotalMarks] <= 1000)
    PRINT 'Check constraint CK_CodingTests_TotalMarks added'
END

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_CodingTests_MaxAttempts')
BEGIN
    ALTER TABLE [dbo].[CodingTests]
    ADD CONSTRAINT [CK_CodingTests_MaxAttempts] 
    CHECK ([MaxAttempts] > 0 AND [MaxAttempts] <= 10)
    PRINT 'Check constraint CK_CodingTests_MaxAttempts added'
END

-- Check constraints for CodingTestQuestions
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_CodingTestQuestions_Marks')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestions]
    ADD CONSTRAINT [CK_CodingTestQuestions_Marks] 
    CHECK ([Marks] > 0 AND [Marks] <= 100)
    PRINT 'Check constraint CK_CodingTestQuestions_Marks added'
END

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_CodingTestQuestions_TimeLimitMinutes')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestions]
    ADD CONSTRAINT [CK_CodingTestQuestions_TimeLimitMinutes] 
    CHECK ([TimeLimitMinutes] > 0 AND [TimeLimitMinutes] <= 120)
    PRINT 'Check constraint CK_CodingTestQuestions_TimeLimitMinutes added'
END

-- Check constraints for CodingTestAttempts
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_CodingTestAttempts_AttemptNumber')
BEGIN
    ALTER TABLE [dbo].[CodingTestAttempts]
    ADD CONSTRAINT [CK_CodingTestAttempts_AttemptNumber] 
    CHECK ([AttemptNumber] > 0)
    PRINT 'Check constraint CK_CodingTestAttempts_AttemptNumber added'
END

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_CodingTestAttempts_Percentage')
BEGIN
    ALTER TABLE [dbo].[CodingTestAttempts]
    ADD CONSTRAINT [CK_CodingTestAttempts_Percentage] 
    CHECK ([Percentage] >= 0.0 AND [Percentage] <= 100.0)
    PRINT 'Check constraint CK_CodingTestAttempts_Percentage added'
END

-- =============================================
-- 8. Sample Data Insertion (Optional)
-- =============================================

-- Uncomment the following section if you want to insert sample data

/*
-- Insert sample coding test
INSERT INTO [dbo].[CodingTests] (
    [TestName], [CreatedBy], [StartDate], [EndDate], 
    [DurationMinutes], [TotalQuestions], [TotalMarks], [TestType], 
    [AllowMultipleAttempts], [MaxAttempts], [ShowResultsImmediately], 
    [AllowCodeReview], [AccessCode], [Tags], [IsResultPublishAutomatically],
    [ApplyBreachRule], [BreachRuleLimit], [HostIP], [ClassId]
) VALUES (
    'Sample Data Structures Test',
    1, -- CreatedBy (replace with actual user ID)
    '2024-01-20 10:00:00',
    '2024-01-20 12:00:00',
    120,
    3,
    60,
    'Practice',
    1,
    3,
    1,
    0,
    'SAMPLE123',
    'arrays,linked-lists,trees',
    1,
    1,
    0,
    '127.0.0.1',
    1
)

-- Get the test ID for inserting questions
DECLARE @TestId INT = SCOPE_IDENTITY()

-- Insert sample questions (assuming problems with IDs 1, 2, 3 exist)
INSERT INTO [dbo].[CodingTestQuestions] (
    [CodingTestId], [ProblemId], [QuestionOrder], [Marks], 
    [TimeLimitMinutes], [CustomInstructions]
) VALUES 
    (@TestId, 1, 1, 20, 30, 'Focus on time complexity'),
    (@TestId, 2, 2, 20, 30, 'Consider edge cases'),
    (@TestId, 3, 3, 20, 30, 'Optimize space complexity')

-- Insert sample topic data
INSERT INTO [dbo].[CodingTestTopicData] (
    [CodingTestId], [SectionId], [DomainId], [SubdomainId]
) VALUES 
    (@TestId, 1, 1, 1),
    (@TestId, 2, 1, 2)

PRINT 'Sample data inserted successfully'
*/

-- =============================================
-- 9. Script Completion
-- =============================================
PRINT '============================================='
PRINT 'Coding Test API Database Tables Creation Complete!'
PRINT '============================================='
PRINT 'Tables created:'
PRINT '- CodingTests'
PRINT '- CodingTestQuestions' 
PRINT '- CodingTestAttempts'
PRINT '- CodingTestQuestionAttempts'
PRINT '- CodingTestTopicData'
PRINT ''
PRINT 'Indexes created for optimal performance'
PRINT 'Check constraints added for data integrity'
PRINT 'Foreign key relationships established'
PRINT ''
PRINT 'You can now use the Coding Test API endpoints!'
PRINT '============================================='
