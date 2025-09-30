-- =============================================
-- Quick Coding Test Tables Creation Script
-- =============================================
-- Simple version without indexes and constraints
-- Run this in SQL Server Management Studio

USE [LeetCode]
GO

-- 1. Create CodingTests Table
CREATE TABLE [dbo].[CodingTests](
    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [TestName] [nvarchar](200) NOT NULL,
    [Description] [nvarchar](1000) NULL,
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
    [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] [datetime2](7) NULL
)
GO

-- 2. Create CodingTestQuestions Table
CREATE TABLE [dbo].[CodingTestQuestions](
    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CodingTestId] [int] NOT NULL,
    [ProblemId] [int] NOT NULL,
    [QuestionOrder] [int] NOT NULL,
    [Marks] [int] NOT NULL,
    [TimeLimitMinutes] [int] NOT NULL,
    [CustomInstructions] [nvarchar](1000) NULL,
    [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETDATE()
)
GO

-- 3. Create CodingTestAttempts Table
CREATE TABLE [dbo].[CodingTestAttempts](
    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
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
    [UpdatedAt] [datetime2](7) NULL
)
GO

-- 4. Create CodingTestQuestionAttempts Table
CREATE TABLE [dbo].[CodingTestQuestionAttempts](
    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
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
    [UpdatedAt] [datetime2](7) NULL
)
GO

-- 5. Add Foreign Key Relationships
ALTER TABLE [dbo].[CodingTestQuestions]
ADD CONSTRAINT [FK_CodingTestQuestions_CodingTests] 
FOREIGN KEY([CodingTestId]) REFERENCES [dbo].[CodingTests] ([Id]) ON DELETE CASCADE
GO

ALTER TABLE [dbo].[CodingTestQuestions]
ADD CONSTRAINT [FK_CodingTestQuestions_Problems] 
FOREIGN KEY([ProblemId]) REFERENCES [dbo].[Problems] ([Id])
GO

ALTER TABLE [dbo].[CodingTestAttempts]
ADD CONSTRAINT [FK_CodingTestAttempts_CodingTests] 
FOREIGN KEY([CodingTestId]) REFERENCES [dbo].[CodingTests] ([Id]) ON DELETE CASCADE
GO

ALTER TABLE [dbo].[CodingTestQuestionAttempts]
ADD CONSTRAINT [FK_CodingTestQuestionAttempts_CodingTestAttempts] 
FOREIGN KEY([CodingTestAttemptId]) REFERENCES [dbo].[CodingTestAttempts] ([Id]) ON DELETE CASCADE
GO

ALTER TABLE [dbo].[CodingTestQuestionAttempts]
ADD CONSTRAINT [FK_CodingTestQuestionAttempts_CodingTestQuestions] 
FOREIGN KEY([CodingTestQuestionId]) REFERENCES [dbo].[CodingTestQuestions] ([Id])
GO

ALTER TABLE [dbo].[CodingTestQuestionAttempts]
ADD CONSTRAINT [FK_CodingTestQuestionAttempts_Problems] 
FOREIGN KEY([ProblemId]) REFERENCES [dbo].[Problems] ([Id])
GO

-- Create CodingTestTopicData table
CREATE TABLE [dbo].[CodingTestTopicData](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [CodingTestId] [int] NOT NULL,
    [SectionId] [int] NOT NULL,
    [DomainId] [int] NOT NULL,
    [SubdomainId] [int] NOT NULL,
    [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_CodingTestTopicData] PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[CodingTestTopicData]
ADD CONSTRAINT [FK_CodingTestTopicData_CodingTests] 
FOREIGN KEY([CodingTestId]) REFERENCES [dbo].[CodingTests] ([Id]) ON DELETE CASCADE
GO

PRINT 'Coding Test API tables created successfully!'
PRINT 'Tables: CodingTests, CodingTestQuestions, CodingTestAttempts, CodingTestQuestionAttempts, CodingTestTopicData'
