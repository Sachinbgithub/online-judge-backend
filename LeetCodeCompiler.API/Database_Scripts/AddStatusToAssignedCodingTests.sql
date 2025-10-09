-- =============================================
-- Add Status Column to AssignedCodingTests Table
-- =============================================
-- This script adds a Status column to track the test state
-- Possible values: Assigned, InProgress, Completed, Expired
-- Run this script in your SQL Server database

USE [LeetCode]
GO

-- =============================================
-- Add Status Column
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssignedCodingTests]') AND name = 'Status')
BEGIN
    ALTER TABLE [dbo].[AssignedCodingTests]
    ADD [Status] NVARCHAR(50) NOT NULL DEFAULT 'Assigned'
    
    PRINT 'Status column added to AssignedCodingTests table'
END
ELSE
BEGIN
    PRINT 'Status column already exists in AssignedCodingTests table'
END
GO

-- =============================================
-- Add StartedAt Column (when user actually starts the test)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssignedCodingTests]') AND name = 'StartedAt')
BEGIN
    ALTER TABLE [dbo].[AssignedCodingTests]
    ADD [StartedAt] DATETIME2(7) NULL
    
    PRINT 'StartedAt column added to AssignedCodingTests table'
END
ELSE
BEGIN
    PRINT 'StartedAt column already exists in AssignedCodingTests table'
END
GO

-- =============================================
-- Add CompletedAt Column (when user completes/ends the test)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssignedCodingTests]') AND name = 'CompletedAt')
BEGIN
    ALTER TABLE [dbo].[AssignedCodingTests]
    ADD [CompletedAt] DATETIME2(7) NULL
    
    PRINT 'CompletedAt column added to AssignedCodingTests table'
END
ELSE
BEGIN
    PRINT 'CompletedAt column already exists in AssignedCodingTests table'
END
GO

-- =============================================
-- Add TimeSpentMinutes Column
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssignedCodingTests]') AND name = 'TimeSpentMinutes')
BEGIN
    ALTER TABLE [dbo].[AssignedCodingTests]
    ADD [TimeSpentMinutes] INT NOT NULL DEFAULT 0
    
    PRINT 'TimeSpentMinutes column added to AssignedCodingTests table'
END
ELSE
BEGIN
    PRINT 'TimeSpentMinutes column already exists in AssignedCodingTests table'
END
GO

-- =============================================
-- Add IsLateSubmission Column
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AssignedCodingTests]') AND name = 'IsLateSubmission')
BEGIN
    ALTER TABLE [dbo].[AssignedCodingTests]
    ADD [IsLateSubmission] BIT NOT NULL DEFAULT 0
    
    PRINT 'IsLateSubmission column added to AssignedCodingTests table'
END
ELSE
BEGIN
    PRINT 'IsLateSubmission column already exists in AssignedCodingTests table'
END
GO

-- =============================================
-- Update existing records to have default status
-- =============================================
UPDATE [dbo].[AssignedCodingTests]
SET [Status] = 'Assigned'
WHERE [Status] IS NULL OR [Status] = ''
GO

