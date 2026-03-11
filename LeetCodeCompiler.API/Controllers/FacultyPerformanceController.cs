using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Services;
using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacultyPerformanceController : ControllerBase
    {
        private readonly IPerformanceService _performanceService;

        public FacultyPerformanceController(IPerformanceService performanceService)
        {
            _performanceService = performanceService;
        }

        [HttpGet("{facultyId}/tests")]
        public async Task<IActionResult> GetTestsByFaculty(long facultyId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _performanceService.GetTestsByFacultyAsync(facultyId, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("class/{classId}/students")]
        public async Task<IActionResult> GetStudentSummaryByClass(int classId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _performanceService.GetStudentSummaryByClassAsync(classId, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("college/{collegeId}/classes")]
        public async Task<IActionResult> GetClassSummaryByCollege(int collegeId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _performanceService.GetClassSummaryByCollegeAsync(collegeId, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("student/{studentId}/history")]
        public async Task<IActionResult> GetStudentTestHistoryForFaculty(long studentId, [FromQuery] long facultyId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _performanceService.GetStudentTestHistoryForFacultyAsync(studentId, facultyId, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{codingTestId}/class/{classId}/completion")]
        public async Task<IActionResult> GetTestCompletionByClass(int codingTestId, int classId)
        {
            var result = await _performanceService.GetTestCompletionByClassAsync(codingTestId, classId);
            return Ok(result);
        }
    }
}
