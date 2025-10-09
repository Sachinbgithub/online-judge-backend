-- =============================================
-- Script to Drop and Recreate CodingTestSubmissions Tables
-- =============================================

-- Drop existing tables if they exist (in correct order due to foreign key constraints)
IF OBJECT_ID('CodingTestSubmissionResults', 'U') IS NOT NULL
    DROP TABLE CodingTestSubmissionResults;

IF OBJECT_ID('CodingTestSubmissions', 'U') IS NOT NULL
    DROP TABLE CodingTestSubmissions;

-- =============================================
-- Create CodingTestSubmissions Table
-- =============================================
CREATE TABLE CodingTestSubmissions (
    SubmissionId BIGINT IDENTITY(1,1) PRIMARY KEY,
    CodingTestId INT NOT NULL,
    CodingTestAttemptId INT NOT NULL,
    CodingTestQuestionAttemptId INT NULL, -- Made nullable for whole test submissions
    ProblemId INT NULL, -- Made nullable for whole test submissions
    UserId BIGINT NOT NULL,
    AttemptNumber INT NOT NULL,
    LanguageUsed NVARCHAR(50) NOT NULL DEFAULT '',
    FinalCodeSnapshot NVARCHAR(MAX) NOT NULL DEFAULT '',
    
    -- Test Case Results
    TotalTestCases INT NOT NULL DEFAULT 0,
    PassedTestCases INT NOT NULL DEFAULT 0,
    FailedTestCases INT NOT NULL DEFAULT 0,
    RequestedHelp BIT NOT NULL DEFAULT 0,
    
    -- Activity Tracking Metrics
    LanguageSwitchCount INT NOT NULL DEFAULT 0,
    RunClickCount INT NOT NULL DEFAULT 0,
    SubmitClickCount INT NOT NULL DEFAULT 0,
    EraseCount INT NOT NULL DEFAULT 0,
    SaveCount INT NOT NULL DEFAULT 0,
    LoginLogoutCount INT NOT NULL DEFAULT 0,
    IsSessionAbandoned BIT NOT NULL DEFAULT 0,
    
    -- Submission Details
    SubmissionTime DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExecutionTimeMs INT NOT NULL DEFAULT 0,
    MemoryUsedKB INT NOT NULL DEFAULT 0,
    Score INT NOT NULL DEFAULT 0,
    MaxScore INT NOT NULL DEFAULT 0,
    IsCorrect BIT NOT NULL DEFAULT 0,
    IsLateSubmission BIT NOT NULL DEFAULT 0,
    
    -- Additional Metadata
    ClassId INT NULL,
    UserIP NVARCHAR(50) NULL,
    UserAgent NVARCHAR(500) NULL,
    BrowserInfo NVARCHAR(200) NULL,
    DeviceInfo NVARCHAR(200) NULL,
    
    -- Error Handling
    ErrorMessage NVARCHAR(1000) NULL,
    ErrorType NVARCHAR(100) NULL,
    CompilationError NVARCHAR(1000) NULL,
    RuntimeError NVARCHAR(1000) NULL,
    
    -- Timestamps
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    
    -- Foreign Key Constraints
    CONSTRAINT FK_CodingTestSubmissions_CodingTest 
        FOREIGN KEY (CodingTestId) REFERENCES CodingTests(Id),
    CONSTRAINT FK_CodingTestSubmissions_CodingTestAttempt 
        FOREIGN KEY (CodingTestAttemptId) REFERENCES CodingTestAttempts(Id),
    CONSTRAINT FK_CodingTestSubmissions_CodingTestQuestionAttempt 
        FOREIGN KEY (CodingTestQuestionAttemptId) REFERENCES CodingTestQuestionAttempts(Id),
    CONSTRAINT FK_CodingTestSubmissions_Problem 
        FOREIGN KEY (ProblemId) REFERENCES Problems(Id)
);

