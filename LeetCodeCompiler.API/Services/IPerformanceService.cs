using System.Collections.Generic;
using System.Threading.Tasks;
using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public interface IPerformanceService
    {
        Task<StudentOverviewResponse> GetStudentOverviewAsync(long userId);
        Task<List<CodingTestHistoryItem>> GetCodingTestHistoryAsync(long userId);
        Task<List<PracticeTestHistoryItem>> GetPracticeTestHistoryAsync(long userId);
        Task<FreePracticeSummaryResponse> GetFreePracticeSummaryAsync(long userId);
        
        Task<List<FacultyStudentResultItem>> GetStudentsForTestAsync(int codingTestId);
        Task<List<LeaderboardItem>> GetLeaderboardAsync(int codingTestId);
        Task<List<PracticeStudentResultItem>> GetPracticeStudentsAsync(int practiceTestId);
        Task<List<ProblemAnalysisItem>> GetProblemAnalysisAsync(int codingTestId);
    }
}
