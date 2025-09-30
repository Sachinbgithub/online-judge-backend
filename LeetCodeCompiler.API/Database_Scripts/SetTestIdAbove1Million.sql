-- =============================================
-- Set CodingTests ID to start above 1,000,000
-- =============================================

USE [LeetCode]
GO

PRINT '============================================='
PRINT 'SETTING CODINGTESTS ID TO START ABOVE 1,000,000'
PRINT '============================================='

-- Step 1: Check current ID values
PRINT ''
PRINT '1. Checking current ID values...'
PRINT '--------------------------------'

SELECT 
    COUNT(*) as TotalTests,
    MIN(Id) as MinId,
    MAX(Id) as MaxId
FROM CodingTests;

-- Step 2: Check if there are any existing records
DECLARE @MaxId INT;
SELECT @MaxId = ISNULL(MAX(Id), 0) FROM CodingTests;

PRINT ''
PRINT '2. Current maximum ID: ' + CAST(@MaxId AS VARCHAR(10))
PRINT '----------------------------------------'

-- Step 3: Set the identity seed to start from 1,000,001
PRINT ''
PRINT '3. Setting identity seed to 1,000,001...'
PRINT '---------------------------------------'

-- First, check if the table has an identity column
IF EXISTS (SELECT * FROM sys.identity_columns WHERE object_id = OBJECT_ID('CodingTests'))
BEGIN
    -- Reset the identity seed
    DBCC CHECKIDENT('CodingTests', RESEED, 1000000);
    PRINT '✓ Identity seed set to 1,000,000 (next ID will be 1,000,001)'
END
ELSE
BEGIN
    PRINT '✗ CodingTests table does not have an identity column'
    PRINT 'The Id column might not be set as IDENTITY'
END

-- Step 4: Verify the identity seed
PRINT ''
PRINT '4. Verifying identity seed...'
PRINT '----------------------------'

SELECT 
    IDENT_CURRENT('CodingTests') as CurrentIdentityValue,
    IDENT_INCR('CodingTests') as IdentityIncrement,
    IDENT_SEED('CodingTests') as IdentitySeed
FROM CodingTests;

-- Step 5: Test insert to verify next ID
PRINT ''
PRINT '5. Testing next ID generation...'
PRINT '-------------------------------'

-- Insert a test record to see the next ID
INSERT INTO CodingTests (
    TestName, CreatedBy, CreatedAt, StartDate, EndDate, 
    DurationMinutes, TotalQuestions, TotalMarks, IsActive, 
    IsPublished, TestType, AllowMultipleAttempts, MaxAttempts,
    ShowResultsImmediately, AllowCodeReview, AccessCode, Tags,
    IsResultPublishAutomatically, ApplyBreachRule, BreachRuleLimit,
    HostIP, ClassId
)
VALUES (
    'TEST_RECORD_FOR_ID_CHECK', 1, GETUTCDATE(), 
    GETUTCDATE(), DATEADD(HOUR, 2, GETUTCDATE()),
    60, 1, 10, 1, 0, 1, 0, 1, 1, 0, 'TEST', 'test',
    1, 1, 0, '127.0.0.1', 0
);

-- Get the ID of the inserted record
DECLARE @TestId INT = SCOPE_IDENTITY();
PRINT '✓ Test record inserted with ID: ' + CAST(@TestId AS VARCHAR(10))

-- Delete the test record
DELETE FROM CodingTests WHERE Id = @TestId;
PRINT '✓ Test record deleted (cleanup complete)'

-- Step 6: Final verification
PRINT ''
PRINT '6. Final verification...'
PRINT '----------------------'

SELECT 
    IDENT_CURRENT('CodingTests') as NextIdWillBe,
    COUNT(*) as TotalExistingRecords
FROM CodingTests;

PRINT ''
PRINT '============================================='
PRINT 'CODINGTESTS ID CONFIGURATION COMPLETE!'
PRINT '============================================='
PRINT ''
PRINT 'Next test ID will be: ' + CAST(IDENT_CURRENT('CodingTests') + 1 AS VARCHAR(10))
PRINT 'All new tests will have IDs above 1,000,000'
PRINT '============================================='
