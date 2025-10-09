-- =============================================
-- Script to Add ProblemId Column to CodingTestSubmissionResults Table
-- =============================================

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

-- Add ProblemId column to CodingTestSubmissionResults table
PRINT 'Adding ProblemId column to CodingTestSubmissionResults...';

-- Check if column already exists
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'CodingTestSubmissionResults' 
    AND COLUMN_NAME = 'ProblemId'
)
BEGIN
    -- Add the ProblemId column
    ALTER TABLE CodingTestSubmissionResults
    ADD ProblemId int NOT NULL DEFAULT 0;
    
    PRINT 'ProblemId column added successfully.';
END
ELSE
BEGIN
    PRINT 'ProblemId column already exists.';
END

-- Update existing records with ProblemId from TestCases table
PRINT 'Updating existing records with ProblemId...';

UPDATE ctsr
SET ProblemId = tc.ProblemId
FROM CodingTestSubmissionResults ctsr
INNER JOIN TestCases tc ON ctsr.TestCaseId = tc.Id
WHERE ctsr.ProblemId = 0 OR ctsr.ProblemId IS NULL;

PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' existing records.';

-- Add foreign key constraint to Problems table
PRINT 'Adding foreign key constraint...';

-- Check if foreign key already exists
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

-- Add index for better query performance
PRINT 'Adding index on ProblemId...';

-- Check if index already exists
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

-- Verify the changes
PRINT 'Verifying changes...';

-- Check updated table structure
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'CodingTestSubmissionResults'
ORDER BY ORDINAL_POSITION;

-- Check foreign key constraints
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

-- Check indexes
SELECT 
    i.name AS IndexName,
    c.name AS ColumnName
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('CodingTestSubmissionResults')
AND i.name = 'IX_CodingTestSubmissionResults_ProblemId';

-- Sample query to verify data integrity
PRINT 'Sample verification query:';
SELECT TOP 5
    ctsr.ResultId,
    ctsr.SubmissionId,
    ctsr.TestCaseId,
    ctsr.ProblemId,
    tc.ProblemId AS TestCaseProblemId,
    CASE WHEN ctsr.ProblemId = tc.ProblemId THEN 'MATCH' ELSE 'MISMATCH' END AS DataIntegrity
FROM CodingTestSubmissionResults ctsr
INNER JOIN TestCases tc ON ctsr.TestCaseId = tc.Id;

PRINT 'Script completed successfully!';
