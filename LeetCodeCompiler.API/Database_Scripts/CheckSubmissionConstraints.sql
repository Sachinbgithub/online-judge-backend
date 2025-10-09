-- =============================================
-- Script to Check CodingTestSubmissions Constraints
-- =============================================

-- Check if the referenced entities exist
PRINT 'Checking referenced entities...';

-- Check if CodingTest exists
SELECT 'CodingTest Check' as CheckType, 
       COUNT(*) as Count,
       CASE WHEN COUNT(*) > 0 THEN 'EXISTS' ELSE 'MISSING' END as Status
FROM CodingTests 
WHERE Id = 1000019;

-- Check if CodingTestAttempt exists or can be created
SELECT 'CodingTestAttempt Check' as CheckType,
       COUNT(*) as Count,
       CASE WHEN COUNT(*) > 0 THEN 'EXISTS' ELSE 'MISSING' END as Status
FROM CodingTestAttempts 
WHERE CodingTestId = 1000019 AND UserId = 40115 AND AttemptNumber = 1;

-- Check if Problems exist
SELECT 'Problem Check' as CheckType,
       COUNT(*) as Count,
       CASE WHEN COUNT(*) > 0 THEN 'EXISTS' ELSE 'MISSING' END as Status
FROM Problems 
WHERE Id IN (6, 7);

-- Check if CodingTestQuestions exist for these problems
SELECT 'CodingTestQuestion Check' as CheckType,
       ctq.Id,
       ctq.CodingTestId,
       ctq.ProblemId,
       CASE WHEN ctq.Id IS NOT NULL THEN 'EXISTS' ELSE 'MISSING' END as Status
FROM (VALUES (6), (7)) AS p(ProblemId)
LEFT JOIN CodingTestQuestions ctq ON ctq.ProblemId = p.ProblemId AND ctq.CodingTestId = 1000019;

-- Check if TestCases exist for these problems
SELECT 'TestCase Check' as CheckType,
       tc.Id,
       tc.ProblemId,
       CASE WHEN tc.Id IS NOT NULL THEN 'EXISTS' ELSE 'MISSING' END as Status
FROM (VALUES (1), (2), (3), (4), (5), (6), (7), (8)) AS tcIds(TestCaseId)
LEFT JOIN TestCases tc ON tc.Id = tcIds.TestCaseId;

-- Check current table structure
PRINT 'Current CodingTestSubmissions table structure:';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'CodingTestSubmissions'
ORDER BY ORDINAL_POSITION;

-- Check foreign key constraints
PRINT 'Foreign key constraints:';
SELECT 
    tc.CONSTRAINT_NAME,
    tc.TABLE_NAME,
    kcu.COLUMN_NAME,
    ccu.TABLE_NAME AS FOREIGN_TABLE_NAME,
    ccu.COLUMN_NAME AS FOREIGN_COLUMN_NAME,
    rc.DELETE_RULE,
    rc.UPDATE_RULE
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc 
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
  ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE AS ccu
  ON ccu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS rc
  ON tc.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
WHERE tc.CONSTRAINT_TYPE = 'FOREIGN KEY' 
  AND tc.TABLE_NAME IN ('CodingTestSubmissions', 'CodingTestSubmissionResults');

-- Check if there are any existing submissions that might cause conflicts
SELECT 'Existing Submissions Check' as CheckType,
       COUNT(*) as Count
FROM CodingTestSubmissions 
WHERE UserId = 40115 AND CodingTestId = 1000019 AND AttemptNumber = 1;

PRINT 'Diagnostic check completed.';
