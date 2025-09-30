-- =============================================
-- Ensure ID Columns are NOT NULL
-- =============================================
-- This script ensures all Id columns are NOT NULL (keeping INT data type)
-- It handles foreign key constraints and maintains data integrity
-- Run this script in your SQL Server database

USE [LeetCode]
GO

PRINT '============================================='
PRINT 'ENSURING ID COLUMNS ARE NOT NULL'
PRINT '============================================='
PRINT ''

-- =============================================
-- STEP 1: Drop Foreign Key Constraints
-- =============================================
PRINT '1. Dropping foreign key constraints...'
PRINT '--------------------------------------'

-- Drop FK from AssignedCodingTests to CodingTests
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AssignedCodingTests_CodingTests')
BEGIN
    ALTER TABLE [dbo].[AssignedCodingTests]
    DROP CONSTRAINT [FK_AssignedCodingTests_CodingTests]
    PRINT '✓ Dropped FK_AssignedCodingTests_CodingTests'
END

-- Drop FK from CodingTestQuestions to CodingTests
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CodingTestQuestions_CodingTests')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestions]
    DROP CONSTRAINT [FK_CodingTestQuestions_CodingTests]
    PRINT '✓ Dropped FK_CodingTestQuestions_CodingTests'
END

-- Drop FK from CodingTestQuestions to Problems
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CodingTestQuestions_Problems')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestions]
    DROP CONSTRAINT [FK_CodingTestQuestions_Problems]
    PRINT '✓ Dropped FK_CodingTestQuestions_Problems'
END

-- Drop FK from TestCases to Problems (if exists)
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TestCases_Problems')
BEGIN
    ALTER TABLE [dbo].[TestCases]
    DROP CONSTRAINT [FK_TestCases_Problems]
    PRINT '✓ Dropped FK_TestCases_Problems'
END

-- Drop FK from StarterCodes to Problems (if exists)
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_StarterCodes_Problems')
BEGIN
    ALTER TABLE [dbo].[StarterCodes]
    DROP CONSTRAINT [FK_StarterCodes_Problems]
    PRINT '✓ Dropped FK_StarterCodes_Problems'
END

-- Drop FK from CodingTestAttempts to CodingTests
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CodingTestAttempts_CodingTests')
BEGIN
    ALTER TABLE [dbo].[CodingTestAttempts]
    DROP CONSTRAINT [FK_CodingTestAttempts_CodingTests]
    PRINT '✓ Dropped FK_CodingTestAttempts_CodingTests'
END

-- Drop FK from CodingTestQuestionAttempts to CodingTestAttempts
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CodingTestQuestionAttempts_CodingTestAttempts')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestionAttempts]
    DROP CONSTRAINT [FK_CodingTestQuestionAttempts_CodingTestAttempts]
    PRINT '✓ Dropped FK_CodingTestQuestionAttempts_CodingTestAttempts'
END

-- Drop FK from CodingTestQuestionAttempts to CodingTestQuestions
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CodingTestQuestionAttempts_CodingTestQuestions')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestionAttempts]
    DROP CONSTRAINT [FK_CodingTestQuestionAttempts_CodingTestQuestions]
    PRINT '✓ Dropped FK_CodingTestQuestionAttempts_CodingTestQuestions'
END

-- Drop FK from CodingTestQuestionAttempts to Problems
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CodingTestQuestionAttempts_Problems')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestionAttempts]
    DROP CONSTRAINT [FK_CodingTestQuestionAttempts_Problems]
    PRINT '✓ Dropped FK_CodingTestQuestionAttempts_Problems'
END

-- Drop FK from CodingTestTopicData to CodingTests
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CodingTestTopicData_CodingTests')
BEGIN
    ALTER TABLE [dbo].[CodingTestTopicData]
    DROP CONSTRAINT [FK_CodingTestTopicData_CodingTests]
    PRINT '✓ Dropped FK_CodingTestTopicData_CodingTests'
END

PRINT ''

-- =============================================
-- STEP 2: Drop Indexes on ID Columns
-- =============================================
PRINT '2. Dropping indexes on ID columns...'
PRINT '------------------------------------'

