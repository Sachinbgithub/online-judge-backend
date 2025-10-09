-- =============================================
-- Practice Test API Database Tables Creation Script
-- =============================================
-- This script creates the necessary tables for the Practice Test API system
-- Run this script in your SQL Server database

USE [LeetCode]
GO

-- =============================================
-- 1. Create PracticeTests Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PracticeTests' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[PracticeTests](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [TestName] [nvarchar](200) NOT NULL,
        [Description] [nvarchar](1000) NULL,
        [DomainId] [int] NOT NULL,
        [SubdomainId] [int] NOT NULL,
        [TotalMarks] [int] NOT NULL,
        [DurationMinutes] [int] NOT NULL,
        [CreatedBy] [int] NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] [datetime2](7) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [IsPublished] [bit] NOT NULL DEFAULT 0,
        [AllowMultipleAttempts] [bit] NOT NULL DEFAULT 1,
        [MaxAttempts] [int] NOT NULL DEFAULT 3,
        [ShowResultsImmediately] [bit] NOT NULL DEFAULT 1,
        [DifficultyLevel] [nvarchar](20) NOT NULL DEFAULT 'Medium', -- Easy, Medium, Hard
        [Tags] [nvarchar](200) NULL,
        [Instructions] [nvarchar](2000) NULL,
        [PassingPercentage] [decimal](5,2) NOT NULL DEFAULT 60.00,
        CONSTRAINT [PK_PracticeTests] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    
    PRINT 'PracticeTests table created successfully'
END
ELSE
BEGIN
    PRINT 'PracticeTests table already exists'
END
GO

-- =============================================
-- 2. Create PracticeTestQuestions Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PracticeTestQuestions' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[PracticeTestQuestions](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [PracticeTestId] [int] NOT NULL,
        [ProblemId] [int] NOT NULL,
        [QuestionOrder] [int] NOT NULL,
        [Marks] [int] NOT NULL,
        [TimeLimitMinutes] [int] NULL, -- NULL means use test default
        [CustomInstructions] [nvarchar](1000) NULL,
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_PracticeTestQuestions] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    
    PRINT 'PracticeTestQuestions table created successfully'
END
ELSE
BEGIN
    PRINT 'PracticeTestQuestions table already exists'
END
GO

-- =============================================
-- 3. Create PracticeTestResults Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PracticeTestResults' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[PracticeTestResults](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [PracticeTestId] [int] NOT NULL,
        [UserId] [int] NOT NULL,
        [AttemptNumber] [int] NOT NULL DEFAULT 1,
        [StartedAt] [datetime2](7) NOT NULL DEFAULT GETDATE(),
        [CompletedAt] [datetime2](7) NULL,
        [TotalMarks] [int] NOT NULL,
        [ObtainedMarks] [decimal](10,2) NOT NULL DEFAULT 0.00,
        [Percentage] [decimal](5,2) NOT NULL DEFAULT 0.00,
        [IsPassed] [bit] NOT NULL DEFAULT 0,
        [TimeTakenMinutes] [int] NULL,
        [Status] [nvarchar](20) NOT NULL DEFAULT 'InProgress', -- InProgress, Completed, Abandoned, Timeout
        [SubmissionData] [nvarchar](max) NULL, -- JSON data for detailed results
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] [datetime2](7) NULL,
        CONSTRAINT [PK_PracticeTestResults] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    
    PRINT 'PracticeTestResults table created successfully'
END
ELSE
BEGIN
    PRINT 'PracticeTestResults table already exists'
END
GO

