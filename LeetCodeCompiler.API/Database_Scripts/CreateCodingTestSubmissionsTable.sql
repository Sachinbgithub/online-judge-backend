-- =============================================
-- Coding Test Submissions Table Creation Script
-- =============================================
-- This script creates a comprehensive table for tracking coding test submissions
-- It integrates with existing tables and provides detailed submission tracking
-- Run this script in your SQL Server database

USE [LeetCode]
GO

-- =============================================
-- Create CodingTestSubmissions Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CodingTestSubmissions](
        [SubmissionId] BIGINT IDENTITY(1000001,1) NOT NULL,
        [CodingTestId] INT NOT NULL,
        [CodingTestAttemptId] INT NOT NULL,
        [CodingTestQuestionAttemptId] INT NOT NULL,
        [ProblemId] INT NOT NULL,
        [UserId] BIGINT NOT NULL,
        [AttemptNumber] INT NOT NULL,
        [LanguageUsed] NVARCHAR(50) NOT NULL,
        [FinalCodeSnapshot] NVARCHAR(MAX) NOT NULL,
        [TotalTestCases] INT NOT NULL DEFAULT 0,
        [PassedTestCases] INT NOT NULL DEFAULT 0,
        [FailedTestCases] INT NOT NULL DEFAULT 0,
        [RequestedHelp] BIT NOT NULL DEFAULT 0,
        
        -- Activity Tracking Metrics
        [LanguageSwitchCount] INT NOT NULL DEFAULT 0,
        [RunClickCount] INT NOT NULL DEFAULT 0,
        [SubmitClickCount] INT NOT NULL DEFAULT 0,
        [EraseCount] INT NOT NULL DEFAULT 0,
        [SaveCount] INT NOT NULL DEFAULT 0,
        [LoginLogoutCount] INT NOT NULL DEFAULT 0,
        [IsSessionAbandoned] BIT NOT NULL DEFAULT 0,
        
        -- Submission Details
        [SubmissionTime] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
        [ExecutionTimeMs] INT NOT NULL DEFAULT 0,
        [MemoryUsedKB] INT NOT NULL DEFAULT 0,
        [Score] INT NOT NULL DEFAULT 0,
        [MaxScore] INT NOT NULL DEFAULT 0,
        [IsCorrect] BIT NOT NULL DEFAULT 0,
        [IsLateSubmission] BIT NOT NULL DEFAULT 0,
        
        -- Additional Metadata
        [ClassId] INT NULL,
        [UserIP] NVARCHAR(50) NULL,
        [UserAgent] NVARCHAR(500) NULL,
        [BrowserInfo] NVARCHAR(200) NULL,
        [DeviceInfo] NVARCHAR(200) NULL,
        
        -- Error Handling
        [ErrorMessage] NVARCHAR(1000) NULL,
        [ErrorType] NVARCHAR(100) NULL,
        [CompilationError] NVARCHAR(1000) NULL,
        [RuntimeError] NVARCHAR(1000) NULL,
        
        -- Timestamps
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME2(7) NULL,
        
        CONSTRAINT [PK_CodingTestSubmissions] PRIMARY KEY CLUSTERED 
        (
            [SubmissionId] ASC
        )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
    
    PRINT 'CodingTestSubmissions table created successfully'
END
ELSE
BEGIN
    PRINT 'CodingTestSubmissions table already exists'
END
GO

-- =============================================
-- Add Foreign Key Constraints
-- =============================================

-- Foreign Key to CodingTests
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_CodingTestSubmissions_CodingTests_CodingTestId]') AND parent_object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissions]'))
BEGIN
    ALTER TABLE [dbo].[CodingTestSubmissions] 
    WITH CHECK ADD CONSTRAINT [FK_CodingTestSubmissions_CodingTests_CodingTestId] 
    FOREIGN KEY([CodingTestId]) REFERENCES [dbo].[CodingTests] ([Id]) ON DELETE CASCADE
END
GO