-- Drop indexes from CodingTestQuestions
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestQuestions_CodingTestId')
BEGIN
    DROP INDEX [IX_CodingTestQuestions_CodingTestId] ON [dbo].[CodingTestQuestions]
    PRINT '✓ Dropped IX_CodingTestQuestions_CodingTestId'
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestQuestions_ProblemId')
BEGIN
    DROP INDEX [IX_CodingTestQuestions_ProblemId] ON [dbo].[CodingTestQuestions]
    PRINT '✓ Dropped IX_CodingTestQuestions_ProblemId'
END

-- Drop indexes from CodingTestAttempts
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestAttempts_CodingTestId_UserId')
BEGIN
    DROP INDEX [IX_CodingTestAttempts_CodingTestId_UserId] ON [dbo].[CodingTestAttempts]
    PRINT '✓ Dropped IX_CodingTestAttempts_CodingTestId_UserId'
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestAttempts_Status')
BEGIN
    DROP INDEX [IX_CodingTestAttempts_Status] ON [dbo].[CodingTestAttempts]
    PRINT '✓ Dropped IX_CodingTestAttempts_Status'
END

-- Drop indexes from CodingTestQuestionAttempts
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestQuestionAttempts_CodingTestAttemptId')
BEGIN
    DROP INDEX [IX_CodingTestQuestionAttempts_CodingTestAttemptId] ON [dbo].[CodingTestQuestionAttempts]
    PRINT '✓ Dropped IX_CodingTestQuestionAttempts_CodingTestAttemptId'
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestQuestionAttempts_UserId')
BEGIN
    DROP INDEX [IX_CodingTestQuestionAttempts_UserId] ON [dbo].[CodingTestQuestionAttempts]
    PRINT '✓ Dropped IX_CodingTestQuestionAttempts_UserId'
END

-- Drop indexes from CodingTestTopicData
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestTopicData_CodingTestId')
BEGIN
    DROP INDEX [IX_CodingTestTopicData_CodingTestId] ON [dbo].[CodingTestTopicData]
    PRINT '✓ Dropped IX_CodingTestTopicData_CodingTestId'
END

-- Drop indexes from AssignedCodingTests
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssignedCodingTests_User')
BEGIN
    DROP INDEX [IX_AssignedCodingTests_User] ON [dbo].[AssignedCodingTests]
    PRINT '✓ Dropped IX_AssignedCodingTests_User'
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssignedCodingTests_Test')
BEGIN
    DROP INDEX [IX_AssignedCodingTests_Test] ON [dbo].[AssignedCodingTests]
    PRINT '✓ Dropped IX_AssignedCodingTests_Test'
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssignedCodingTests_Assignment')
BEGIN
    DROP INDEX [IX_AssignedCodingTests_Assignment] ON [dbo].[AssignedCodingTests]
    PRINT '✓ Dropped IX_AssignedCodingTests_Assignment'
END

-- Drop unique constraint from AssignedCodingTests
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_AssignedCodingTests_UserTest')
BEGIN
    DROP INDEX [UQ_AssignedCodingTests_UserTest] ON [dbo].[AssignedCodingTests]
    PRINT '✓ Dropped UQ_AssignedCodingTests_UserTest'
END

PRINT ''

-- =============================================
-- STEP 3: Drop Primary Key Constraints
-- =============================================
PRINT '3. Dropping primary key constraints...'
PRINT '--------------------------------------'

-- Drop PK from CodingTests
IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_CodingTests')
BEGIN
    ALTER TABLE [dbo].[CodingTests]
    DROP CONSTRAINT [PK_CodingTests]
    PRINT '✓ Dropped PK_CodingTests'
END

-- Drop PK from CodingTestQuestions
IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_CodingTestQuestions')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestions]
    DROP CONSTRAINT [PK_CodingTestQuestions]
    PRINT '✓ Dropped PK_CodingTestQuestions'
END

-- Drop PK from CodingTestAttempts
IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_CodingTestAttempts')
BEGIN
    ALTER TABLE [dbo].[CodingTestAttempts]
    DROP CONSTRAINT [PK_CodingTestAttempts]
    PRINT '✓ Dropped PK_CodingTestAttempts'
