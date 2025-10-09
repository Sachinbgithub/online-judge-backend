-- =============================================
-- Script to Check TestCase IDs
-- =============================================

PRINT 'Checking TestCase IDs from the request...';

-- Check which TestCase IDs exist from your request (1, 2, 3, 4, 5, 6, 7, 8)
SELECT 
    'Requested TestCase IDs' as CheckType,
    tc.Id,
    tc.ProblemId,
    CASE WHEN tc.Id IS NOT NULL THEN 'EXISTS' ELSE 'MISSING' END as Status,
    tc.Input,
    tc.ExpectedOutput
FROM (VALUES (1), (2), (3), (4), (5), (6), (7), (8)) AS requested(TestCaseId)
LEFT JOIN TestCases tc ON tc.Id = requested.TestCaseId
ORDER BY requested.TestCaseId;

PRINT '';
PRINT 'Available TestCase IDs for Problem 6 (Reverse Integer):';
SELECT 
    Id,
    ProblemId,
    Input,
    ExpectedOutput
FROM TestCases 
WHERE ProblemId = 6
ORDER BY Id;

PRINT '';
PRINT 'Available TestCase IDs for Problem 7:';
SELECT 
    Id,
    ProblemId,
    Input,
    ExpectedOutput
FROM TestCases 
WHERE ProblemId = 7
ORDER BY Id;

PRINT '';
PRINT 'All available TestCase IDs:';
SELECT 
    Id,
    ProblemId,
    Input,
    ExpectedOutput
FROM TestCases 
ORDER BY ProblemId, Id;

PRINT '';
PRINT 'Count of TestCases per Problem:';
SELECT 
    ProblemId,
    COUNT(*) as TestCaseCount
FROM TestCases 
GROUP BY ProblemId
ORDER BY ProblemId;