-- =============================================
-- Create CodingTestSubmissionResults Table
-- =============================================
CREATE TABLE CodingTestSubmissionResults (
    ResultId BIGINT IDENTITY(1,1) PRIMARY KEY,
    SubmissionId BIGINT NOT NULL,
    TestCaseId INT NOT NULL,
    TestCaseOrder INT NOT NULL,
    Input NVARCHAR(MAX) NOT NULL DEFAULT '',
    ExpectedOutput NVARCHAR(MAX) NOT NULL DEFAULT '',
    ActualOutput NVARCHAR(MAX) NULL,
    IsPassed BIT NOT NULL DEFAULT 0,
    ExecutionTimeMs INT NOT NULL DEFAULT 0,
    MemoryUsedKB INT NOT NULL DEFAULT 0,
    ErrorMessage NVARCHAR(1000) NULL,
    ErrorType NVARCHAR(100) NULL, -- CompilationError, RuntimeError, TimeoutError, WrongAnswer
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    -- Foreign Key Constraints
    CONSTRAINT FK_CodingTestSubmissionResults_Submission 
        FOREIGN KEY (SubmissionId) REFERENCES CodingTestSubmissions(SubmissionId) ON DELETE CASCADE,
    CONSTRAINT FK_CodingTestSubmissionResults_TestCase 
        FOREIGN KEY (TestCaseId) REFERENCES TestCases(Id)
);

-- =============================================
-- Create Indexes for Performance
-- =============================================

-- Indexes for CodingTestSubmissions
CREATE NONCLUSTERED INDEX IX_CodingTestSubmissions_CodingTestId 
    ON CodingTestSubmissions(CodingTestId);

CREATE NONCLUSTERED INDEX IX_CodingTestSubmissions_UserId 
    ON CodingTestSubmissions(UserId);

CREATE NONCLUSTERED INDEX IX_CodingTestSubmissions_CodingTestAttemptId 
    ON CodingTestSubmissions(CodingTestAttemptId);

CREATE NONCLUSTERED INDEX IX_CodingTestSubmissions_SubmissionTime 
    ON CodingTestSubmissions(SubmissionTime);

CREATE NONCLUSTERED INDEX IX_CodingTestSubmissions_CreatedAt 
    ON CodingTestSubmissions(CreatedAt);

-- Indexes for CodingTestSubmissionResults
CREATE NONCLUSTERED INDEX IX_CodingTestSubmissionResults_SubmissionId 
    ON CodingTestSubmissionResults(SubmissionId);

CREATE NONCLUSTERED INDEX IX_CodingTestSubmissionResults_TestCaseId 
    ON CodingTestSubmissionResults(TestCaseId);

CREATE NONCLUSTERED INDEX IX_CodingTestSubmissionResults_TestCaseOrder 
    ON CodingTestSubmissionResults(SubmissionId, TestCaseOrder);

-- =============================================
-- Add Entity Framework Model Configurations
-- =============================================

-- Note: You may need to add these configurations to your AppDbContext.OnModelCreating method:
/*
// CodingTestSubmission configurations
modelBuilder.Entity<CodingTestSubmission>()
    .Property(e => e.CreatedAt)
    .HasDefaultValueSql("GETUTCDATE()");

modelBuilder.Entity<CodingTestSubmission>()
    .Property(e => e.SubmissionTime)
    .HasDefaultValueSql("GETUTCDATE()");

// CodingTestSubmissionResult configurations
modelBuilder.Entity<CodingTestSubmissionResult>()
    .Property(e => e.CreatedAt)
    .HasDefaultValueSql("GETUTCDATE()");

// Relationships
modelBuilder.Entity<CodingTestSubmission>()
    .HasOne(s => s.CodingTest)
    .WithMany()
    .HasForeignKey(s => s.CodingTestId);

modelBuilder.Entity<CodingTestSubmission>()
    .HasOne(s => s.CodingTestAttempt)
    .WithMany()
    .HasForeignKey(s => s.CodingTestAttemptId);

modelBuilder.Entity<CodingTestSubmission>()
    .HasOne(s => s.CodingTestQuestionAttempt)
    .WithMany()
    .HasForeignKey(s => s.CodingTestQuestionAttemptId);

modelBuilder.Entity<CodingTestSubmission>()
    .HasOne(s => s.Problem)
    .WithMany()
    .HasForeignKey(s => s.ProblemId);

modelBuilder.Entity<CodingTestSubmission>()
    .HasMany(s => s.SubmissionResults)
    .WithOne(r => r.Submission)
    .HasForeignKey(r => r.SubmissionId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<CodingTestSubmissionResult>()
    .HasOne(r => r.TestCase)
    .WithMany()
    .HasForeignKey(r => r.TestCaseId);
*/

PRINT 'CodingTestSubmissions tables created successfully!';