END

-- Drop PK from CodingTestQuestionAttempts
IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_CodingTestQuestionAttempts')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestionAttempts]
    DROP CONSTRAINT [PK_CodingTestQuestionAttempts]
    PRINT '✓ Dropped PK_CodingTestQuestionAttempts'
END

-- Drop PK from CodingTestTopicData
IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_CodingTestTopicData')
BEGIN
    ALTER TABLE [dbo].[CodingTestTopicData]
    DROP CONSTRAINT [PK_CodingTestTopicData]
    PRINT '✓ Dropped PK_CodingTestTopicData'
END

-- Drop PK from Problems
IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_Problems')
BEGIN
    ALTER TABLE [dbo].[Problems]
    DROP CONSTRAINT [PK_Problems]
    PRINT '✓ Dropped PK_Problems'
END

-- Drop PK from TestCases (if exists)
IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_TestCases')
BEGIN
    ALTER TABLE [dbo].[TestCases]
    DROP CONSTRAINT [PK_TestCases]
    PRINT '✓ Dropped PK_TestCases'
END

-- Drop PK from StarterCodes (if exists)
IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_StarterCodes')
BEGIN
    ALTER TABLE [dbo].[StarterCodes]
    DROP CONSTRAINT [PK_StarterCodes]
    PRINT '✓ Dropped PK_StarterCodes'
END

PRINT ''

-- =============================================
-- STEP 4: Ensure ID Columns are NOT NULL
-- =============================================
PRINT '4. Ensuring ID columns are NOT NULL...'
PRINT '--------------------------------------'

-- Ensure CodingTests.Id is NOT NULL
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CodingTests')
BEGIN
    ALTER TABLE [dbo].[CodingTests]
    ALTER COLUMN [Id] [int] NOT NULL
    PRINT '✓ CodingTests.Id ensured NOT NULL'
END

-- Ensure CodingTestQuestions ID columns are NOT NULL
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CodingTestQuestions')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestions]
    ALTER COLUMN [Id] [int] NOT NULL
    PRINT '✓ CodingTestQuestions.Id ensured NOT NULL'
    
    ALTER TABLE [dbo].[CodingTestQuestions]
    ALTER COLUMN [CodingTestId] [int] NOT NULL
    PRINT '✓ CodingTestQuestions.CodingTestId ensured NOT NULL'
    
    ALTER TABLE [dbo].[CodingTestQuestions]
    ALTER COLUMN [ProblemId] [int] NOT NULL
    PRINT '✓ CodingTestQuestions.ProblemId ensured NOT NULL'
END

-- Ensure CodingTestAttempts ID columns are NOT NULL
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CodingTestAttempts')
BEGIN
    ALTER TABLE [dbo].[CodingTestAttempts]
    ALTER COLUMN [Id] [int] NOT NULL
    PRINT '✓ CodingTestAttempts.Id ensured NOT NULL'
    
    ALTER TABLE [dbo].[CodingTestAttempts]
    ALTER COLUMN [CodingTestId] [int] NOT NULL
    PRINT '✓ CodingTestAttempts.CodingTestId ensured NOT NULL'
    
    ALTER TABLE [dbo].[CodingTestAttempts]
    ALTER COLUMN [UserId] [int] NOT NULL
    PRINT '✓ CodingTestAttempts.UserId ensured NOT NULL'
END

-- Ensure CodingTestQuestionAttempts ID columns are NOT NULL
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CodingTestQuestionAttempts')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestionAttempts]
    ALTER COLUMN [Id] [int] NOT NULL
    PRINT '✓ CodingTestQuestionAttempts.Id ensured NOT NULL'
    
    ALTER TABLE [dbo].[CodingTestQuestionAttempts]
    ALTER COLUMN [CodingTestAttemptId] [int] NOT NULL
    PRINT '✓ CodingTestQuestionAttempts.CodingTestAttemptId ensured NOT NULL'
    
    ALTER TABLE [dbo].[CodingTestQuestionAttempts]
    ALTER COLUMN [CodingTestQuestionId] [int] NOT NULL
    PRINT '✓ CodingTestQuestionAttempts.CodingTestQuestionId ensured NOT NULL'
    
    ALTER TABLE [dbo].[CodingTestQuestionAttempts]
    ALTER COLUMN [ProblemId] [int] NOT NULL
    PRINT '✓ CodingTestQuestionAttempts.ProblemId ensured NOT NULL'
    
    ALTER TABLE [dbo].[CodingTestQuestionAttempts]
    ALTER COLUMN [UserId] [int] NOT NULL
    PRINT '✓ CodingTestQuestionAttempts.UserId ensured NOT NULL'
