using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ProblemsController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetProblems()
        {
            var problems = await _context.Problems
                .Include(p => p.TestCases)
                .Include(p => p.StarterCodes)
                .ToListAsync();
            
            return Ok(problems);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProblem(int id)
        {
            var problem = await _context.Problems
                .Include(p => p.TestCases)
                .Include(p => p.StarterCodes)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            return problem == null ? NotFound() : Ok(problem);
        }

        /// <summary>
        /// Get all problems by subdomain ID
        /// </summary>
        /// <param name="subdomainId">Subdomain ID</param>
        /// <returns>List of problems for the specified subdomain</returns>
        [HttpGet("subdomain/{subdomainId}")]
        public async Task<IActionResult> GetProblemsBySubdomainId(int subdomainId)
        {
            try
            {
                var problems = await _context.Problems
                    .Include(p => p.TestCases)
                    .Include(p => p.StarterCodes)
                    .Where(p => p.SubdomainId == subdomainId)
                    .ToListAsync();
                
                return Ok(problems);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving problems: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a new problem
        /// </summary>
        /// <param name="request">Problem creation request</param>
        /// <returns>Created problem</returns>
        [HttpPost]
        public async Task<IActionResult> CreateProblem([FromBody] CreateProblemRequest request)
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

                // Check if subdomain exists
                var subdomain = await _context.Subdomains
                    .Include(s => s.Domain)
                    .FirstOrDefaultAsync(s => s.SubdomainId == request.SubdomainId);
                
                if (subdomain == null)
                {
                    return NotFound(new { error = $"Subdomain with ID {request.SubdomainId} not found" });
                }

                // Check if difficulty exists (1-3 range)
                if (request.Difficulty < 1 || request.Difficulty > 3)
                {
                    return BadRequest(new { error = "Difficulty must be between 1 and 3" });
                }

                // Check if problem with same title already exists in the same subdomain
                var existingProblem = await _context.Problems
                    .FirstOrDefaultAsync(p => p.SubdomainId == request.SubdomainId && 
                                            p.Title.ToLower() == request.Title.ToLower());
                
                if (existingProblem != null)
                {
                    return Conflict(new { error = $"Problem with title '{request.Title}' already exists in subdomain '{subdomain.SubdomainName}'" });
                }

                // Create new problem
                var newProblem = new Problem
                {
                    Title = request.Title.Trim(),
                    Description = request.Description.Trim(),
                    Examples = request.Examples.Trim(),
                    Constraints = request.Constraints.Trim(),
                    Hints = request.Hints,
                    TimeLimit = request.TimeLimit,
                    MemoryLimit = request.MemoryLimit,
                    SubdomainId = request.SubdomainId,
                    Difficulty = request.Difficulty,
                    StreamId = request.StreamId,
                    CreatedByUserId = request.CreatedByUserId,
                    UpdatedByUserId = request.UpdatedByUserId
                };

                _context.Problems.Add(newProblem);
                await _context.SaveChangesAsync();

                // Return the created problem with subdomain and domain information
                var createdProblem = new CreateProblemResponse
                {
                    Id = newProblem.Id,
                    Title = newProblem.Title,
                    Description = newProblem.Description,
                    Examples = newProblem.Examples,
                    Constraints = newProblem.Constraints,
                    Hints = newProblem.Hints,
                    TimeLimit = newProblem.TimeLimit,
                    MemoryLimit = newProblem.MemoryLimit,
                    SubdomainId = newProblem.SubdomainId,
                    Difficulty = newProblem.Difficulty,
                    StreamId = newProblem.StreamId,
                    CreatedByUserId = newProblem.CreatedByUserId,
                    UpdatedByUserId = newProblem.UpdatedByUserId,
                    SubdomainName = subdomain.SubdomainName,
                    DomainName = subdomain.Domain?.DomainName ?? "",
                    TestCases = new List<TestCase>(),
                    StarterCodes = new List<StarterCode>()
                };

                return CreatedAtAction(nameof(GetProblem), new { id = newProblem.Id }, createdProblem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the problem", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing problem
        /// </summary>
        /// <param name="id">Problem ID</param>
        /// <param name="request">Problem update request</param>
        /// <returns>Updated problem</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProblem(int id, [FromBody] UpdateProblemRequest request)
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

                // Find the problem
                var problem = await _context.Problems
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (problem == null)
                {
                    return NotFound(new { error = $"Problem with ID {id} not found" });
                }

                // Check if subdomain exists
                var subdomain = await _context.Subdomains
                    .Include(s => s.Domain)
                    .FirstOrDefaultAsync(s => s.SubdomainId == request.SubdomainId);
                
                if (subdomain == null)
                {
                    return NotFound(new { error = $"Subdomain with ID {request.SubdomainId} not found" });
                }

                // Check if difficulty exists (1-3 range)
                if (request.Difficulty < 1 || request.Difficulty > 3)
                {
                    return BadRequest(new { error = "Difficulty must be between 1 and 3" });
                }

                // Check if another problem with the same title exists in the same subdomain (case-insensitive)
                var existingProblem = await _context.Problems
                    .FirstOrDefaultAsync(p => p.Id != id && 
                                            p.SubdomainId == request.SubdomainId && 
                                            p.Title.ToLower() == request.Title.ToLower());

                if (existingProblem != null)
                {
                    return Conflict(new { error = $"Problem with title '{request.Title}' already exists in subdomain '{subdomain.SubdomainName}'" });
                }

                // Update the problem
                problem.Title = request.Title.Trim();
                problem.Description = request.Description.Trim();
                problem.Examples = request.Examples.Trim();
                problem.Constraints = request.Constraints.Trim();
                problem.Hints = request.Hints;
                problem.TimeLimit = request.TimeLimit;
                problem.MemoryLimit = request.MemoryLimit;
                problem.SubdomainId = request.SubdomainId;
                problem.Difficulty = request.Difficulty;
                problem.StreamId = request.StreamId;
                problem.CreatedByUserId = request.CreatedByUserId;
                problem.UpdatedByUserId = request.UpdatedByUserId;

                await _context.SaveChangesAsync();

                // Get test cases and starter codes for response
                var testCases = await _context.TestCases
                    .Where(tc => tc.ProblemId == id)
                    .ToListAsync();

                var starterCodes = await _context.StarterCodes
                    .Where(sc => sc.ProblemId == id)
                    .ToListAsync();

                // Return the updated problem
                var updatedProblem = new CreateProblemResponse
                {
                    Id = problem.Id,
                    Title = problem.Title,
                    Description = problem.Description,
                    Examples = problem.Examples,
                    Constraints = problem.Constraints,
                    Hints = problem.Hints,
                    TimeLimit = problem.TimeLimit,
                    MemoryLimit = problem.MemoryLimit,
                    SubdomainId = problem.SubdomainId,
                    Difficulty = problem.Difficulty,
                    StreamId = problem.StreamId,
                    CreatedByUserId = problem.CreatedByUserId,
                    UpdatedByUserId = problem.UpdatedByUserId,
                    SubdomainName = subdomain.SubdomainName,
                    DomainName = subdomain.Domain?.DomainName ?? "",
                    TestCases = testCases,
                    StarterCodes = starterCodes
                };

                return Ok(updatedProblem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the problem", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a problem
        /// </summary>
        /// <param name="id">Problem ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProblem(int id)
        {
            try
            {
                var problem = await _context.Problems
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (problem == null)
                {
                    return NotFound(new { error = $"Problem with ID {id} not found" });
                }

                // Get related data counts for information
                var testCasesCount = await _context.TestCases
                    .CountAsync(tc => tc.ProblemId == id);

                var starterCodesCount = await _context.StarterCodes
                    .CountAsync(sc => sc.ProblemId == id);

                var problemHintsCount = await _context.ProblemHints
                    .CountAsync(ph => ph.ProblemId == id);

                // Delete the problem (cascade will handle related data)
                _context.Problems.Remove(problem);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = $"Problem '{problem.Title}' has been deleted successfully",
                    deletedData = new {
                        testCases = testCasesCount,
                        starterCodes = starterCodesCount,
                        problemHints = problemHintsCount
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the problem", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all problems by stream ID
        /// </summary>
        /// <param name="streamId">Stream ID (optional - omit parameter or pass null to search for NULL streamId)</param>
        /// <returns>List of problems with the specified stream ID</returns>
        [HttpGet("stream")]
        public async Task<IActionResult> GetProblemsByStreamId([FromQuery] int? streamId)
        {
            try
            {
                IQueryable<Problem> query = _context.Problems
                    .Include(p => p.TestCases)
                    .Include(p => p.StarterCodes);

                // If streamId parameter is not provided or is explicitly null, search for NULL values
                if (streamId == null)
                {
                    query = query.Where(p => p.StreamId == null);
                }
                else
                {
                    query = query.Where(p => p.StreamId == streamId);
                }

                var problems = await query.ToListAsync();
                
                return Ok(problems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving problems by stream ID", details = ex.Message });
            }
        }

        /// <summary>
        /// Update stream ID for a specific problem
        /// </summary>
        /// <param name="id">Problem ID</param>
        /// <param name="streamId">New stream ID (can be null)</param>
        /// <returns>Updated problem</returns>
        [HttpPut("{id}/stream")]
        public async Task<IActionResult> UpdateProblemStreamId(int id, [FromBody] int? streamId)
        {
            try
            {
                var problem = await _context.Problems
                    .Include(p => p.TestCases)
                    .Include(p => p.StarterCodes)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (problem == null)
                {
                    return NotFound(new { error = $"Problem with ID {id} not found" });
                }

                // Update stream ID
                problem.StreamId = streamId;
                await _context.SaveChangesAsync();

                // Return the updated problem
                return Ok(problem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating problem stream ID", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all problems by created by user ID
        /// </summary>
        /// <param name="createdByUserId">Created by user ID (optional - omit parameter or pass null to search for NULL createdByUserId)</param>
        /// <returns>List of problems with the specified created by user ID</returns>
        [HttpGet("created-by")]
        public async Task<IActionResult> GetProblemsByCreatedByUserId([FromQuery] int? createdByUserId)
        {
            try
            {
                IQueryable<Problem> query = _context.Problems
                    .Include(p => p.TestCases)
                    .Include(p => p.StarterCodes);

                // If createdByUserId parameter is not provided or is explicitly null, search for NULL values
                if (createdByUserId == null)
                {
                    query = query.Where(p => p.CreatedByUserId == null);
                }
                else
                {
                    query = query.Where(p => p.CreatedByUserId == createdByUserId);
                }

                var problems = await query.ToListAsync();
                
                return Ok(problems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving problems by created by user ID", details = ex.Message });
            }
        }

        /// <summary>
        /// Update created by user ID for a specific problem
        /// </summary>
        /// <param name="id">Problem ID</param>
        /// <param name="createdByUserId">New created by user ID (can be null)</param>
        /// <returns>Updated problem</returns>
        [HttpPut("{id}/created-by")]
        public async Task<IActionResult> UpdateProblemCreatedByUserId(int id, [FromBody] int? createdByUserId)
        {
            try
            {
                var problem = await _context.Problems
                    .Include(p => p.TestCases)
                    .Include(p => p.StarterCodes)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (problem == null)
                {
                    return NotFound(new { error = $"Problem with ID {id} not found" });
                }

                // Update created by user ID
                problem.CreatedByUserId = createdByUserId;
                await _context.SaveChangesAsync();

                // Return the updated problem
                return Ok(problem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating problem created by user ID", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all problems by updated by user ID
        /// </summary>
        /// <param name="updatedByUserId">Updated by user ID (optional - omit parameter or pass null to search for NULL updatedByUserId)</param>
        /// <returns>List of problems with the specified updated by user ID</returns>
        [HttpGet("updated-by")]
        public async Task<IActionResult> GetProblemsByUpdatedByUserId([FromQuery] int? updatedByUserId)
        {
            try
            {
                IQueryable<Problem> query = _context.Problems
                    .Include(p => p.TestCases)
                    .Include(p => p.StarterCodes);

                // If updatedByUserId parameter is not provided or is explicitly null, search for NULL values
                if (updatedByUserId == null)
                {
                    query = query.Where(p => p.UpdatedByUserId == null);
                }
                else
                {
                    query = query.Where(p => p.UpdatedByUserId == updatedByUserId);
                }

                var problems = await query.ToListAsync();
                
                return Ok(problems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving problems by updated by user ID", details = ex.Message });
            }
        }

        /// <summary>
        /// Update updated by user ID for a specific problem
        /// </summary>
        /// <param name="id">Problem ID</param>
        /// <param name="updatedByUserId">New updated by user ID (can be null)</param>
        /// <returns>Updated problem</returns>
        [HttpPut("{id}/updated-by")]
        public async Task<IActionResult> UpdateProblemUpdatedByUserId(int id, [FromBody] int? updatedByUserId)
        {
            try
            {
                var problem = await _context.Problems
                    .Include(p => p.TestCases)
                    .Include(p => p.StarterCodes)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (problem == null)
                {
                    return NotFound(new { error = $"Problem with ID {id} not found" });
                }

                // Update updated by user ID
                problem.UpdatedByUserId = updatedByUserId;
                await _context.SaveChangesAsync();

                // Return the updated problem
                return Ok(problem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating problem updated by user ID", details = ex.Message });
            }
        }

        /// <summary>
        /// Get problem usage statistics
        /// </summary>
        /// <param name="id">Problem ID</param>
        /// <returns>Problem usage statistics</returns>
        [HttpGet("{id}/usage")]
        public async Task<IActionResult> GetProblemUsage(int id)
        {
            try
            {
                var problem = await _context.Problems
                    .Include(p => _context.Subdomains.Where(s => s.SubdomainId == p.SubdomainId))
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (problem == null)
                {
                    return NotFound(new { error = $"Problem with ID {id} not found" });
                }

                var testCasesCount = await _context.TestCases
                    .CountAsync(tc => tc.ProblemId == id);

                var starterCodesCount = await _context.StarterCodes
                    .CountAsync(sc => sc.ProblemId == id);

                var problemHintsCount = await _context.ProblemHints
                    .CountAsync(ph => ph.ProblemId == id);

                // Get subdomain and domain info
                var subdomain = await _context.Subdomains
                    .Include(s => s.Domain)
                    .FirstOrDefaultAsync(s => s.SubdomainId == problem.SubdomainId);

                var usageStats = new
                {
                    ProblemId = problem.Id,
                    Title = problem.Title,
                    SubdomainId = problem.SubdomainId,
                    SubdomainName = subdomain?.SubdomainName ?? "Unknown",
                    DomainId = subdomain?.DomainId ?? 0,
                    DomainName = subdomain?.Domain?.DomainName ?? "Unknown",
                    TestCasesCount = testCasesCount,
                    StarterCodesCount = starterCodesCount,
                    ProblemHintsCount = problemHintsCount,
                    Difficulty = problem.Difficulty,
                    TimeLimit = problem.TimeLimit,
                    MemoryLimit = problem.MemoryLimit,
                    IsUsed = testCasesCount > 0 || starterCodesCount > 0 || problemHintsCount > 0
                };

                return Ok(usageStats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving problem usage statistics", details = ex.Message });
            }
        }
    }
}