ALTER TABLE [dbo].[CodingTestSubmissions] CHECK CONSTRAINT [FK_CodingTestSubmissions_CodingTests_CodingTestId]
GO

-- Foreign Key to CodingTestAttempts
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_CodingTestSubmissions_CodingTestAttempts_CodingTestAttemptId]') AND parent_object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissions]'))
BEGIN
    ALTER TABLE [dbo].[CodingTestSubmissions] 
    WITH CHECK ADD CONSTRAINT [FK_CodingTestSubmissions_CodingTestAttempts_CodingTestAttemptId] 
    FOREIGN KEY([CodingTestAttemptId]) REFERENCES [dbo].[CodingTestAttempts] ([Id]) ON DELETE NO ACTION
END
GO

ALTER TABLE [dbo].[CodingTestSubmissions] CHECK CONSTRAINT [FK_CodingTestSubmissions_CodingTestAttempts_CodingTestAttemptId]
GO

-- Foreign Key to CodingTestQuestionAttempts
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_CodingTestSubmissions_CodingTestQuestionAttempts_CodingTestQuestionAttemptId]') AND parent_object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissions]'))
BEGIN
    ALTER TABLE [dbo].[CodingTestSubmissions] 
    WITH CHECK ADD CONSTRAINT [FK_CodingTestSubmissions_CodingTestQuestionAttempts_CodingTestQuestionAttemptId] 
    FOREIGN KEY([CodingTestQuestionAttemptId]) REFERENCES [dbo].[CodingTestQuestionAttempts] ([Id]) ON DELETE NO ACTION
END
GO

ALTER TABLE [dbo].[CodingTestSubmissions] CHECK CONSTRAINT [FK_CodingTestSubmissions_CodingTestQuestionAttempts_CodingTestQuestionAttemptId]
GO

-- Foreign Key to Problems
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_CodingTestSubmissions_Problems_ProblemId]') AND parent_object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissions]'))
BEGIN
    ALTER TABLE [dbo].[CodingTestSubmissions] 
    WITH CHECK ADD CONSTRAINT [FK_CodingTestSubmissions_Problems_ProblemId] 
    FOREIGN KEY([ProblemId]) REFERENCES [dbo].[Problems] ([Id])
END
GO

ALTER TABLE [dbo].[CodingTestSubmissions] CHECK CONSTRAINT [FK_CodingTestSubmissions_Problems_ProblemId]
GO

-- =============================================
-- Create Indexes for Performance
-- =============================================