END

-- Ensure CodingTestTopicData ID columns are NOT NULL
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CodingTestTopicData')
BEGIN
    ALTER TABLE [dbo].[CodingTestTopicData]
    ALTER COLUMN [Id] [int] NOT NULL
    PRINT '✓ CodingTestTopicData.Id ensured NOT NULL'
    
    ALTER TABLE [dbo].[CodingTestTopicData]
    ALTER COLUMN [CodingTestId] [int] NOT NULL
    PRINT '✓ CodingTestTopicData.CodingTestId ensured NOT NULL'
END

-- Ensure AssignedCodingTests.CodingTestId is NOT NULL
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AssignedCodingTests')
BEGIN
    ALTER TABLE [dbo].[AssignedCodingTests]
    ALTER COLUMN [CodingTestId] [int] NOT NULL
    PRINT '✓ AssignedCodingTests.CodingTestId ensured NOT NULL'
END

-- Ensure Problems.Id is NOT NULL
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Problems')
BEGIN
    ALTER TABLE [dbo].[Problems]
    ALTER COLUMN [Id] [int] NOT NULL
    PRINT '✓ Problems.Id ensured NOT NULL'
END

-- Ensure TestCases ID columns are NOT NULL (if table exists)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TestCases')
BEGIN
    ALTER TABLE [dbo].[TestCases]
    ALTER COLUMN [Id] [int] NOT NULL
    PRINT '✓ TestCases.Id ensured NOT NULL'
    
    ALTER TABLE [dbo].[TestCases]
    ALTER COLUMN [ProblemId] [int] NOT NULL
    PRINT '✓ TestCases.ProblemId ensured NOT NULL'
END

-- Ensure StarterCodes ID columns are NOT NULL (if table exists)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'StarterCodes')
BEGIN
    ALTER TABLE [dbo].[StarterCodes]
    ALTER COLUMN [Id] [int] NOT NULL
    PRINT '✓ StarterCodes.Id ensured NOT NULL'
    
    ALTER TABLE [dbo].[StarterCodes]
    ALTER COLUMN [ProblemId] [int] NOT NULL
    PRINT '✓ StarterCodes.ProblemId ensured NOT NULL'
END

PRINT ''

-- =============================================
-- STEP 5: Recreate Primary Key Constraints
-- =============================================
PRINT '5. Recreating primary key constraints...'
PRINT '---------------------------------------'

-- Recreate PK on CodingTests
IF NOT EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_CodingTests')
BEGIN
    ALTER TABLE [dbo].[CodingTests]
    ADD CONSTRAINT [PK_CodingTests] PRIMARY KEY CLUSTERED ([Id] ASC)
    PRINT '✓ Recreated PK_CodingTests'
END

-- Recreate PK on CodingTestQuestions
IF NOT EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_CodingTestQuestions')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestions]
    ADD CONSTRAINT [PK_CodingTestQuestions] PRIMARY KEY CLUSTERED ([Id] ASC)
    PRINT '✓ Recreated PK_CodingTestQuestions'
END

-- Recreate PK on CodingTestAttempts
IF NOT EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_CodingTestAttempts')
BEGIN
    ALTER TABLE [dbo].[CodingTestAttempts]
    ADD CONSTRAINT [PK_CodingTestAttempts] PRIMARY KEY CLUSTERED ([Id] ASC)
    PRINT '✓ Recreated PK_CodingTestAttempts'
END

-- Recreate PK on CodingTestQuestionAttempts
IF NOT EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_CodingTestQuestionAttempts')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestionAttempts]
    ADD CONSTRAINT [PK_CodingTestQuestionAttempts] PRIMARY KEY CLUSTERED ([Id] ASC)
    PRINT '✓ Recreated PK_CodingTestQuestionAttempts'
