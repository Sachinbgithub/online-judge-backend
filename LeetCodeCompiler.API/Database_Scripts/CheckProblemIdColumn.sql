-- =============================================
-- Check if ProblemId column exists in CodingTestSubmissionResults
-- =============================================

PRINT 'Checking if ProblemId column exists in CodingTestSubmissionResults table...';

-- Check if the column exists
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'CodingTestSubmissionResults' 
    AND COLUMN_NAME = 'ProblemId'
)
BEGIN
    PRINT '✅ ProblemId column EXISTS in CodingTestSubmissionResults table.';
    
    -- Show column details
    SELECT 
        COLUMN_NAME,
        DATA_TYPE,
        IS_NULLABLE,
        COLUMN_DEFAULT,
        CHARACTER_MAXIMUM_LENGTH
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'CodingTestSubmissionResults' 
    AND COLUMN_NAME = 'ProblemId';
END
ELSE
BEGIN
    PRINT '❌ ProblemId column DOES NOT EXIST in CodingTestSubmissionResults table.';
    PRINT 'You need to run the AddProblemIdToSubmissionResults.sql script first.';
END

-- Check current table structure
PRINT 'Current CodingTestSubmissionResults table structure:';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'CodingTestSubmissionResults'
ORDER BY ORDINAL_POSITION;

-- Check if there are any existing records
PRINT 'Checking existing records...';
SELECT COUNT(*) as TotalRecords FROM CodingTestSubmissionResults;

-- If column exists, check if any records have ProblemId populated
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'CodingTestSubmissionResults' 
    AND COLUMN_NAME = 'ProblemId'
)
BEGIN
    SELECT 
        COUNT(*) as TotalRecords,
        COUNT(CASE WHEN ProblemId = 0 OR ProblemId IS NULL THEN 1 END) as RecordsWithNoProblemId,
        COUNT(CASE WHEN ProblemId > 0 THEN 1 END) as RecordsWithProblemId
    FROM CodingTestSubmissionResults;
END
