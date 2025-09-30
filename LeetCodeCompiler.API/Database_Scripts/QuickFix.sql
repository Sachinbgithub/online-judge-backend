-- =============================================
-- Quick Fix Script for Coding Test API
-- =============================================
-- This script will quickly fix the most common issues

USE [LeetCode]
GO

PRINT '============================================='
PRINT 'QUICK FIX FOR CODING TEST API'
PRINT '============================================='

-- 1. Create Problems table if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Problems')
BEGIN
    PRINT 'Creating Problems table...'
    
    CREATE TABLE [dbo].[Problems](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Title] [nvarchar](200) NOT NULL,
        [Description] [nvarchar](max) NOT NULL,
        [Difficulty] [int] NULL,
        [Category] [nvarchar](100) NULL,
        [Tags] [nvarchar](500) NULL,
        [Constraints] [nvarchar](max) NULL,
        [Examples] [nvarchar](max) NULL,
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] [datetime2](7) NULL,
        CONSTRAINT [PK_Problems] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    
    PRINT 'Problems table created successfully'
END
ELSE
BEGIN
    PRINT 'Problems table already exists'
END

-- 2. Insert sample problems if table is empty
IF (SELECT COUNT(*) FROM Problems) = 0
BEGIN
    PRINT 'Inserting sample problems...'
    
    INSERT INTO Problems (Title, Description, Difficulty, Category, Tags, Constraints, Examples)
    VALUES 
    ('Two Sum', 'Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target.', 1, 'Array', 'array,hash-table', '2 <= nums.length <= 10^4', '[{"input": "[2,7,11,15], 9", "output": "[0,1]"}]'),
    ('Reverse String', 'Write a function that reverses a string. The input string is given as an array of characters s.', 1, 'String', 'string,two-pointers', '1 <= s.length <= 10^5', '[{"input": "hello", "output": "olleh"}]'),
    ('Valid Parentheses', 'Given a string s containing just the characters ''('', '')'', ''{'', ''}'', ''['' and '']'', determine if the input string is valid.', 1, 'Stack', 'stack,string', '1 <= s.length <= 10^4', '[{"input": "()", "output": "true"}]'),
    ('Maximum Subarray', 'Given an integer array nums, find the contiguous subarray (containing at least one number) which has the largest sum and return its sum.', 2, 'Array', 'array,divide-and-conquer', '1 <= nums.length <= 10^5', '[{"input": "[-2,1,-3,4,-1,2,1,-5,4]", "output": "6"}]'),
    ('Climbing Stairs', 'You are climbing a staircase. It takes n steps to reach the top. Each time you can either climb 1 or 2 steps. In how many distinct ways can you climb to the top?', 1, 'Dynamic Programming', 'dynamic-programming', '1 <= n <= 45', '[{"input": "2", "output": "2"}]')
    
    PRINT 'Sample problems inserted successfully!'
END
ELSE
BEGIN
    PRINT 'Problems table already has data'
END

-- 3. Create CodingTests table if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CodingTests')
BEGIN
    PRINT 'Creating CodingTests table...'
    
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
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CodingTestQuestions')
BEGIN
    PRINT 'Creating CodingTestQuestions table...'
    
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
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CodingTestTopicData')
BEGIN
    PRINT 'Creating CodingTestTopicData table...'
    
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
PRINT 'QUICK FIX COMPLETE!'
PRINT '============================================='
PRINT 'Tables created/verified:'
PRINT '- Problems (with sample data)'
PRINT '- CodingTests'
PRINT '- CodingTestQuestions'
PRINT '- CodingTestTopicData'
PRINT ''
PRINT 'You can now test the API!'
PRINT '============================================='
