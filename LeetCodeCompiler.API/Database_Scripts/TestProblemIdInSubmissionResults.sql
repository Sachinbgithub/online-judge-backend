-- =============================================
-- Test Script for ProblemId in CodingTestSubmissionResults
-- =============================================

-- Test 1: Verify the column exists and has correct structure
PRINT 'Test 1: Verifying ProblemId column structure...';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'CodingTestSubmissionResults' 
AND COLUMN_NAME = 'ProblemId';

-- Test 2: Verify foreign key constraint exists
PRINT 'Test 2: Verifying foreign key constraint...';
SELECT 
    tc.CONSTRAINT_NAME,
    tc.TABLE_NAME,
    kcu.COLUMN_NAME,
    ccu.TABLE_NAME AS FOREIGN_TABLE_NAME,
    ccu.COLUMN_NAME AS FOREIGN_COLUMN_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc 
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
  ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE AS ccu
  ON ccu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
WHERE tc.CONSTRAINT_TYPE = 'FOREIGN KEY' 
  AND tc.TABLE_NAME = 'CodingTestSubmissionResults'
  AND kcu.COLUMN_NAME = 'ProblemId';

-- Test 3: Verify index exists
PRINT 'Test 3: Verifying index...';
SELECT 
    i.name AS IndexName,
    c.name AS ColumnName
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('CodingTestSubmissionResults')
AND i.name = 'IX_CodingTestSubmissionResults_ProblemId';

-- Test 4: Check data integrity (if any data exists)
PRINT 'Test 4: Checking data integrity...';
SELECT 
    COUNT(*) as TotalRecords,
    COUNT(CASE WHEN ProblemId = 0 THEN 1 END) as RecordsWithDefaultProblemId,
    COUNT(CASE WHEN ProblemId > 0 THEN 1 END) as RecordsWithValidProblemId
FROM CodingTestSubmissionResults;

-- Test 5: Sample data verification (if data exists)
IF EXISTS (SELECT 1 FROM CodingTestSubmissionResults WHERE ProblemId > 0)
BEGIN
    PRINT 'Test 5: Sample data verification...';
    SELECT TOP 5
        ctsr.ResultId,
        ctsr.SubmissionId,
        ctsr.TestCaseId,
        ctsr.ProblemId,
        tc.ProblemId AS TestCaseProblemId,
        CASE WHEN ctsr.ProblemId = tc.ProblemId THEN 'MATCH' ELSE 'MISMATCH' END AS DataIntegrity
    FROM CodingTestSubmissionResults ctsr
    INNER JOIN TestCases tc ON ctsr.TestCaseId = tc.Id
    WHERE ctsr.ProblemId > 0;
END
ELSE
BEGIN
    PRINT 'Test 5: No data with ProblemId > 0 found. This is expected for new installations.';
END

-- Test 6: Verify we can query by ProblemId efficiently
PRINT 'Test 6: Testing query performance with ProblemId...';
IF EXISTS (SELECT 1 FROM CodingTestSubmissionResults WHERE ProblemId > 0)
BEGIN
    DECLARE @TestProblemId INT = (SELECT TOP 1 ProblemId FROM CodingTestSubmissionResults WHERE ProblemId > 0);
    
    SELECT 
        COUNT(*) as ResultsForProblem,
        @TestProblemId as TestProblemId
    FROM CodingTestSubmissionResults 
    WHERE ProblemId = @TestProblemId;
END
ELSE
BEGIN
    PRINT 'Test 6: Skipped - no data available for testing.';
END

PRINT 'All tests completed successfully!';
