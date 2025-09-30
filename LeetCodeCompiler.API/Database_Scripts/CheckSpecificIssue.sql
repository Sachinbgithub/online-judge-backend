-- =============================================
-- Check Specific Issue with Coding Test API
-- =============================================

USE [LeetCode]
GO

PRINT '============================================='
PRINT 'CHECKING SPECIFIC CODING TEST API ISSUES'
PRINT '============================================='

-- 1. Check if problem with ID 1 exists
PRINT ''
PRINT '1. Checking if Problem ID 1 exists...'
PRINT '------------------------------------'

IF EXISTS (SELECT * FROM Problems WHERE Id = 1)
BEGIN
    SELECT Id, Title, Description FROM Problems WHERE Id = 1
    PRINT '✓ Problem ID 1 exists'
END
ELSE
BEGIN
    PRINT '✗ Problem ID 1 does NOT exist!'
    PRINT 'This will cause foreign key constraint error.'
    
    -- Show available problems
    PRINT ''
    PRINT 'Available Problems:'
    SELECT TOP 5 Id, Title FROM Problems ORDER BY Id
END

-- 2. Check CodingTests table structure
PRINT ''
PRINT '2. Checking CodingTests table structure...'
PRINT '------------------------------------------'

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'CodingTests'
ORDER BY ORDINAL_POSITION;

-- 3. Check if there are any existing CodingTests
PRINT ''
PRINT '3. Checking existing CodingTests...'
PRINT '-----------------------------------'

SELECT COUNT(*) as CodingTestCount FROM CodingTests;

-- 4. Check CodingTestQuestions table
PRINT ''
PRINT '4. Checking CodingTestQuestions table...'
PRINT '----------------------------------------'

SELECT COUNT(*) as CodingTestQuestionCount FROM CodingTestQuestions;

-- 5. Check CodingTestTopicData table
PRINT ''
PRINT '5. Checking CodingTestTopicData table...'
PRINT '---------------------------------------'

SELECT COUNT(*) as CodingTestTopicDataCount FROM CodingTestTopicData;

-- 6. Test insert a simple problem if ID 1 doesn't exist
IF NOT EXISTS (SELECT * FROM Problems WHERE Id = 1)
BEGIN
    PRINT ''
    PRINT '6. INSERTING PROBLEM ID 1...'
    PRINT '----------------------------'
    
    -- Check what columns exist in Problems table
    DECLARE @HasExamples BIT = 0, @HasConstraints BIT = 0, @HasHints BIT = 0
    
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Problems' AND COLUMN_NAME = 'Examples')
        SET @HasExamples = 1
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Problems' AND COLUMN_NAME = 'Constraints')
        SET @HasConstraints = 1
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Problems' AND COLUMN_NAME = 'Hints')
        SET @HasHints = 1
    
    -- Insert based on available columns
    IF @HasExamples = 1 AND @HasConstraints = 1
    BEGIN
        INSERT INTO Problems (Id, Title, Description, Examples, Constraints, Difficulty)
        VALUES (1, 'Two Sum', 'Find two numbers that add up to target', '[{"input": "[2,7,11,15], 9", "output": "[0,1]"}]', '2 <= nums.length <= 10^4', 1)
    END
    ELSE
    BEGIN
        INSERT INTO Problems (Id, Title, Description, Difficulty)
        VALUES (1, 'Two Sum', 'Find two numbers that add up to target', 1)
    END
    
    PRINT '✓ Problem ID 1 inserted successfully'
END

PRINT ''
PRINT '============================================='
PRINT 'DIAGNOSIS COMPLETE'
PRINT '============================================='
PRINT ''
PRINT 'If Problem ID 1 now exists, your API should work!'
PRINT '============================================='
