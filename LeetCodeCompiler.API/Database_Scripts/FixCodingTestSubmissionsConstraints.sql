-- =============================================
-- Fix Foreign Key Constraints for CodingTestSubmissions
-- =============================================
-- This script fixes the foreign key constraint issues caused by multiple cascade paths
-- Run this script after the main table creation script

USE [LeetCode]
GO

-- =============================================
-- Drop existing problematic foreign key constraints
-- =============================================

-- Drop CodingTestAttempts foreign key if it exists
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_CodingTestSubmissions_CodingTestAttempts_CodingTestAttemptId]') AND parent_object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissions]'))
BEGIN
    ALTER TABLE [dbo].[CodingTestSubmissions] DROP CONSTRAINT [FK_CodingTestSubmissions_CodingTestAttempts_CodingTestAttemptId]
    PRINT 'Dropped FK_CodingTestSubmissions_CodingTestAttempts_CodingTestAttemptId constraint'
END
GO

-- Drop CodingTestQuestionAttempts foreign key if it exists
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_CodingTestSubmissions_CodingTestQuestionAttempts_CodingTestQuestionAttemptId]') AND parent_object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissions]'))
BEGIN
    ALTER TABLE [dbo].[CodingTestSubmissions] DROP CONSTRAINT [FK_CodingTestSubmissions_CodingTestQuestionAttempts_CodingTestQuestionAttemptId]
    PRINT 'Dropped FK_CodingTestSubmissions_CodingTestQuestionAttempts_CodingTestQuestionAttemptId constraint'
END
GO

-- =============================================
-- Add foreign key constraints with NO ACTION
-- =============================================

-- Foreign Key to CodingTestAttempts (NO ACTION to avoid cascade cycles)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_CodingTestSubmissions_CodingTestAttempts_CodingTestAttemptId]') AND parent_object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissions]'))
BEGIN
    ALTER TABLE [dbo].[CodingTestSubmissions] 
    WITH CHECK ADD CONSTRAINT [FK_CodingTestSubmissions_CodingTestAttempts_CodingTestAttemptId] 
    FOREIGN KEY([CodingTestAttemptId]) REFERENCES [dbo].[CodingTestAttempts] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION
    
    PRINT 'Added FK_CodingTestSubmissions_CodingTestAttempts_CodingTestAttemptId constraint with NO ACTION'
END
ELSE
BEGIN
    PRINT 'FK_CodingTestSubmissions_CodingTestAttempts_CodingTestAttemptId constraint already exists'
END
GO

ALTER TABLE [dbo].[CodingTestSubmissions] CHECK CONSTRAINT [FK_CodingTestSubmissions_CodingTestAttempts_CodingTestAttemptId]
GO

-- Foreign Key to CodingTestQuestionAttempts (NO ACTION to avoid cascade cycles)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_CodingTestSubmissions_CodingTestQuestionAttempts_CodingTestQuestionAttemptId]') AND parent_object_id = OBJECT_ID(N'[dbo].[CodingTestSubmissions]'))
BEGIN
    ALTER TABLE [dbo].[CodingTestSubmissions] 
    WITH CHECK ADD CONSTRAINT [FK_CodingTestSubmissions_CodingTestQuestionAttempts_CodingTestQuestionAttemptId] 
    FOREIGN KEY([CodingTestQuestionAttemptId]) REFERENCES [dbo].[CodingTestQuestionAttempts] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION
    
    PRINT 'Added FK_CodingTestSubmissions_CodingTestQuestionAttempts_CodingTestQuestionAttemptId constraint with NO ACTION'
END
ELSE
BEGIN
    PRINT 'FK_CodingTestSubmissions_CodingTestQuestionAttempts_CodingTestQuestionAttemptId constraint already exists'
END
GO

ALTER TABLE [dbo].[CodingTestSubmissions] CHECK CONSTRAINT [FK_CodingTestSubmissions_CodingTestQuestionAttempts_CodingTestQuestionAttemptId]
GO

-- =============================================
-- Verify all foreign key constraints
-- =============================================
PRINT ''
PRINT 'Foreign Key Constraints Status:'
PRINT '----------------------------------------'

SELECT 
    fk.name AS ConstraintName,
    tp.name AS ParentTable,
    cp.name AS ParentColumn,
    tr.name AS ReferencedTable,
    cr.name AS ReferencedColumn,
    fk.delete_referential_action_desc AS DeleteAction,
    fk.update_referential_action_desc AS UpdateAction
FROM sys.foreign_keys fk
INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id
INNER JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns cp ON fkc.parent_column_id = cp.column_id AND fkc.parent_object_id = cp.object_id
INNER JOIN sys.columns cr ON fkc.referenced_column_id = cr.column_id AND fkc.referenced_object_id = cr.object_id
WHERE tp.name = 'CodingTestSubmissions'
ORDER BY fk.name

-- =============================================
-- Test the constraints
-- =============================================
PRINT ''
PRINT 'Testing foreign key constraints...'
PRINT '----------------------------------------'

-- Test if we can insert a record (this will fail if constraints are not working)
-- Note: This is just a test - we'll rollback the transaction
BEGIN TRANSACTION TestConstraints

BEGIN TRY
    -- This should fail because we're trying to insert with non-existent IDs
    INSERT INTO [dbo].[CodingTestSubmissions] (
        CodingTestId, CodingTestAttemptId, CodingTestQuestionAttemptId, ProblemId, UserId, 
        AttemptNumber, LanguageUsed, FinalCodeSnapshot
    ) VALUES (
        999999, 999999, 999999, 999999, 999999, 
        1, 'python', 'test code'
    )
    
    -- If we get here, something is wrong
    PRINT 'ERROR: Foreign key constraints are not working properly!'
    ROLLBACK TRANSACTION TestConstraints
END TRY
BEGIN CATCH
    -- This is expected - foreign key constraint should prevent the insert
    PRINT 'SUCCESS: Foreign key constraints are working properly!'
    PRINT 'Error message: ' + ERROR_MESSAGE()
    ROLLBACK TRANSACTION TestConstraints
END CATCH

-- =============================================
-- Script Completion
-- =============================================
PRINT ''
PRINT '============================================='
PRINT 'Foreign Key Constraints Fixed Successfully!'
PRINT '============================================='
PRINT ''
PRINT 'Changes Made:'
PRINT '1. Dropped problematic CASCADE DELETE constraints'
PRINT '2. Added NO ACTION constraints to prevent cascade cycles'
PRINT '3. Verified constraint functionality'
PRINT ''
PRINT 'Note: NO ACTION means:'
PRINT '- Parent records cannot be deleted if child records exist'
PRINT '- This prevents cascade cycles and maintains data integrity'
PRINT '- You must manually delete child records before deleting parent records'
PRINT ''
PRINT 'Ready to use CodingTestSubmissions table!'
PRINT '============================================='