END

-- Recreate PK on CodingTestTopicData
IF NOT EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_CodingTestTopicData')
BEGIN
    ALTER TABLE [dbo].[CodingTestTopicData]
    ADD CONSTRAINT [PK_CodingTestTopicData] PRIMARY KEY CLUSTERED ([Id] ASC)
    PRINT '✓ Recreated PK_CodingTestTopicData'
END

-- Recreate PK on Problems
IF NOT EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_Problems')
BEGIN
    ALTER TABLE [dbo].[Problems]
    ADD CONSTRAINT [PK_Problems] PRIMARY KEY CLUSTERED ([Id] ASC)
    PRINT '✓ Recreated PK_Problems'
END

-- Recreate PK on TestCases (if exists)
IF NOT EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_TestCases')
BEGIN
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TestCases')
    BEGIN
        ALTER TABLE [dbo].[TestCases]
        ADD CONSTRAINT [PK_TestCases] PRIMARY KEY CLUSTERED ([Id] ASC)
        PRINT '✓ Recreated PK_TestCases'
    END
END

-- Recreate PK on StarterCodes (if exists)
IF NOT EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_StarterCodes')
BEGIN
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'StarterCodes')
    BEGIN
        ALTER TABLE [dbo].[StarterCodes]
        ADD CONSTRAINT [PK_StarterCodes] PRIMARY KEY CLUSTERED ([Id] ASC)
        PRINT '✓ Recreated PK_StarterCodes'
    END
END

PRINT ''

-- =============================================
-- STEP 6: Recreate Foreign Key Constraints
-- =============================================
PRINT '6. Recreating foreign key constraints...'
PRINT '---------------------------------------'

-- Recreate FK from AssignedCodingTests to CodingTests
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AssignedCodingTests_CodingTests')
BEGIN
    ALTER TABLE [dbo].[AssignedCodingTests]
    ADD CONSTRAINT [FK_AssignedCodingTests_CodingTests]
    FOREIGN KEY([CodingTestId]) REFERENCES [dbo].[CodingTests] ([Id]) ON DELETE CASCADE
    PRINT '✓ Recreated FK_AssignedCodingTests_CodingTests'
END

-- Recreate FK from CodingTestQuestions to CodingTests
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CodingTestQuestions_CodingTests')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestions]
    ADD CONSTRAINT [FK_CodingTestQuestions_CodingTests]
    FOREIGN KEY([CodingTestId]) REFERENCES [dbo].[CodingTests] ([Id]) ON DELETE CASCADE
    PRINT '✓ Recreated FK_CodingTestQuestions_CodingTests'
END

-- Recreate FK from CodingTestQuestions to Problems (if Problems table exists)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Problems')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CodingTestQuestions_Problems')
    BEGIN
        ALTER TABLE [dbo].[CodingTestQuestions]
        ADD CONSTRAINT [FK_CodingTestQuestions_Problems]
        FOREIGN KEY([ProblemId]) REFERENCES [dbo].[Problems] ([Id])
        PRINT '✓ Recreated FK_CodingTestQuestions_Problems'
    END
END

-- Recreate FK from CodingTestAttempts to CodingTests
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CodingTestAttempts_CodingTests')
BEGIN
    ALTER TABLE [dbo].[CodingTestAttempts]
    ADD CONSTRAINT [FK_CodingTestAttempts_CodingTests]
    FOREIGN KEY([CodingTestId]) REFERENCES [dbo].[CodingTests] ([Id]) ON DELETE CASCADE
    PRINT '✓ Recreated FK_CodingTestAttempts_CodingTests'
END

-- Recreate FK from CodingTestQuestionAttempts to CodingTestAttempts
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CodingTestQuestionAttempts_CodingTestAttempts')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestionAttempts]
    ADD CONSTRAINT [FK_CodingTestQuestionAttempts_CodingTestAttempts]
    FOREIGN KEY([CodingTestAttemptId]) REFERENCES [dbo].[CodingTestAttempts] ([Id]) ON DELETE CASCADE
    PRINT '✓ Recreated FK_CodingTestQuestionAttempts_CodingTestAttempts'
