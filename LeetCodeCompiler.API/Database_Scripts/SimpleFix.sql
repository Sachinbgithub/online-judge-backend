-- =============================================
-- Simple Fix Script - Works with existing Problems table
-- =============================================

USE [LeetCode]
GO

PRINT '============================================='
PRINT 'SIMPLE FIX FOR CODING TEST API'
PRINT '============================================='

-- 1. Check Problems table structure
PRINT ''
PRINT '1. Checking Problems table structure...'
PRINT '---------------------------------------'

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Problems'
ORDER BY ORDINAL_POSITION;

-- 2. Insert sample problems (minimal columns)
PRINT ''
PRINT '2. Inserting sample problems...'
PRINT '--------------------------------'

-- Check if Problems table is empty
IF (SELECT COUNT(*) FROM Problems) = 0
BEGIN
    -- Insert with only basic columns that should exist
    INSERT INTO Problems (Title, Description, Difficulty)
    VALUES 
    ('Two Sum', 'Find two numbers that add up to target', 1),
    ('Reverse String', 'Reverse a string', 1),
    ('Valid Parentheses', 'Check if parentheses are valid', 1),
    ('Maximum Subarray', 'Find the contiguous subarray with largest sum', 2),
    ('Climbing Stairs', 'Count ways to climb stairs', 1)
    
    PRINT 'Sample problems inserted successfully!'
    PRINT 'Problem IDs: 1, 2, 3, 4, 5 are now available'
END
ELSE
BEGIN
    PRINT 'Problems table already has data'
    SELECT COUNT(*) as ProblemCount FROM Problems
END

-- 3. Create CodingTests table if it doesn't exist
PRINT ''
PRINT '3. Creating CodingTests table...'
PRINT '---------------------------------'

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CodingTests')
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

-- 4. Create CodingTestQuestions table if it doesn't exist
PRINT ''
PRINT '4. Creating CodingTestQuestions table...'
PRINT '----------------------------------------'

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CodingTestQuestions')
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
    
    -- Add foreign key constraints
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

-- 5. Create CodingTestTopicData table if it doesn't exist
PRINT ''
PRINT '5. Creating CodingTestTopicData table...'
PRINT '---------------------------------------'

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CodingTestTopicData')
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

PRINT ''
PRINT '============================================='
PRINT 'SIMPLE FIX COMPLETE!'
PRINT '============================================='
PRINT 'Status:'
PRINT '- Problems table: Checked and populated'
PRINT '- CodingTests table: Created/Verified'
PRINT '- CodingTestQuestions table: Created/Verified'
PRINT '- CodingTestTopicData table: Created/Verified'
PRINT ''
PRINT 'Your API should now work!'
PRINT 'Test with problemId: 1, 2, 3, 4, or 5'
PRINT '============================================='