-- Index for quick lookup by UserId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_CodingTestSubmissions_UserId' AND object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissions]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestSubmissions_UserId] ON [dbo].[CodingTestSubmissions]
    (
        [UserId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END
GO

-- Index for quick lookup by CodingTestId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_CodingTestSubmissions_CodingTestId' AND object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissions]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestSubmissions_CodingTestId] ON [dbo].[CodingTestSubmissions]
    (
        [CodingTestId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END
GO

-- Index for quick lookup by ProblemId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_CodingTestSubmissions_ProblemId' AND object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissions]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestSubmissions_ProblemId] ON [dbo].[CodingTestSubmissions]
    (
        [ProblemId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END
GO

-- Index for submission time queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_CodingTestSubmissions_SubmissionTime' AND object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissions]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestSubmissions_SubmissionTime] ON [dbo].[CodingTestSubmissions]
    (
        [SubmissionTime] DESC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END
GO

-- Composite index for user and test queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_CodingTestSubmissions_UserId_CodingTestId' AND object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissions]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestSubmissions_UserId_CodingTestId] ON [dbo].[CodingTestSubmissions]
    (
        [UserId] ASC,
        [CodingTestId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END
GO

-- Composite index for attempt tracking
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_CodingTestSubmissions_UserId_ProblemId_AttemptNumber' AND object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissions]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestSubmissions_UserId_ProblemId_AttemptNumber] ON [dbo].[CodingTestSubmissions]
    (
        [UserId] ASC,
        [ProblemId] ASC,
        [AttemptNumber] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END
GO

-- =============================================
-- Create CodingTestSubmissionResults Table (for detailed test case results)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissionResults]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CodingTestSubmissionResults](
        [ResultId] BIGINT IDENTITY(1000001,1) NOT NULL,
        [SubmissionId] BIGINT NOT NULL,
        [TestCaseId] INT NOT NULL,
        [TestCaseOrder] INT NOT NULL,
        [Input] NVARCHAR(MAX) NOT NULL,
        [ExpectedOutput] NVARCHAR(MAX) NOT NULL,
        [ActualOutput] NVARCHAR(MAX) NULL,
        [IsPassed] BIT NOT NULL DEFAULT 0,
        [ExecutionTimeMs] INT NOT NULL DEFAULT 0,
        [MemoryUsedKB] INT NOT NULL DEFAULT 0,
        [ErrorMessage] NVARCHAR(1000) NULL,
        [ErrorType] NVARCHAR(100) NULL, -- CompilationError, RuntimeError, TimeoutError, WrongAnswer
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT [PK_CodingTestSubmissionResults] PRIMARY KEY CLUSTERED 
        (
            [ResultId] ASC
        )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
    
    PRINT 'CodingTestSubmissionResults table created successfully'
END
ELSE
BEGIN
    PRINT 'CodingTestSubmissionResults table already exists'
END
GO

-- Foreign Key to CodingTestSubmissions
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_CodingTestSubmissionResults_CodingTestSubmissions_SubmissionId]') AND parent_object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissionResults]'))
BEGIN
    ALTER TABLE [dbo].[CodingTestSubmissionResults] 
    WITH CHECK ADD CONSTRAINT [FK_CodingTestSubmissionResults_CodingTestSubmissions_SubmissionId] 
    FOREIGN KEY([SubmissionId]) REFERENCES [dbo].[CodingTestSubmissions] ([SubmissionId]) ON DELETE CASCADE
END
GO

ALTER TABLE [dbo].[CodingTestSubmissionResults] CHECK CONSTRAINT [FK_CodingTestSubmissionResults_CodingTestSubmissions_SubmissionId]
GO

-- Foreign Key to TestCases
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_CodingTestSubmissionResults_TestCases_TestCaseId]') AND parent_object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissionResults]'))
BEGIN
    ALTER TABLE [dbo].[CodingTestSubmissionResults] 
    WITH CHECK ADD CONSTRAINT [FK_CodingTestSubmissionResults_TestCases_TestCaseId] 
    FOREIGN KEY([TestCaseId]) REFERENCES [dbo].[TestCases] ([Id])
END
GO

ALTER TABLE [dbo].[CodingTestSubmissionResults] CHECK CONSTRAINT [FK_CodingTestSubmissionResults_TestCases_TestCaseId]
GO

-- Index for SubmissionId queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_CodingTestSubmissionResults_SubmissionId' AND object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissionResults]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestSubmissionResults_SubmissionId] ON [dbo].[CodingTestSubmissionResults]
    (
        [SubmissionId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
END
GO

-- =============================================
-- Insert Sample Data (Optional)
-- =============================================
PRINT ''
PRINT 'Inserting sample data...'
PRINT '----------------------------------------'

-- Note: Sample data will only be inserted if the tables are empty
-- This ensures we don't duplicate data if the script is run multiple times

IF NOT EXISTS (SELECT TOP 1 1 FROM [dbo].[CodingTestSubmissions])
BEGIN
    PRINT 'No existing submissions found. Sample data insertion skipped.'
    PRINT 'You can insert sample data manually after creating coding tests and attempts.'
END
ELSE
BEGIN
    PRINT 'Existing submissions found. Sample data insertion skipped.'
END

-- =============================================
-- Create Views for Common Queries
-- =============================================

-- View for submission summary
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_CodingTestSubmissionSummary')
    DROP VIEW [dbo].[vw_CodingTestSubmissionSummary]
GO

CREATE VIEW [dbo].[vw_CodingTestSubmissionSummary]
AS
SELECT 
    s.SubmissionId,
    s.CodingTestId,
    ct.TestName,
    s.ProblemId,
    p.Title AS ProblemTitle,
    s.UserId,
    s.AttemptNumber,
    s.LanguageUsed,
    s.TotalTestCases,
    s.PassedTestCases,
    s.FailedTestCases,
    s.Score,
    s.MaxScore,
    s.IsCorrect,
    s.IsLateSubmission,
    s.SubmissionTime,
    s.ExecutionTimeMs,
    s.MemoryUsedKB,
    s.LanguageSwitchCount,
    s.RunClickCount,
    s.SubmitClickCount,
    s.EraseCount,
    s.SaveCount,
    s.LoginLogoutCount,
    s.IsSessionAbandoned,
    s.ClassId,
    s.CreatedAt
FROM [dbo].[CodingTestSubmissions] s
INNER JOIN [dbo].[CodingTests] ct ON s.CodingTestId = ct.Id
INNER JOIN [dbo].[Problems] p ON s.ProblemId = p.Id
GO

-- View for detailed submission results
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_CodingTestSubmissionDetails')
    DROP VIEW [dbo].[vw_CodingTestSubmissionDetails]
GO

CREATE VIEW [dbo].[vw_CodingTestSubmissionDetails]
AS
SELECT 
    s.SubmissionId,
    s.CodingTestId,
    ct.TestName,
    s.ProblemId,
    p.Title AS ProblemTitle,
    s.UserId,
    s.AttemptNumber,
    s.LanguageUsed,
    s.FinalCodeSnapshot,
    s.TotalTestCases,
    s.PassedTestCases,
    s.FailedTestCases,
    s.Score,
    s.MaxScore,
    s.IsCorrect,
    s.IsLateSubmission,
    s.SubmissionTime,
    s.ExecutionTimeMs,
    s.MemoryUsedKB,
    s.ErrorMessage,
    s.ErrorType,
    s.CompilationError,
    s.RuntimeError,
    s.LanguageSwitchCount,
    s.RunClickCount,
    s.SubmitClickCount,
    s.EraseCount,
    s.SaveCount,
    s.LoginLogoutCount,
    s.IsSessionAbandoned,
    s.ClassId,
    s.CreatedAt,
    -- Test case results
    r.ResultId,
    r.TestCaseId,
    r.TestCaseOrder,
    r.Input,
    r.ExpectedOutput,
    r.ActualOutput,
    r.IsPassed AS TestCasePassed,
    r.ExecutionTimeMs AS TestCaseExecutionTimeMs,
    r.MemoryUsedKB AS TestCaseMemoryUsedKB,
    r.ErrorMessage AS TestCaseErrorMessage,
    r.ErrorType AS TestCaseErrorType
FROM [dbo].[CodingTestSubmissions] s
INNER JOIN [dbo].[CodingTests] ct ON s.CodingTestId = ct.Id
INNER JOIN [dbo].[Problems] p ON s.ProblemId = p.Id
LEFT JOIN [dbo].[CodingTestSubmissionResults] r ON s.SubmissionId = r.SubmissionId
GO

-- =============================================
-- Create Stored Procedures for Common Operations
-- =============================================

-- Stored procedure to get user submission history
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetUserCodingTestSubmissions')
    DROP PROCEDURE [dbo].[sp_GetUserCodingTestSubmissions]
GO

CREATE PROCEDURE [dbo].[sp_GetUserCodingTestSubmissions]
    @UserId BIGINT,
    @CodingTestId INT = NULL,
    @ProblemId INT = NULL,
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        SubmissionId,
        CodingTestId,
        TestName,
        ProblemId,
        ProblemTitle,
        AttemptNumber,
        LanguageUsed,
        TotalTestCases,
        PassedTestCases,
        FailedTestCases,
        Score,
        MaxScore,
        IsCorrect,
        IsLateSubmission,
        SubmissionTime,
        ExecutionTimeMs,
        MemoryUsedKB,
        LanguageSwitchCount,
        RunClickCount,
        SubmitClickCount,
        EraseCount,
        SaveCount,
        LoginLogoutCount,
        IsSessionAbandoned,
        ClassId,
        CreatedAt
    FROM [dbo].[vw_CodingTestSubmissionSummary]
    WHERE UserId = @UserId
        AND (@CodingTestId IS NULL OR CodingTestId = @CodingTestId)
        AND (@ProblemId IS NULL OR ProblemId = @ProblemId)
        AND (@StartDate IS NULL OR SubmissionTime >= @StartDate)
        AND (@EndDate IS NULL OR SubmissionTime <= @EndDate)
    ORDER BY SubmissionTime DESC
END
GO

-- Stored procedure to get test statistics
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetCodingTestStatistics')
    DROP PROCEDURE [dbo].[sp_GetCodingTestStatistics]
GO

CREATE PROCEDURE [dbo].[sp_GetCodingTestStatistics]
    @CodingTestId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        CodingTestId,
        COUNT(*) AS TotalSubmissions,
        COUNT(DISTINCT UserId) AS UniqueUsers,
        COUNT(DISTINCT ProblemId) AS UniqueProblems,
        AVG(CAST(PassedTestCases AS FLOAT) / NULLIF(TotalTestCases, 0)) AS AverageSuccessRate,
        AVG(CAST(Score AS FLOAT) / NULLIF(MaxScore, 0)) AS AverageScoreRate,
        AVG(ExecutionTimeMs) AS AverageExecutionTimeMs,
        AVG(MemoryUsedKB) AS AverageMemoryUsedKB,
        SUM(LanguageSwitchCount) AS TotalLanguageSwitches,
        SUM(RunClickCount) AS TotalRunClicks,
        SUM(SubmitClickCount) AS TotalSubmitClicks,
        SUM(EraseCount) AS TotalEraseCount,
        SUM(SaveCount) AS TotalSaveCount,
        SUM(LoginLogoutCount) AS TotalLoginLogoutCount,
        SUM(CASE WHEN IsSessionAbandoned = 1 THEN 1 ELSE 0 END) AS AbandonedSessions,
        SUM(CASE WHEN IsLateSubmission = 1 THEN 1 ELSE 0 END) AS LateSubmissions
    FROM [dbo].[CodingTestSubmissions]
    WHERE CodingTestId = @CodingTestId
    GROUP BY CodingTestId
END
GO

-- =============================================
-- Script Completion
-- =============================================
PRINT ''
PRINT '============================================='
PRINT 'Coding Test Submissions Tables Created Successfully!'
PRINT '============================================='
PRINT ''
PRINT 'Tables Created:'
PRINT '1. CodingTestSubmissions - Main submission tracking table'
PRINT '2. CodingTestSubmissionResults - Detailed test case results'
PRINT ''
PRINT 'Views Created:'
PRINT '1. vw_CodingTestSubmissionSummary - Summary view for quick queries'
PRINT '2. vw_CodingTestSubmissionDetails - Detailed view with test case results'
PRINT ''
PRINT 'Stored Procedures Created:'
PRINT '1. sp_GetUserCodingTestSubmissions - Get user submission history'
PRINT '2. sp_GetCodingTestStatistics - Get test statistics'
PRINT ''
PRINT 'Indexes Created:'
PRINT '- Performance indexes on UserId, CodingTestId, ProblemId, SubmissionTime'
PRINT '- Composite indexes for common query patterns'
PRINT ''
PRINT 'Foreign Key Constraints:'
PRINT '- Links to CodingTests, CodingTestAttempts, CodingTestQuestionAttempts, Problems, TestCases'
PRINT ''
PRINT 'Ready to track coding test submissions!'
PRINT '============================================='