-- =============================================
-- Create index for Status queries
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_AssignedCodingTests_Status' AND object_id = OBJECT_ID(N'[dbo].[AssignedCodingTests]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AssignedCodingTests_Status] ON [dbo].[AssignedCodingTests]
    (
        [Status] ASC,
        [AssignedToUserId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    
    PRINT 'Index IX_AssignedCodingTests_Status created successfully'
END
ELSE
BEGIN
    PRINT 'Index IX_AssignedCodingTests_Status already exists'
END
GO

-- =============================================
-- Create stored procedure to get test status
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetUserTestStatus')
    DROP PROCEDURE [dbo].[sp_GetUserTestStatus]
GO

CREATE PROCEDURE [dbo].[sp_GetUserTestStatus]
    @UserId BIGINT,
    @CodingTestId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        act.AssignedId,
        act.CodingTestId,
        ct.TestName,
        act.AssignedToUserId,
        act.AssignedToUserType,
        act.AssignedDate,
        act.Status,
        act.StartedAt,
        act.CompletedAt,
        act.TimeSpentMinutes,
        act.IsLateSubmission,
        ct.StartDate,
        ct.EndDate,
        ct.DurationMinutes,
        ct.TotalQuestions,
        ct.TotalMarks,
        CASE 
            WHEN GETDATE() > ct.EndDate THEN 'Expired'
            WHEN act.Status = 'Completed' THEN 'Completed'
            WHEN act.Status = 'InProgress' THEN 'InProgress'
            ELSE 'Assigned'
        END AS CurrentStatus
    FROM [dbo].[AssignedCodingTests] act
    INNER JOIN [dbo].[CodingTests] ct ON act.CodingTestId = ct.Id
    WHERE act.AssignedToUserId = @UserId 
        AND act.CodingTestId = @CodingTestId
        AND act.IsDeleted = 0
END
GO

-- =============================================
-- Create stored procedure to update test status
-- =============================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_UpdateTestStatus')
    DROP PROCEDURE [dbo].[sp_UpdateTestStatus]
GO

CREATE PROCEDURE [dbo].[sp_UpdateTestStatus]
    @AssignedId BIGINT,
    @NewStatus NVARCHAR(50),
    @TimeSpentMinutes INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @OldStatus NVARCHAR(50)
    DECLARE @StartedAt DATETIME2
    DECLARE @TestEndDate DATETIME2
    
    -- Get current status and test end date
    SELECT 
        @OldStatus = act.Status,
        @StartedAt = act.StartedAt,
        @TestEndDate = ct.EndDate
    FROM [dbo].[AssignedCodingTests] act
    INNER JOIN [dbo].[CodingTests] ct ON act.CodingTestId = ct.Id
    WHERE act.AssignedId = @AssignedId
    
    IF @OldStatus IS NULL
    BEGIN
        RAISERROR('Assignment not found', 16, 1)
        RETURN
    END
    
    -- Update based on new status
    IF @NewStatus = 'InProgress'
    BEGIN
        UPDATE [dbo].[AssignedCodingTests]
        SET 
            [Status] = @NewStatus,
            [StartedAt] = CASE WHEN [StartedAt] IS NULL THEN GETDATE() ELSE [StartedAt] END,
            [UpdatedAt] = GETDATE()
        WHERE [AssignedId] = @AssignedId
    END
    ELSE IF @NewStatus = 'Completed'
    BEGIN
        UPDATE [dbo].[AssignedCodingTests]
        SET 
            [Status] = @NewStatus,
            [CompletedAt] = GETDATE(),
            [TimeSpentMinutes] = @TimeSpentMinutes,
            [IsLateSubmission] = CASE WHEN GETDATE() > @TestEndDate THEN 1 ELSE 0 END,
            [UpdatedAt] = GETDATE()
        WHERE [AssignedId] = @AssignedId
    END
    ELSE
    BEGIN
        UPDATE [dbo].[AssignedCodingTests]
        SET 
            [Status] = @NewStatus,
            [UpdatedAt] = GETDATE()
        WHERE [AssignedId] = @AssignedId
    END
    
    SELECT 
        AssignedId,
        CodingTestId,
        Status,
        StartedAt,
        CompletedAt,
        TimeSpentMinutes,
        IsLateSubmission
    FROM [dbo].[AssignedCodingTests]
    WHERE [AssignedId] = @AssignedId
END
GO

-- =============================================
-- Script Completion
-- =============================================
PRINT ''
PRINT '============================================='
PRINT 'Status Columns Added Successfully!'
PRINT '============================================='
PRINT ''
PRINT 'Columns Added:'
PRINT '1. Status (NVARCHAR(50)) - Test status: Assigned, InProgress, Completed, Expired'
PRINT '2. StartedAt (DATETIME2) - When user started the test'
PRINT '3. CompletedAt (DATETIME2) - When user completed the test'
PRINT '4. TimeSpentMinutes (INT) - Total time spent on test'
PRINT '5. IsLateSubmission (BIT) - Whether test was submitted late'
PRINT ''
PRINT 'Stored Procedures Created:'
PRINT '1. sp_GetUserTestStatus - Get test status for a user'
PRINT '2. sp_UpdateTestStatus - Update test status'
PRINT ''
PRINT 'Index Created:'
PRINT '- IX_AssignedCodingTests_Status for performance'
PRINT ''
PRINT 'Ready to manage test status!'
PRINT '============================================='
