-- Check if Coding Test tables exist
SELECT 
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('CodingTests', 'CodingTestQuestions', 'CodingTestAttempts', 'CodingTestQuestionAttempts', 'CodingTestTopicData')
ORDER BY TABLE_NAME;

-- Check if Problems table exists and has data
SELECT COUNT(*) as ProblemCount FROM Problems;

-- Check first few problems
SELECT TOP 5 Id, Title FROM Problems;

-- Check if CodingTests table has any data
SELECT COUNT(*) as CodingTestCount FROM CodingTests;

-- Check table structure of CodingTests
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'CodingTests'
ORDER BY ORDINAL_POSITION;
