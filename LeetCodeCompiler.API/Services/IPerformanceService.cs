using System.Collections.Generic;
using System.Threading.Tasks;
using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public interface IPerformanceService
    {
        Task<StudentOverviewResponse> GetStudentOverviewAsync(long userId);
        Task<PagedResult<CodingTestHistoryItem>> GetCodingTestHistoryAsync(long userId, int pageNumber, int pageSize);
        Task<PagedResult<PracticeTestHistoryItem>> GetPracticeTestHistoryAsync(long userId, int pageNumber, int pageSize);
        Task<FreePracticeSummaryResponse> GetFreePracticeSummaryAsync(long userId);
        
        Task<PagedResult<FacultyStudentResultItem>> GetStudentsForTestAsync(int codingTestId, int pageNumber, int pageSize);
        Task<PagedResult<LeaderboardItem>> GetLeaderboardAsync(int codingTestId, int pageNumber, int pageSize);
        Task<PagedResult<PracticeStudentResultItem>> GetPracticeStudentsAsync(int practiceTestId, int pageNumber, int pageSize);
        Task<PagedResult<ProblemAnalysisItem>> GetProblemAnalysisAsync(int codingTestId, int pageNumber, int pageSize);

        // === Group 1: Browse APIs ===
        Task<PagedResult<FacultyTestSummaryItem>> GetTestsByFacultyAsync(long facultyId, int pageNumber, int pageSize);
        Task<PagedResult<ClassStudentSummaryItem>> GetStudentSummaryByClassAsync(int classId, int pageNumber, int pageSize);
        Task<PagedResult<CollegeClassSummaryItem>> GetClassSummaryByCollegeAsync(int collegeId, int pageNumber, int pageSize);
        Task<PagedResult<StudentTestHistoryItem>> GetStudentTestHistoryForFacultyAsync(long studentId, long facultyId, int pageNumber, int pageSize);
        Task<TestCompletionStatusResponse> GetTestCompletionByClassAsync(int codingTestId, int classId);

        // === Group 2: User Performance by UserId (faculty view) ===
        Task<UserFullPerformanceResponse> GetUserFullPerformanceAsync(long userId);
        Task<PagedResult<UserCodingTestSummaryItem>> GetUserCodingTestHistoryAsync(long userId, int pageNumber, int pageSize);
        Task<PagedResult<UserPracticeTestSummaryItem>> GetUserPracticeTestHistoryAsync(long userId, int pageNumber, int pageSize);
        Task<UserPracticeTestDetailResponse> GetUserPracticeTestDetailAsync(long userId, int practiceTestId, int? attemptNumber);
        Task<UserFreePracticeSummaryResponse> GetUserFreePracticeSummaryAsync(long userId);
    }
}
