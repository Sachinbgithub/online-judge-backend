-- =============================================
-- Diagnostic and Fix Script for Coding Test API
-- =============================================
-- This script will diagnose the database issues and provide fixes

USE [LeetCode]
GO

PRINT '============================================='
PRINT 'DIAGNOSING CODING TEST API DATABASE ISSUES'
PRINT '============================================='

-- 1. Check if Coding Test tables exist
PRINT ''
PRINT '1. Checking if Coding Test tables exist...'
PRINT '-------------------------------------------'

SELECT 
    TABLE_NAME,
    TABLE_TYPE,
    CASE 
        WHEN TABLE_NAME IN ('CodingTests', 'CodingTestQuestions', 'CodingTestAttempts', 'CodingTestQuestionAttempts', 'CodingTestTopicData')
        THEN 'EXISTS'
        ELSE 'MISSING'
    END as Status
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME LIKE 'CodingTest%'
ORDER BY TABLE_NAME;

-- 2. Check Problems table
PRINT ''
PRINT '2. Checking Problems table...'
PRINT '-----------------------------'

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Problems')
BEGIN
    DECLARE @ProblemCount INT
    SELECT @ProblemCount = COUNT(*) FROM Problems
    PRINT 'Problems table exists with ' + CAST(@ProblemCount AS VARCHAR(10)) + ' records'
    
    IF @ProblemCount = 0
    BEGIN
        PRINT 'WARNING: Problems table is empty!'
        PRINT 'This will cause foreign key constraint errors.'
    END
    ELSE
    BEGIN
        PRINT 'Sample problems:'
        SELECT TOP 3 Id, Title FROM Problems
    END
END
ELSE
BEGIN
    PRINT 'ERROR: Problems table does not exist!'
    PRINT 'This is required for CodingTestQuestions foreign key.'
END

-- 3. Check for any existing CodingTests data
PRINT ''
PRINT '3. Checking existing CodingTests data...'
PRINT '----------------------------------------'

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CodingTests')
BEGIN
    DECLARE @CodingTestCount INT
    SELECT @CodingTestCount = COUNT(*) FROM CodingTests
    PRINT 'CodingTests table exists with ' + CAST(@CodingTestCount AS VARCHAR(10)) + ' records'
END
ELSE
BEGIN
    PRINT 'CodingTests table does not exist - this is the main issue!'
END

-- 4. Check table structure if CodingTests exists
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CodingTests')
BEGIN
    PRINT ''
    PRINT '4. Checking CodingTests table structure...'
    PRINT '------------------------------------------'
    
    SELECT 
        COLUMN_NAME,
        DATA_TYPE,
        IS_NULLABLE,
        COLUMN_DEFAULT
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'CodingTests'
    ORDER BY ORDINAL_POSITION;
END

PRINT ''
PRINT '============================================='
PRINT 'DIAGNOSIS COMPLETE'
PRINT '============================================='
PRINT ''
PRINT 'SOLUTIONS:'
PRINT '1. If CodingTests table does not exist:'
PRINT '   - Run CreateCodingTestTables.sql script'
PRINT ''
PRINT '2. If Problems table is empty:'
PRINT '   - Insert some sample problems first'
PRINT ''
PRINT '3. If foreign key constraints fail:'
PRINT '   - Ensure problemId exists in Problems table'
PRINT ''
PRINT '============================================='

-- 5. Quick fix: Insert sample problems if table is empty
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Problems')
BEGIN
    IF (SELECT COUNT(*) FROM Problems) = 0
    BEGIN
        PRINT ''
        PRINT '5. INSERTING SAMPLE PROBLEMS...'
        PRINT '-------------------------------'
        
        -- Check what columns exist in Problems table first
        DECLARE @HasCategory BIT = 0, @HasTags BIT = 0, @HasConstraints BIT = 0, @HasExamples BIT = 0
        
        IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Problems' AND COLUMN_NAME = 'Category')
            SET @HasCategory = 1
        IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Problems' AND COLUMN_NAME = 'Tags')
            SET @HasTags = 1
        IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Problems' AND COLUMN_NAME = 'Constraints')
            SET @HasConstraints = 1
        IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Problems' AND COLUMN_NAME = 'Examples')
            SET @HasExamples = 1
        
        -- Insert based on available columns
        IF @HasCategory = 1 AND @HasTags = 1 AND @HasConstraints = 1 AND @HasExamples = 1
        BEGIN
            INSERT INTO Problems (Title, Description, Difficulty, Category, Tags, Constraints, Examples)
            VALUES 
            ('Two Sum', 'Find two numbers that add up to target', 1, 'Array', 'array,hash', '2 <= nums.length <= 10^4', '[{"input": "[2,7,11,15], 9", "output": "[0,1]"}]'),
            ('Reverse String', 'Reverse a string', 1, 'String', 'string,two-pointers', '1 <= s.length <= 10^5', '[{"input": "hello", "output": "olleh"}]'),
            ('Valid Parentheses', 'Check if parentheses are valid', 1, 'Stack', 'stack,string', '1 <= s.length <= 10^4', '[{"input": "()", "output": "true"}]')
        END
        ELSE IF @HasCategory = 1 AND @HasTags = 1
        BEGIN
            INSERT INTO Problems (Title, Description, Difficulty, Category, Tags)
            VALUES 
            ('Two Sum', 'Find two numbers that add up to target', 1, 'Array', 'array,hash'),
            ('Reverse String', 'Reverse a string', 1, 'String', 'string,two-pointers'),
            ('Valid Parentheses', 'Check if parentheses are valid', 1, 'Stack', 'stack,string')
        END
        ELSE
        BEGIN
            INSERT INTO Problems (Title, Description, Difficulty)
            VALUES 
            ('Two Sum', 'Find two numbers that add up to target', 1),
            ('Reverse String', 'Reverse a string', 1),
            ('Valid Parentheses', 'Check if parentheses are valid', 1)
        END
        
        PRINT 'Sample problems inserted successfully!'
        PRINT 'Problem IDs: 1, 2, 3 are now available'
    END
END