END

-- Recreate FK from CodingTestQuestionAttempts to CodingTestQuestions
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CodingTestQuestionAttempts_CodingTestQuestions')
BEGIN
    ALTER TABLE [dbo].[CodingTestQuestionAttempts]
    ADD CONSTRAINT [FK_CodingTestQuestionAttempts_CodingTestQuestions]
    FOREIGN KEY([CodingTestQuestionId]) REFERENCES [dbo].[CodingTestQuestions] ([Id])
    PRINT '✓ Recreated FK_CodingTestQuestionAttempts_CodingTestQuestions'
END

-- Recreate FK from CodingTestQuestionAttempts to Problems (if Problems table exists)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Problems')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CodingTestQuestionAttempts_Problems')
    BEGIN
        ALTER TABLE [dbo].[CodingTestQuestionAttempts]
        ADD CONSTRAINT [FK_CodingTestQuestionAttempts_Problems]
        FOREIGN KEY([ProblemId]) REFERENCES [dbo].[Problems] ([Id])
        PRINT '✓ Recreated FK_CodingTestQuestionAttempts_Problems'
    END
END

-- Recreate FK from CodingTestTopicData to CodingTests
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CodingTestTopicData_CodingTests')
BEGIN
    ALTER TABLE [dbo].[CodingTestTopicData]
    ADD CONSTRAINT [FK_CodingTestTopicData_CodingTests]
    FOREIGN KEY([CodingTestId]) REFERENCES [dbo].[CodingTests] ([Id]) ON DELETE CASCADE
    PRINT '✓ Recreated FK_CodingTestTopicData_CodingTests'
END

-- Recreate FK from TestCases to Problems (if exists)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TestCases_Problems')
BEGIN
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TestCases')
    BEGIN
        ALTER TABLE [dbo].[TestCases]
        ADD CONSTRAINT [FK_TestCases_Problems]
        FOREIGN KEY([ProblemId]) REFERENCES [dbo].[Problems] ([Id])
        PRINT '✓ Recreated FK_TestCases_Problems'
    END
END

-- Recreate FK from StarterCodes to Problems (if exists)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_StarterCodes_Problems')
BEGIN
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'StarterCodes')
    BEGIN
        ALTER TABLE [dbo].[StarterCodes]
        ADD CONSTRAINT [FK_StarterCodes_Problems]
        FOREIGN KEY([ProblemId]) REFERENCES [dbo].[Problems] ([Id])
        PRINT '✓ Recreated FK_StarterCodes_Problems'
    END
END

PRINT ''

-- =============================================
-- STEP 7: Recreate Indexes
-- =============================================
PRINT '7. Recreating indexes...'
PRINT '------------------------'

-- Recreate indexes on CodingTestQuestions
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestQuestions_CodingTestId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestQuestions_CodingTestId] 
    ON [dbo].[CodingTestQuestions] ([CodingTestId] ASC)
    PRINT '✓ Recreated IX_CodingTestQuestions_CodingTestId'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestQuestions_ProblemId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestQuestions_ProblemId] 
    ON [dbo].[CodingTestQuestions] ([ProblemId] ASC)
    PRINT '✓ Recreated IX_CodingTestQuestions_ProblemId'
END

-- Recreate indexes on CodingTestAttempts
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestAttempts_CodingTestId_UserId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestAttempts_CodingTestId_UserId] 
    ON [dbo].[CodingTestAttempts] ([CodingTestId] ASC, [UserId] ASC)
    PRINT '✓ Recreated IX_CodingTestAttempts_CodingTestId_UserId'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestAttempts_Status')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestAttempts_Status] 
    ON [dbo].[CodingTestAttempts] ([Status] ASC)
    PRINT '✓ Recreated IX_CodingTestAttempts_Status'
END