-- =============================================
-- 4. Create PracticeTestQuestionResults Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PracticeTestQuestionResults' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[PracticeTestQuestionResults](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [PracticeTestResultId] [int] NOT NULL,
        [PracticeTestQuestionId] [int] NOT NULL,
        [ProblemId] [int] NOT NULL,
        [QuestionOrder] [int] NOT NULL,
        [SubmittedCode] [nvarchar](max) NULL,
        [Language] [nvarchar](50) NOT NULL,
        [Marks] [int] NOT NULL,
        [ObtainedMarks] [decimal](10,2) NOT NULL DEFAULT 0.00,
        [IsCorrect] [bit] NOT NULL DEFAULT 0,
        [ExecutionTime] [int] NULL, -- in milliseconds
        [MemoryUsed] [int] NULL, -- in KB
        [TestCasesPassed] [int] NOT NULL DEFAULT 0,
        [TotalTestCases] [int] NOT NULL DEFAULT 0,
        [ErrorMessage] [nvarchar](max) NULL,
        [CompilationStatus] [nvarchar](20) NOT NULL DEFAULT 'Pending', -- Pending, Success, Failed
        [ExecutionStatus] [nvarchar](20) NOT NULL DEFAULT 'Pending', -- Pending, Success, Failed, Timeout
        [SubmittedAt] [datetime2](7) NOT NULL DEFAULT GETDATE(),
        [TimeTakenMinutes] [int] NULL,
        CONSTRAINT [PK_PracticeTestQuestionResults] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    
    PRINT 'PracticeTestQuestionResults table created successfully'
END
ELSE
BEGIN
    PRINT 'PracticeTestQuestionResults table already exists'
END
GO

-- =============================================
-- 5. Add Foreign Key Constraints
-- =============================================

-- PracticeTests foreign keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PracticeTests_Domains')
BEGIN
    ALTER TABLE [dbo].[PracticeTests]
    ADD CONSTRAINT [FK_PracticeTests_Domains] 
    FOREIGN KEY([DomainId]) REFERENCES [dbo].[Domain] ([DomainId])
    PRINT 'Foreign key FK_PracticeTests_Domains added'
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PracticeTests_Subdomains')
BEGIN
    ALTER TABLE [dbo].[PracticeTests]
    ADD CONSTRAINT [FK_PracticeTests_Subdomains] 
    FOREIGN KEY([SubdomainId]) REFERENCES [dbo].[Subdomain] ([SubdomainId])
    PRINT 'Foreign key FK_PracticeTests_Subdomains added'
END

-- PracticeTestQuestions foreign keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PracticeTestQuestions_PracticeTests')
BEGIN
    ALTER TABLE [dbo].[PracticeTestQuestions]
    ADD CONSTRAINT [FK_PracticeTestQuestions_PracticeTests] 
    FOREIGN KEY([PracticeTestId]) REFERENCES [dbo].[PracticeTests] ([Id]) ON DELETE CASCADE
    PRINT 'Foreign key FK_PracticeTestQuestions_PracticeTests added'
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PracticeTestQuestions_Problems')
BEGIN
    ALTER TABLE [dbo].[PracticeTestQuestions]
    ADD CONSTRAINT [FK_PracticeTestQuestions_Problems] 
    FOREIGN KEY([ProblemId]) REFERENCES [dbo].[Problems] ([Id])
    PRINT 'Foreign key FK_PracticeTestQuestions_Problems added'
END

-- PracticeTestResults foreign keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PracticeTestResults_PracticeTests')
BEGIN
    ALTER TABLE [dbo].[PracticeTestResults]
    ADD CONSTRAINT [FK_PracticeTestResults_PracticeTests] 
    FOREIGN KEY([PracticeTestId]) REFERENCES [dbo].[PracticeTests] ([Id]) ON DELETE CASCADE
    PRINT 'Foreign key FK_PracticeTestResults_PracticeTests added'
END

