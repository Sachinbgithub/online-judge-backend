-- =============================================
-- Update TestType from String to Integer
-- =============================================

USE [LeetCode]
GO

PRINT '============================================='
PRINT 'UPDATING TESTTYPE FROM STRING TO INTEGER'
PRINT '============================================='

-- Step 1: Check current TestType values
PRINT ''
PRINT '1. Checking current TestType values...'
PRINT '------------------------------------'

SELECT DISTINCT TestType, COUNT(*) as Count
FROM CodingTests 
GROUP BY TestType;

-- Step 2: Add a temporary column for the new integer TestType
PRINT ''
PRINT '2. Adding temporary TestTypeInt column...'
PRINT '----------------------------------------'

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'CodingTests' AND COLUMN_NAME = 'TestTypeInt')
BEGIN
    ALTER TABLE CodingTests 
    ADD TestTypeInt INT DEFAULT 1;
    PRINT '✓ TestTypeInt column added'
END
ELSE
BEGIN
    PRINT '✓ TestTypeInt column already exists'
END

-- Step 3: Update the temporary column with integer values
PRINT ''
PRINT '3. Converting string values to integers...'
PRINT '----------------------------------------'

-- Update based on string values
UPDATE CodingTests 
SET TestTypeInt = CASE 
    WHEN TestType = 'Practice' THEN 1
    WHEN TestType = 'Assessment' THEN 2
    WHEN TestType = 'Exam' THEN 3
    ELSE 1  -- Default to Practice
END;

PRINT '✓ String values converted to integers'

-- Step 4: Check the conversion results
PRINT ''
PRINT '4. Checking conversion results...'
PRINT '--------------------------------'

SELECT 
    TestType as OldValue,
    TestTypeInt as NewValue,
    COUNT(*) as Count
FROM CodingTests 
GROUP BY TestType, TestTypeInt
ORDER BY TestTypeInt;

-- Step 5: Drop the old TestType column
PRINT ''
PRINT '5. Dropping old TestType column...'
PRINT '--------------------------------'

ALTER TABLE CodingTests DROP COLUMN TestType;
PRINT '✓ Old TestType column dropped'

-- Step 6: Rename TestTypeInt to TestType
PRINT ''
PRINT '6. Renaming TestTypeInt to TestType...'
PRINT '-------------------------------------'

EXEC sp_rename 'CodingTests.TestTypeInt', 'TestType', 'COLUMN';
PRINT '✓ Column renamed to TestType'

-- Step 7: Add NOT NULL constraint and default value
PRINT ''
PRINT '7. Adding constraints...'
PRINT '----------------------'

ALTER TABLE CodingTests 
ALTER COLUMN TestType INT NOT NULL;

ALTER TABLE CodingTests 
ADD CONSTRAINT DF_CodingTests_TestType DEFAULT 1 FOR TestType;

PRINT '✓ Constraints added'

-- Step 8: Verify final result
PRINT ''
PRINT '8. Final verification...'
PRINT '-----------------------'

SELECT 
    TestType,
    COUNT(*) as Count
FROM CodingTests 
GROUP BY TestType
ORDER BY TestType;

-- Show table structure
PRINT ''
PRINT 'Updated table structure:'
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'CodingTests' AND COLUMN_NAME = 'TestType';

PRINT ''
PRINT '============================================='
PRINT 'TESTTYPE UPDATE COMPLETE!'
PRINT '============================================='
PRINT ''
PRINT 'TestType values:'
PRINT '1 = Practice'
PRINT '2 = Assessment' 
PRINT '3 = Exam'
PRINT '============================================='
