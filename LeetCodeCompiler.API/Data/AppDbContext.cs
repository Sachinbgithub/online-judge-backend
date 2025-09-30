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
        
        // Domain and Subdomain DbSets
        public DbSet<Domain> Domains { get; set; }
        public DbSet<Subdomain> Subdomains { get; set; }
        
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
                .HasOne<Problem>()
                .WithMany(p => p.StarterCodes)
                .HasForeignKey(sc => sc.ProblemId);

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

            // Configure Problem table column mappings
            modelBuilder.Entity<Problem>().ToTable("Problems");
            modelBuilder.Entity<Problem>()
                .Property(p => p.Hints)
                .HasColumnName("Hints");
            modelBuilder.Entity<Problem>()
                .Property(p => p.TimeLimit)
                .HasColumnName("TimeLimit");
            modelBuilder.Entity<Problem>()
                .Property(p => p.MemoryLimit)
                .HasColumnName("MemoryLimit");
            modelBuilder.Entity<Problem>()
                .Property(p => p.SubdomainId)
                .HasColumnName("SubdomainId");
            modelBuilder.Entity<Problem>()
                .Property(p => p.Difficulty)
                .HasColumnName("Difficulty");

            // Remove old seeding for Problems.StarterCode
            modelBuilder.Entity<Problem>().HasData(
                new Problem
                {
                    Id = 1,
                    Title = "Two Sum",
                    Description = "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target. You may assume that each input would have exactly one solution, and you may not use the same element twice. You can return the answer in any order.",
                    Examples = "Input: nums = [2,7,11,15], target = 9\nOutput: [0,1]\nExplanation: Because nums[0] + nums[1] == 9, we return [0, 1].",
                    Constraints = "2 ≤ nums.length ≤ 10⁴\n-10⁹ ≤ nums[i] ≤ 10⁹\n-10⁹ ≤ target ≤ 10⁹\nOnly one valid answer exists.",
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
                    TestCases = new List<TestCase>(),
                    StarterCodes = new List<StarterCode>()
                }
            );
        }
    }
}