-- Recreate indexes on CodingTestQuestionAttempts
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestQuestionAttempts_CodingTestAttemptId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestQuestionAttempts_CodingTestAttemptId] 
    ON [dbo].[CodingTestQuestionAttempts] ([CodingTestAttemptId] ASC)
    PRINT '✓ Recreated IX_CodingTestQuestionAttempts_CodingTestAttemptId'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestQuestionAttempts_UserId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestQuestionAttempts_UserId] 
    ON [dbo].[CodingTestQuestionAttempts] ([UserId] ASC)
    PRINT '✓ Recreated IX_CodingTestQuestionAttempts_UserId'
END

-- Recreate indexes on CodingTestTopicData
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CodingTestTopicData_CodingTestId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CodingTestTopicData_CodingTestId] 
    ON [dbo].[CodingTestTopicData] ([CodingTestId] ASC)
    PRINT '✓ Recreated IX_CodingTestTopicData_CodingTestId'
END

-- Recreate indexes on AssignedCodingTests
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssignedCodingTests_User')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AssignedCodingTests_User] 
    ON [dbo].[AssignedCodingTests] ([AssignedToUserId], [AssignedToUserType], [IsDeleted])
    PRINT '✓ Recreated IX_AssignedCodingTests_User'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssignedCodingTests_Test')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AssignedCodingTests_Test] 
    ON [dbo].[AssignedCodingTests] ([CodingTestId], [IsDeleted])
    PRINT '✓ Recreated IX_AssignedCodingTests_Test'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssignedCodingTests_Assignment')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AssignedCodingTests_Assignment] 
    ON [dbo].[AssignedCodingTests] ([AssignedByUserId], [AssignedDate])
    PRINT '✓ Recreated IX_AssignedCodingTests_Assignment'
END

-- Recreate unique constraint on AssignedCodingTests
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_AssignedCodingTests_UserTest')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_AssignedCodingTests_UserTest] 
    ON [dbo].[AssignedCodingTests] ([CodingTestId], [AssignedToUserId], [AssignedToUserType])
    WHERE [IsDeleted] = 0
    PRINT '✓ Recreated UQ_AssignedCodingTests_UserTest'
END

PRINT ''

-- =============================================
-- STEP 8: Verify Changes
-- =============================================
PRINT '8. Verifying data type changes...'
PRINT '---------------------------------'

SELECT 
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE t.name IN ('CodingTests', 'CodingTestQuestions', 'CodingTestAttempts', 
                 'CodingTestQuestionAttempts', 'CodingTestTopicData', 'AssignedCodingTests')
    AND (c.name LIKE '%Id' OR c.name = 'Id')
ORDER BY t.name, c.name

PRINT ''
PRINT '============================================='
PRINT 'ID COLUMN ALTERATION COMPLETE!'
PRINT '============================================='
PRINT ''
PRINT 'Summary of Changes:'
PRINT '• Step 1: All foreign key constraints dropped'
PRINT '• Step 2: All indexes on ID columns dropped'
PRINT '• Step 3: All primary key constraints dropped'
PRINT '• Step 4: All Id columns ensured NOT NULL (kept INT data type)'
PRINT '• Step 5: All primary key constraints recreated'
PRINT '• Step 6: All foreign key constraints recreated'
PRINT '• Step 7: All indexes recreated'
PRINT '• Step 8: Changes verified'
PRINT ''
PRINT 'Data Integrity:'
PRINT '• All constraints restored'
PRINT '• All indexes restored'
PRINT '• Existing data preserved'
PRINT '• Relationships maintained'
PRINT '• Data type kept as INT (not changed to BIGINT)'
PRINT ''
PRINT 'Tables Updated (NOT NULL enforced):'
PRINT '• CodingTests (Id)'
PRINT '• CodingTestQuestions (Id, CodingTestId, ProblemId)'
PRINT '• CodingTestAttempts (Id, CodingTestId, UserId)'
PRINT '• CodingTestQuestionAttempts (Id, CodingTestAttemptId, CodingTestQuestionId, ProblemId, UserId)'
PRINT '• CodingTestTopicData (Id, CodingTestId)'
PRINT '• AssignedCodingTests (CodingTestId)'
PRINT '• Problems (Id)'
PRINT '• TestCases (Id, ProblemId)'
PRINT '• StarterCodes (Id, ProblemId)'
PRINT '============================================='
GO
