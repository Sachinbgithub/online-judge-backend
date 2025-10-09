-- =============================================
-- Complete Fix for ProblemId in CodingTestSubmissionResults
-- =============================================
-- This script combines all the necessary steps to fix the ProblemId storage issue

PRINT 'Starting complete ProblemId fix for CodingTestSubmissionResults...';

-- Step 1: Check if ProblemId column exists
PRINT 'Step 1: Checking if ProblemId column exists...';
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'CodingTestSubmissionResults' 
    AND COLUMN_NAME = 'ProblemId'
)
BEGIN
    PRINT 'ProblemId column does not exist. Adding it now...';
    
    -- Add the ProblemId column
    ALTER TABLE CodingTestSubmissionResults
    ADD ProblemId int NOT NULL DEFAULT 0;
    
    PRINT 'ProblemId column added successfully.';
END
ELSE
BEGIN
    PRINT 'ProblemId column already exists.';
END

-- Step 2: Update existing records with ProblemId from TestCases table
PRINT 'Step 2: Updating existing records with ProblemId...';
UPDATE ctsr
SET ProblemId = tc.ProblemId
FROM CodingTestSubmissionResults ctsr
INNER JOIN TestCases tc ON ctsr.TestCaseId = tc.Id
WHERE ctsr.ProblemId = 0 OR ctsr.ProblemId IS NULL;

DECLARE @UpdatedRows INT = @@ROWCOUNT;
PRINT 'Updated ' + CAST(@UpdatedRows AS VARCHAR(10)) + ' existing records.';

-- Step 3: Add foreign key constraint to Problems table
PRINT 'Step 3: Adding foreign key constraint...';
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
        ON rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
    WHERE kcu.TABLE_NAME = 'CodingTestSubmissionResults' 
    AND kcu.COLUMN_NAME = 'ProblemId'
)
BEGIN
    -- Add foreign key constraint
    ALTER TABLE CodingTestSubmissionResults
    ADD CONSTRAINT FK_CodingTestSubmissionResults_Problems 
    FOREIGN KEY (ProblemId) REFERENCES Problems(Id);
    
    PRINT 'Foreign key constraint added successfully.';
END
ELSE
BEGIN
    PRINT 'Foreign key constraint already exists.';
END

-- Step 4: Add index for better query performance
PRINT 'Step 4: Adding index on ProblemId...';
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_CodingTestSubmissionResults_ProblemId' 
    AND object_id = OBJECT_ID('CodingTestSubmissionResults')
)
BEGIN
    CREATE INDEX IX_CodingTestSubmissionResults_ProblemId 
    ON CodingTestSubmissionResults (ProblemId);
    
    PRINT 'Index created successfully.';
END
ELSE
BEGIN
    PRINT 'Index already exists.';
END

-- Step 5: Verify the changes
PRINT 'Step 5: Verifying changes...';

-- Check updated table structure
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'CodingTestSubmissionResults'
AND COLUMN_NAME = 'ProblemId';

-- Check data integrity
SELECT 
    COUNT(*) as TotalRecords,
    COUNT(CASE WHEN ProblemId = 0 THEN 1 END) as RecordsWithNoProblemId,
    COUNT(CASE WHEN ProblemId > 0 THEN 1 END) as RecordsWithProblemId
FROM CodingTestSubmissionResults;

-- Sample verification query
IF EXISTS (SELECT 1 FROM CodingTestSubmissionResults WHERE ProblemId > 0)
BEGIN
    PRINT 'Sample data verification:';
    SELECT TOP 3
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

PRINT 'Complete ProblemId fix completed successfully!';
PRINT 'Next steps:';
PRINT '1. Restart your application to pick up the Entity Framework model changes';
PRINT '2. Test creating new submission results to verify ProblemId is stored correctly';
PRINT '3. Run queries to verify the data integrity';
