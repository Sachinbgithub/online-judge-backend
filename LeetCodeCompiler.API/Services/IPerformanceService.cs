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
    }
}
