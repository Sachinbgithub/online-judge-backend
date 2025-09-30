-- =============================================
-- Create AssignedCodingTests Table
-- =============================================

USE [LeetCode]
GO

PRINT '============================================='
PRINT 'CREATING ASSIGNEDCODINGTESTS TABLE'
PRINT '============================================='

-- Step 1: Create the AssignedCodingTests table
PRINT ''
PRINT '1. Creating AssignedCodingTests table...'
PRINT '----------------------------------------'

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AssignedCodingTests]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AssignedCodingTests](
        [AssignedId] [bigint] IDENTITY(1000001,1) NOT NULL, -- Start above 1M
        [CodingTestId] [int] NOT NULL,
        [AssignedToUserId] [bigint] NOT NULL,
        [AssignedToUserType] [tinyint] NOT NULL, -- User type (25, 1, 3, etc.)
        [AssignedByUserId] [bigint] NOT NULL,
        [AssignedDate] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [TestType] [int] NOT NULL DEFAULT 1002, -- Your custom test type
        [TestMode] [tinyint] NOT NULL DEFAULT 5, -- Your custom test mode
        [IsDeleted] [bit] NOT NULL DEFAULT 0,
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] [datetime2](7) NULL,
        
        CONSTRAINT [PK_AssignedCodingTests] PRIMARY KEY CLUSTERED ([AssignedId] ASC)
    );
    
    PRINT '✓ AssignedCodingTests table created successfully'
END
ELSE
BEGIN
    PRINT '✓ AssignedCodingTests table already exists'
END

-- Step 2: Create foreign key constraints
PRINT ''
PRINT '2. Creating foreign key constraints...'
PRINT '-------------------------------------'

-- Foreign key to CodingTests
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AssignedCodingTests_CodingTests')
BEGIN
    ALTER TABLE [dbo].[AssignedCodingTests]
    ADD CONSTRAINT [FK_AssignedCodingTests_CodingTests] 
    FOREIGN KEY([CodingTestId]) REFERENCES [dbo].[CodingTests] ([Id]) ON DELETE CASCADE;
    PRINT '✓ Foreign key to CodingTests created'
END
ELSE
BEGIN
    PRINT '✓ Foreign key to CodingTests already exists'
END

-- Step 3: Create indexes for performance
PRINT ''
PRINT '3. Creating indexes...'
PRINT '----------------------'

-- Index for user-based queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssignedCodingTests_User')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AssignedCodingTests_User] 
    ON [dbo].[AssignedCodingTests] ([AssignedToUserId], [AssignedToUserType], [IsDeleted]);
    PRINT '✓ User index created'
END
ELSE
BEGIN
    PRINT '✓ User index already exists'
END

-- Index for test-based queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssignedCodingTests_Test')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AssignedCodingTests_Test] 
    ON [dbo].[AssignedCodingTests] ([CodingTestId], [IsDeleted]);
    PRINT '✓ Test index created'
END
ELSE
BEGIN
    PRINT '✓ Test index already exists'
END

-- Index for assignment tracking
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AssignedCodingTests_Assignment')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AssignedCodingTests_Assignment] 
    ON [dbo].[AssignedCodingTests] ([AssignedByUserId], [AssignedDate]);
    PRINT '✓ Assignment index created'
END
ELSE
BEGIN
    PRINT '✓ Assignment index already exists'
END

-- Step 4: Create unique constraint to prevent duplicate assignments
PRINT ''
PRINT '4. Creating unique constraint...'
PRINT '------------------------------'

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_AssignedCodingTests_UserTest')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_AssignedCodingTests_UserTest] 
    ON [dbo].[AssignedCodingTests] ([CodingTestId], [AssignedToUserId], [AssignedToUserType])
    WHERE [IsDeleted] = 0;
    PRINT '✓ Unique constraint created (prevents duplicate assignments)'
END
ELSE
BEGIN
    PRINT '✓ Unique constraint already exists'
END

-- Step 5: Insert sample data
PRINT ''
PRINT '5. Inserting sample data...'
PRINT '--------------------------'

-- Check if we have any coding tests to assign
IF EXISTS (SELECT * FROM CodingTests WHERE Id > 0)
BEGIN
    -- Insert sample assignments (only if they don't exist)
    IF NOT EXISTS (SELECT * FROM AssignedCodingTests WHERE AssignedId = 1000001)
    BEGIN
        INSERT INTO AssignedCodingTests (
            CodingTestId, AssignedToUserId, AssignedToUserType, 
            AssignedByUserId, TestType, TestMode
        )
        SELECT TOP 3 
            Id as CodingTestId,
            12345 as AssignedToUserId,  -- Sample user
            25 as AssignedToUserType,   -- Sample user type
            4021 as AssignedByUserId,   -- Sample assigner
            1002 as TestType,
            5 as TestMode
        FROM CodingTests 
        WHERE Id > 0;
        
        PRINT '✓ Sample assignments inserted'
    END
    ELSE
    BEGIN
        PRINT '✓ Sample assignments already exist'
    END
END
ELSE
BEGIN
    PRINT '⚠ No coding tests found - skipping sample data insertion'
END

-- Step 6: Verify table structure
PRINT ''
PRINT '6. Verifying table structure...'
PRINT '------------------------------'

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'AssignedCodingTests'
ORDER BY ORDINAL_POSITION;

-- Step 7: Show sample data
PRINT ''
PRINT '7. Sample data:'
PRINT '--------------'

SELECT TOP 5 
    AssignedId,
    CodingTestId,
    AssignedToUserId,
    AssignedToUserType,
    AssignedByUserId,
    AssignedDate,
    TestType,
    TestMode,
    IsDeleted
FROM AssignedCodingTests
ORDER BY AssignedId;

PRINT ''
PRINT '============================================='
PRINT 'ASSIGNEDCODINGTESTS TABLE CREATION COMPLETE!'
PRINT '============================================='
PRINT ''
PRINT 'Table Features:'
PRINT '• ID starts from 1,000,001'
PRINT '• TestType = 1002 (your custom type)'
PRINT '• TestMode = 5 (your custom mode)'
PRINT '• Foreign key to CodingTests'
PRINT '• Performance indexes'
PRINT '• Unique constraint prevents duplicates'
PRINT '• Soft delete support (IsDeleted)'
PRINT '============================================='
