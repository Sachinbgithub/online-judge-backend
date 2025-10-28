using Microsoft.EntityFrameworkCore;
using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Problem> Problems { get; set; }
        public DbSet<TestCase> TestCases { get; set; }
        public DbSet<StarterCode> StarterCodes { get; set; }
        public DbSet<ProblemHint> ProblemHints { get; set; }
        
        // Domain and Subdomain DbSets
        public DbSet<Domain> Domains { get; set; }
        public DbSet<Subdomain> Subdomains { get; set; }
        public DbSet<Difficulty> Difficulties { get; set; }
        public DbSet<Language> Languages { get; set; }
        
        // New DbSets for activity tracking
        public DbSet<UserCodingActivityLog> UserCodingActivityLogs { get; set; }
        public DbSet<CoreQuestionResult> CoreQuestionResults { get; set; }
        public DbSet<CoreTestCaseResult> CoreTestCaseResults { get; set; }
        
        // New DbSets for coding tests
        public DbSet<CodingTest> CodingTests { get; set; }
        public DbSet<CodingTestQuestion> CodingTestQuestions { get; set; }
        public DbSet<CodingTestAttempt> CodingTestAttempts { get; set; }
        public DbSet<CodingTestQuestionAttempt> CodingTestQuestionAttempts { get; set; }
        public DbSet<CodingTestTopicData> CodingTestTopicData { get; set; }
        public DbSet<AssignedCodingTest> AssignedCodingTests { get; set; }
        public DbSet<CodingTestSubmission> CodingTestSubmissions { get; set; }
        public DbSet<CodingTestSubmissionResult> CodingTestSubmissionResults { get; set; }

        // Practice Test Tables
        public DbSet<PracticeTest> PracticeTests { get; set; }
        public DbSet<PracticeTestQuestion> PracticeTestQuestions { get; set; }
        public DbSet<PracticeTestResult> PracticeTestResults { get; set; }
        public DbSet<PracticeTestQuestionResult> PracticeTestQuestionResults { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Table name configurations
            modelBuilder.Entity<UserCodingActivityLog>().ToTable("UserCodingActivityLog");
            modelBuilder.Entity<CoreTestCaseResult>().ToTable("CoreTestCaseResult");
            modelBuilder.Entity<CoreQuestionResult>().ToTable("CoreQuestionResult");

            // Relationships
            modelBuilder.Entity<TestCase>()
                .HasOne<Problem>()
                .WithMany(p => p.TestCases)
                .HasForeignKey(tc => tc.ProblemId);

            modelBuilder.Entity<StarterCode>()
                .HasOne(sc => sc.Problem)
                .WithMany(p => p.StarterCodes)
                .HasForeignKey(sc => sc.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Language entity
            modelBuilder.Entity<Language>().ToTable("Languages");
            modelBuilder.Entity<Language>()
                .Property(l => l.Id)
                .ValueGeneratedNever(); // Manual ID generation
            modelBuilder.Entity<Language>()
                .Property(l => l.LanguageName)
                .HasColumnName("Language");

            // Configure Language relationship
            modelBuilder.Entity<StarterCode>()
                .HasOne(sc => sc.LanguageNavigation)
                .WithMany()
                .HasForeignKey(sc => sc.Language)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure ProblemHint relationship
            modelBuilder.Entity<ProblemHint>()
                .HasOne(ph => ph.Problem)
                .WithMany()
                .HasForeignKey(ph => ph.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            // New relationships for activity tracking
            modelBuilder.Entity<CoreTestCaseResult>()
                .HasOne<CoreQuestionResult>()
                .WithMany()
                .HasForeignKey(ctr => ctr.CoreQuestionResultId);

            modelBuilder.Entity<UserCodingActivityLog>()
                .HasOne<Problem>()
                .WithMany()
                .HasForeignKey(ucal => ucal.ProblemId);

            modelBuilder.Entity<CoreQuestionResult>()
                .HasOne<Problem>()
                .WithMany()
                .HasForeignKey(cqr => cqr.ProblemId);

            // Configure timestamps for activity tracking entities
            modelBuilder.Entity<UserCodingActivityLog>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<UserCodingActivityLog>()
                .Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<CoreQuestionResult>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<CoreQuestionResult>()
                .Property(e => e.LastSubmittedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<CoreTestCaseResult>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Coding Test relationships
            modelBuilder.Entity<CodingTestQuestion>()
                .HasOne(ctq => ctq.CodingTest)
                .WithMany(ct => ct.Questions)
                .HasForeignKey(ctq => ctq.CodingTestId);

            modelBuilder.Entity<CodingTestQuestion>()
                .HasOne(ctq => ctq.Problem)
                .WithMany()
                .HasForeignKey(ctq => ctq.ProblemId);

            modelBuilder.Entity<CodingTestAttempt>()
                .HasOne(cta => cta.CodingTest)
                .WithMany(ct => ct.Attempts)
                .HasForeignKey(cta => cta.CodingTestId);

            modelBuilder.Entity<CodingTestQuestionAttempt>()
                .HasOne(ctqa => ctqa.CodingTestAttempt)
                .WithMany(cta => cta.QuestionAttempts)
                .HasForeignKey(ctqa => ctqa.CodingTestAttemptId);

            modelBuilder.Entity<CodingTestQuestionAttempt>()
                .HasOne(ctqa => ctqa.CodingTestQuestion)
                .WithMany()
                .HasForeignKey(ctqa => ctqa.CodingTestQuestionId);

            modelBuilder.Entity<CodingTestTopicData>()
                .HasOne(cttd => cttd.CodingTest)
                .WithMany(ct => ct.TopicData)
                .HasForeignKey(cttd => cttd.CodingTestId);

            // AssignedCodingTest relationships
            modelBuilder.Entity<AssignedCodingTest>()
                .HasOne(act => act.CodingTest)
                .WithMany()
                .HasForeignKey(act => act.CodingTestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure timestamps for coding test entities
            modelBuilder.Entity<CodingTest>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<CodingTestQuestion>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<CodingTestAttempt>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<CodingTestQuestionAttempt>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<CodingTestTopicData>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<AssignedCodingTest>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<AssignedCodingTest>()
                .Property(e => e.AssignedDate)
                .HasDefaultValueSql("GETDATE()");

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

            // CodingTestSubmission relationships
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
                .HasForeignKey(s => s.CodingTestQuestionAttemptId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CodingTestSubmission>()
                .HasOne(s => s.Problem)
                .WithMany()
                .HasForeignKey(s => s.ProblemId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CodingTestSubmission>()
                .HasMany(s => s.SubmissionResults)
                .WithOne(r => r.Submission)
                .HasForeignKey(r => r.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CodingTestSubmissionResult>()
                .HasOne(r => r.TestCase)
                .WithMany()
                .HasForeignKey(r => r.TestCaseId);

            modelBuilder.Entity<CodingTestSubmissionResult>()
                .HasOne(r => r.Problem)
                .WithMany()
                .HasForeignKey(r => r.ProblemId);

            // Domain and Subdomain table mappings and relationships
            modelBuilder.Entity<Domain>().ToTable("Domain");
            modelBuilder.Entity<Domain>()
                .Property(d => d.DomainName)
                .HasColumnName("Domain");
                
            modelBuilder.Entity<Subdomain>().ToTable("Subdomain");
            modelBuilder.Entity<Subdomain>()
                .Property(s => s.SubdomainName)
                .HasColumnName("Subdomain");
            
            modelBuilder.Entity<Subdomain>()
                .HasOne(s => s.Domain)
                .WithMany(d => d.Subdomains)
                .HasForeignKey(s => s.DomainId);

            // Difficulty table mapping
            modelBuilder.Entity<Difficulty>().ToTable("Difficulty");
            modelBuilder.Entity<Difficulty>()
                .Property(d => d.DifficultyName)
                .HasColumnName("Difficulty");

            // Configure Problem table column mappings
            modelBuilder.Entity<Problem>().ToTable("Problems");
            modelBuilder.Entity<Problem>()
                .Property(p => p.Hints)
                .HasColumnName("Hints")
                .HasColumnType("int");
            modelBuilder.Entity<Problem>()
                .Property(p => p.TimeLimit)
                .HasColumnName("TimeLimit")
                .HasColumnType("int");
            modelBuilder.Entity<Problem>()
                .Property(p => p.MemoryLimit)
                .HasColumnName("MemoryLimit")
                .HasColumnType("int");
            modelBuilder.Entity<Problem>()
                .Property(p => p.SubdomainId)
                .HasColumnName("SubdomainId")
                .HasColumnType("int");
            modelBuilder.Entity<Problem>()
                .Property(p => p.Difficulty)
                .HasColumnName("Difficulty")
                .HasColumnType("int");

            // Configure StarterCode table column mappings
            modelBuilder.Entity<StarterCode>().ToTable("StarterCodes");
            modelBuilder.Entity<StarterCode>()
                .Property(sc => sc.ProblemId)
                .HasColumnName("ProblemId");
            modelBuilder.Entity<StarterCode>()
                .Property(sc => sc.Language)
                .HasColumnName("Language")
                .HasColumnType("int");
            modelBuilder.Entity<StarterCode>()
                .Property(sc => sc.Code)
                .HasColumnName("Code");


            // Note: Language seed data removed to avoid ID conflicts
            // Languages will be created through the API endpoints

            // Seed Difficulty data
            modelBuilder.Entity<Difficulty>().HasData(
                new Difficulty { Id = 1, DifficultyId = 1, DifficultyName = "Easy" },
                new Difficulty { Id = 2, DifficultyId = 2, DifficultyName = "Medium" },
                new Difficulty { Id = 3, DifficultyId = 3, DifficultyName = "Hard" }
            );

            // Remove old seeding for Problems.StarterCode
            modelBuilder.Entity<Problem>().HasData(
                new Problem
                {
                    Id = 1,
                    Title = "Two Sum",
                    Description = "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target. You may assume that each input would have exactly one solution, and you may not use the same element twice. You can return the answer in any order.",
                    Examples = "Input: nums = [2,7,11,15], target = 9\nOutput: [0,1]\nExplanation: Because nums[0] + nums[1] == 9, we return [0, 1].",
                    Constraints = "2 ≤ nums.length ≤ 10⁴\n-10⁹ ≤ nums[i] ≤ 10⁹\n-10⁹ ≤ target ≤ 10⁹\nOnly one valid answer exists.",
                    TimeLimit = 5,
                    MemoryLimit = 256,
                    SubdomainId = 9,
                    Difficulty = 1,
                    TestCases = new List<TestCase>(),
                    StarterCodes = new List<StarterCode>()
                },
                new Problem
                {
                    Id = 2,
                    Title = "Reverse String",
                    Description = "Write a function that reverses a string. The input string is given as an array of characters s.",
                    Examples = "Input: s = [\"h\",\"e\",\"l\",\"l\",\"o\"]\nOutput: [\"o\",\"l\",\"l\",\"e\",\"h\"]",
                    Constraints = "1 ≤ s.length ≤ 10⁵",
                    TimeLimit = 3,
                    MemoryLimit = 128,
                    SubdomainId = 9,
                    Difficulty = 1,
                    TestCases = new List<TestCase>(),
                    StarterCodes = new List<StarterCode>()
                }
            );
        }
    }
}