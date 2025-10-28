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
                    Difficulty = request.Difficulty
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
    }
}