-- PracticeTestQuestionResults foreign keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PracticeTestQuestionResults_PracticeTestResults')
BEGIN
    ALTER TABLE [dbo].[PracticeTestQuestionResults]
    ADD CONSTRAINT [FK_PracticeTestQuestionResults_PracticeTestResults] 
    FOREIGN KEY([PracticeTestResultId]) REFERENCES [dbo].[PracticeTestResults] ([Id]) ON DELETE CASCADE
    PRINT 'Foreign key FK_PracticeTestQuestionResults_PracticeTestResults added'
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PracticeTestQuestionResults_PracticeTestQuestions')
BEGIN
    ALTER TABLE [dbo].[PracticeTestQuestionResults]
    ADD CONSTRAINT [FK_PracticeTestQuestionResults_PracticeTestQuestions] 
    FOREIGN KEY([PracticeTestQuestionId]) REFERENCES [dbo].[PracticeTestQuestions] ([Id])
    PRINT 'Foreign key FK_PracticeTestQuestionResults_PracticeTestQuestions added'
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_PracticeTestQuestionResults_Problems')
BEGIN
    ALTER TABLE [dbo].[PracticeTestQuestionResults]
    ADD CONSTRAINT [FK_PracticeTestQuestionResults_Problems] 
    FOREIGN KEY([ProblemId]) REFERENCES [dbo].[Problems] ([Id])
    PRINT 'Foreign key FK_PracticeTestQuestionResults_Problems added'
END

GO

-- =============================================
-- 6. Create Indexes for Performance
-- =============================================

-- Indexes on PracticeTests
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PracticeTests_DomainId_SubdomainId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PracticeTests_DomainId_SubdomainId] ON [dbo].[PracticeTests]
    (
        [DomainId] ASC,
        [SubdomainId] ASC
    )
    PRINT 'Index IX_PracticeTests_DomainId_SubdomainId created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PracticeTests_IsActive_IsPublished')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PracticeTests_IsActive_IsPublished] ON [dbo].[PracticeTests]
    (
        [IsActive] ASC,
        [IsPublished] ASC
    )
    PRINT 'Index IX_PracticeTests_IsActive_IsPublished created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PracticeTests_CreatedBy')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PracticeTests_CreatedBy] ON [dbo].[PracticeTests]
    (
        [CreatedBy] ASC
    )
    PRINT 'Index IX_PracticeTests_CreatedBy created'
END

-- Indexes on PracticeTestQuestions
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PracticeTestQuestions_PracticeTestId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PracticeTestQuestions_PracticeTestId] ON [dbo].[PracticeTestQuestions]
    (
        [PracticeTestId] ASC
    )
    PRINT 'Index IX_PracticeTestQuestions_PracticeTestId created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PracticeTestQuestions_QuestionOrder')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PracticeTestQuestions_QuestionOrder] ON [dbo].[PracticeTestQuestions]
    (
        [PracticeTestId] ASC,
        [QuestionOrder] ASC
    )
    PRINT 'Index IX_PracticeTestQuestions_QuestionOrder created'
END

-- Indexes on PracticeTestResults
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PracticeTestResults_PracticeTestId_UserId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PracticeTestResults_PracticeTestId_UserId] ON [dbo].[PracticeTestResults]
    (
        [PracticeTestId] ASC,
        [UserId] ASC
    )
    PRINT 'Index IX_PracticeTestResults_PracticeTestId_UserId created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PracticeTestResults_UserId_AttemptNumber')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PracticeTestResults_UserId_AttemptNumber] ON [dbo].[PracticeTestResults]
    (
        [UserId] ASC,
        [AttemptNumber] ASC
    )
    PRINT 'Index IX_PracticeTestResults_UserId_AttemptNumber created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PracticeTestResults_Status')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PracticeTestResults_Status] ON [dbo].[PracticeTestResults]
    (
        [Status] ASC
    )
    PRINT 'Index IX_PracticeTestResults_Status created'
END

-- Indexes on PracticeTestQuestionResults
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PracticeTestQuestionResults_PracticeTestResultId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PracticeTestQuestionResults_PracticeTestResultId] ON [dbo].[PracticeTestQuestionResults]
    (
        [PracticeTestResultId] ASC
    )
    PRINT 'Index IX_PracticeTestQuestionResults_PracticeTestResultId created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PracticeTestQuestionResults_QuestionOrder')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PracticeTestQuestionResults_QuestionOrder] ON [dbo].[PracticeTestQuestionResults]
    (
        [PracticeTestResultId] ASC,
        [QuestionOrder] ASC
    )
    PRINT 'Index IX_PracticeTestQuestionResults_QuestionOrder created'
END

GO

-- =============================================
-- 7. Add Check Constraints
-- =============================================

