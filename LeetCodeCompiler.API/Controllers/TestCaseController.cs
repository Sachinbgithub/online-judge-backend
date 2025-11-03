using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestCaseController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TestCaseController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all test cases
        /// </summary>
        /// <returns>List of all test cases with problem information</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllTestCases()
        {
            try
            {
                var testCases = await _context.TestCases
                    .Select(tc => new CreateTestCaseResponse
                    {
                        Id = tc.Id,
                        ProblemId = tc.ProblemId,
                        Input = tc.Input,
                        ExpectedOutput = tc.ExpectedOutput,
                        ProblemTitle = _context.Problems
                            .Where(p => p.Id == tc.ProblemId)
                            .Select(p => p.Title)
                            .FirstOrDefault() ?? "Unknown"
                    })
                    .ToListAsync();

                return Ok(testCases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving test cases", details = ex.Message });
            }
        }

        /// <summary>
        /// Get test case by ID
        /// </summary>
        /// <param name="id">Test case ID</param>
        /// <returns>Test case details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTestCaseById(int id)
        {
            try
            {
                var testCase = await _context.TestCases
                    .Where(tc => tc.Id == id)
                    .Select(tc => new CreateTestCaseResponse
                    {
                        Id = tc.Id,
                        ProblemId = tc.ProblemId,
                        Input = tc.Input,
                        ExpectedOutput = tc.ExpectedOutput,
                        ProblemTitle = _context.Problems
                            .Where(p => p.Id == tc.ProblemId)
                            .Select(p => p.Title)
                            .FirstOrDefault() ?? "Unknown"
                    })
                    .FirstOrDefaultAsync();

                if (testCase == null)
                {
                    return NotFound(new { error = $"Test case with ID {id} not found" });
                }

                return Ok(testCase);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the test case", details = ex.Message });
            }
        }

        /// <summary>
        /// Get test cases by problem ID
        /// </summary>
        /// <param name="problemId">Problem ID</param>
        /// <returns>List of test cases for the specified problem</returns>
        [HttpGet("problem/{problemId}")]
        public async Task<IActionResult> GetTestCasesByProblemId(int problemId)
        {
            try
            {
                // Check if problem exists
                var problemExists = await _context.Problems
                    .AnyAsync(p => p.Id == problemId);

                if (!problemExists)
                {
                    return NotFound(new { error = $"Problem with ID {problemId} not found" });
                }

                var testCases = await _context.TestCases
                    .Where(tc => tc.ProblemId == problemId)
                    .Select(tc => new CreateTestCaseResponse
                    {
                        Id = tc.Id,
                        ProblemId = tc.ProblemId,
                        Input = tc.Input,
                        ExpectedOutput = tc.ExpectedOutput,
                        ProblemTitle = _context.Problems
                            .Where(p => p.Id == tc.ProblemId)
                            .Select(p => p.Title)
                            .FirstOrDefault() ?? "Unknown"
                    })
                    .ToListAsync();

                return Ok(testCases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving test cases for the problem", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new test case
        /// </summary>
        /// <param name="request">Test case creation request</param>
        /// <returns>Created test case</returns>
        [HttpPost]
        public async Task<IActionResult> CreateTestCase([FromBody] CreateTestCaseRequest request)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { error = "Validation failed", details = errors });
                }

                // Check if problem exists
                var problem = await _context.Problems
                    .FirstOrDefaultAsync(p => p.Id == request.ProblemId);
                
                if (problem == null)
                {
                    return NotFound(new { error = $"Problem with ID {request.ProblemId} not found" });
                }

                // Create new test case
                var newTestCase = new TestCase
                {
                    ProblemId = request.ProblemId,
                    Input = request.Input,
                    ExpectedOutput = request.ExpectedOutput
                };

                _context.TestCases.Add(newTestCase);
                await _context.SaveChangesAsync();

                // Return the created test case with problem information
                var createdTestCase = new CreateTestCaseResponse
                {
                    Id = newTestCase.Id,
                    ProblemId = newTestCase.ProblemId,
                    Input = newTestCase.Input,
                    ExpectedOutput = newTestCase.ExpectedOutput,
                    ProblemTitle = problem.Title
                };

                return CreatedAtAction(nameof(GetTestCaseById), new { id = newTestCase.Id }, createdTestCase);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the test case", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing test case
        /// </summary>
        /// <param name="id">Test case ID</param>
        /// <param name="request">Test case update request</param>
        /// <returns>Updated test case</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTestCase(int id, [FromBody] UpdateTestCaseRequest request)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { error = "Validation failed", details = errors });
                }

                // Find the test case
                var testCase = await _context.TestCases
                    .FirstOrDefaultAsync(tc => tc.Id == id);

                if (testCase == null)
                {
                    return NotFound(new { error = $"Test case with ID {id} not found" });
                }

                // Update the test case
                testCase.Input = request.Input;
                testCase.ExpectedOutput = request.ExpectedOutput;

                await _context.SaveChangesAsync();

                // Get problem title for response
                var problemTitle = await _context.Problems
                    .Where(p => p.Id == testCase.ProblemId)
                    .Select(p => p.Title)
                    .FirstOrDefaultAsync() ?? "Unknown";

                // Return the updated test case
                var updatedTestCase = new CreateTestCaseResponse
                {
                    Id = testCase.Id,
                    ProblemId = testCase.ProblemId,
                    Input = testCase.Input,
                    ExpectedOutput = testCase.ExpectedOutput,
                    ProblemTitle = problemTitle
                };

                return Ok(updatedTestCase);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the test case", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a test case
        /// </summary>
        /// <param name="id">Test case ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTestCase(int id)
        {
            try
            {
                var testCase = await _context.TestCases
                    .FirstOrDefaultAsync(tc => tc.Id == id);

                if (testCase == null)
                {
                    return NotFound(new { error = $"Test case with ID {id} not found" });
                }

                _context.TestCases.Remove(testCase);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Test case with ID {id} has been deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the test case", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete all test cases for a specific problem
        /// </summary>
        /// <param name="problemId">Problem ID</param>
        /// <returns>Success message with count of deleted test cases</returns>
        [HttpDelete("problem/{problemId}")]
        public async Task<IActionResult> DeleteTestCasesByProblemId(int problemId)
        {
            try
            {
                // Check if problem exists
                var problemExists = await _context.Problems
                    .AnyAsync(p => p.Id == problemId);

                if (!problemExists)
                {
                    return NotFound(new { error = $"Problem with ID {problemId} not found" });
                }

                var testCases = await _context.TestCases
                    .Where(tc => tc.ProblemId == problemId)
                    .ToListAsync();

                var count = testCases.Count;

                if (count == 0)
                {
                    return Ok(new { message = $"No test cases found for problem ID {problemId}" });
                }

                _context.TestCases.RemoveRange(testCases);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"{count} test case(s) for problem ID {problemId} have been deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting test cases for the problem", details = ex.Message });
            }
        }
    }
}