-- Check constraints for PracticeTests
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_PracticeTests_TotalMarks_Positive')
BEGIN
    ALTER TABLE [dbo].[PracticeTests]
    ADD CONSTRAINT [CK_PracticeTests_TotalMarks_Positive] 
    CHECK ([TotalMarks] > 0)
    PRINT 'Check constraint CK_PracticeTests_TotalMarks_Positive added'
END

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_PracticeTests_DurationMinutes_Positive')
BEGIN
    ALTER TABLE [dbo].[PracticeTests]
    ADD CONSTRAINT [CK_PracticeTests_DurationMinutes_Positive] 
    CHECK ([DurationMinutes] > 0)
    PRINT 'Check constraint CK_PracticeTests_DurationMinutes_Positive added'
END

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_PracticeTests_MaxAttempts_Positive')
BEGIN
    ALTER TABLE [dbo].[PracticeTests]
    ADD CONSTRAINT [CK_PracticeTests_MaxAttempts_Positive] 
    CHECK ([MaxAttempts] > 0)
    PRINT 'Check constraint CK_PracticeTests_MaxAttempts_Positive added'
END

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_PracticeTests_PassingPercentage_Range')
BEGIN
    ALTER TABLE [dbo].[PracticeTests]
    ADD CONSTRAINT [CK_PracticeTests_PassingPercentage_Range] 
    CHECK ([PassingPercentage] >= 0 AND [PassingPercentage] <= 100)
    PRINT 'Check constraint CK_PracticeTests_PassingPercentage_Range added'
END

-- Check constraints for PracticeTestQuestions
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_PracticeTestQuestions_Marks_Positive')
BEGIN
    ALTER TABLE [dbo].[PracticeTestQuestions]
    ADD CONSTRAINT [CK_PracticeTestQuestions_Marks_Positive] 
    CHECK ([Marks] > 0)
    PRINT 'Check constraint CK_PracticeTestQuestions_Marks_Positive added'
END

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_PracticeTestQuestions_QuestionOrder_Positive')
BEGIN
    ALTER TABLE [dbo].[PracticeTestQuestions]
    ADD CONSTRAINT [CK_PracticeTestQuestions_QuestionOrder_Positive] 
    CHECK ([QuestionOrder] > 0)
    PRINT 'Check constraint CK_PracticeTestQuestions_QuestionOrder_Positive added'
END

-- Check constraints for PracticeTestResults
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_PracticeTestResults_AttemptNumber_Positive')
BEGIN
    ALTER TABLE [dbo].[PracticeTestResults]
    ADD CONSTRAINT [CK_PracticeTestResults_AttemptNumber_Positive] 
    CHECK ([AttemptNumber] > 0)
    PRINT 'Check constraint CK_PracticeTestResults_AttemptNumber_Positive added'
END

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_PracticeTestResults_ObtainedMarks_NonNegative')
BEGIN
    ALTER TABLE [dbo].[PracticeTestResults]
    ADD CONSTRAINT [CK_PracticeTestResults_ObtainedMarks_NonNegative] 
    CHECK ([ObtainedMarks] >= 0)
    PRINT 'Check constraint CK_PracticeTestResults_ObtainedMarks_NonNegative added'
END

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_PracticeTestResults_Percentage_Range')
BEGIN
    ALTER TABLE [dbo].[PracticeTestResults]
    ADD CONSTRAINT [CK_PracticeTestResults_Percentage_Range] 
    CHECK ([Percentage] >= 0 AND [Percentage] <= 100)
    PRINT 'Check constraint CK_PracticeTestResults_Percentage_Range added'
END

GO

PRINT ''
PRINT '============================================='
PRINT 'Practice Test API tables created successfully!'
PRINT '============================================='
PRINT 'Tables created:'
PRINT '- PracticeTests'
PRINT '- PracticeTestQuestions'
PRINT '- PracticeTestResults'
PRINT '- PracticeTestQuestionResults'
PRINT ''
PRINT 'Foreign keys, indexes, and constraints added!'
PRINT 'You can now use the Practice Test API!'
PRINT '============================================='
