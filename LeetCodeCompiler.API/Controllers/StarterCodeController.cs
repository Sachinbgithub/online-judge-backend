using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StarterCodeController : ControllerBase
    {
        private readonly AppDbContext _context;
        
        public StarterCodeController(AppDbContext context) => _context = context;

        /// <summary>
        /// Check database schema for StarterCodes table
        /// </summary>
        /// <returns>Database schema information</returns>
        [HttpGet("schema")]
        public async Task<IActionResult> CheckDatabaseSchema()
        {
            try
            {
                // Try to get a sample record to see the data types
                var sampleRecord = await _context.StarterCodes.FirstOrDefaultAsync();
                
                if (sampleRecord == null)
                {
                    return Ok(new { message = "No records found in StarterCodes table" });
                }
                
                return Ok(new { 
                    message = "Sample record found",
                    sample = new {
                        Id = sampleRecord.Id,
                        ProblemId = sampleRecord.ProblemId,
                        Language = sampleRecord.Language,
                        LanguageType = sampleRecord.Language.GetType().Name,
                        Code = sampleRecord.Code?.Substring(0, Math.Min(50, sampleRecord.Code?.Length ?? 0))
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error checking database schema", details = ex.Message });
            }
        }

        [HttpGet("problem-schema")]
        public async Task<IActionResult> CheckProblemSchema()
        {
            try
            {
                // Try to get a sample problem record to see the data types
                var sampleProblem = await _context.Problems.FirstOrDefaultAsync();
                
                if (sampleProblem == null)
                {
                    return Ok(new { message = "No records found in Problems table" });
                }
                
                return Ok(new { 
                    message = "Sample problem found",
                    sample = new {
                        Id = sampleProblem.Id,
                        Title = sampleProblem.Title,
                        TimeLimit = sampleProblem.TimeLimit,
                        TimeLimitType = sampleProblem.TimeLimit.GetType().Name,
                        MemoryLimit = sampleProblem.MemoryLimit,
                        MemoryLimitType = sampleProblem.MemoryLimit.GetType().Name,
                        SubdomainId = sampleProblem.SubdomainId,
                        SubdomainIdType = sampleProblem.SubdomainId.GetType().Name,
                        Difficulty = sampleProblem.Difficulty,
                        DifficultyType = sampleProblem.Difficulty.GetType().Name,
                        Hints = sampleProblem.Hints,
                        HintsType = sampleProblem.Hints?.GetType().Name ?? "null"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error checking problem schema", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all starter codes
        /// </summary>
        /// <returns>List of all starter codes with problem information</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllStarterCodes()
        {
            var starterCodes = await _context.StarterCodes
                .Select(sc => new CreateStarterCodeResponse
                {
                    Id = sc.Id,
                    ProblemId = sc.ProblemId,
                    Language = sc.Language,
                    Code = sc.Code,
                    ProblemTitle = _context.Problems
                        .Where(p => p.Id == sc.ProblemId)
                        .Select(p => p.Title)
                        .FirstOrDefault() ?? ""
                })
                .ToListAsync();
            
            return Ok(starterCodes);
        }

        /// <summary>
        /// Get starter code by ID
        /// </summary>
        /// <param name="id">Starter code ID</param>
        /// <returns>Starter code with problem information</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStarterCodeById(int id)
        {
            var starterCode = await _context.StarterCodes
                .Where(sc => sc.Id == id)
                .Select(sc => new CreateStarterCodeResponse
                {
                    Id = sc.Id,
                    ProblemId = sc.ProblemId,
                    Language = sc.Language,
                    Code = sc.Code,
                    ProblemTitle = _context.Problems
                        .Where(p => p.Id == sc.ProblemId)
                        .Select(p => p.Title)
                        .FirstOrDefault() ?? ""
                })
                .FirstOrDefaultAsync();
            
            if (starterCode == null)
                return NotFound($"Starter code with ID {id} not found");
            
            return Ok(starterCode);
        }

        /// <summary>
        /// Get all starter codes for a specific problem
        /// </summary>
        /// <param name="problemId">Problem ID</param>
        /// <returns>List of starter codes for the specified problem</returns>
        [HttpGet("problem/{problemId}")]
        public async Task<IActionResult> GetStarterCodesByProblemId(int problemId)
        {
            var starterCodes = await _context.StarterCodes
                .Where(sc => sc.ProblemId == problemId)
                .Select(sc => new CreateStarterCodeResponse
                {
                    Id = sc.Id,
                    ProblemId = sc.ProblemId,
                    Language = sc.Language,
                    Code = sc.Code,
                    ProblemTitle = _context.Problems
                        .Where(p => p.Id == sc.ProblemId)
                        .Select(p => p.Title)
                        .FirstOrDefault() ?? ""
                })
                .ToListAsync();
            
            return Ok(starterCodes);
        }

        /// <summary>
        /// Create a new starter code
        /// </summary>
        /// <param name="request">Starter code creation request</param>
        /// <returns>Created starter code</returns>
        [HttpPost]
        public async Task<IActionResult> CreateStarterCode([FromBody] CreateStarterCodeRequest request)
        {
            try
            {
                // Debug logging
                Console.WriteLine($"CreateStarterCode called with ProblemId: {request?.ProblemId}, Language: {request?.Language}, Code: {request?.Code}");
                
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
                Console.WriteLine($"Checking if problem {request.ProblemId} exists...");
                var problem = await _context.Problems
                    .FirstOrDefaultAsync(p => p.Id == request.ProblemId);
                
                if (problem == null)
                {
                    Console.WriteLine($"Problem {request.ProblemId} not found");
                    return NotFound(new { error = $"Problem with ID {request.ProblemId} not found" });
                }
                Console.WriteLine($"Problem found: {problem.Title}");

                // Check if starter code already exists for this problem and language
                Console.WriteLine($"Checking for existing starter code with ProblemId: {request.ProblemId}, Language: {request.Language}");
                var existingStarterCode = await _context.StarterCodes
                    .FirstOrDefaultAsync(sc => sc.ProblemId == request.ProblemId && 
                                            sc.Language == request.Language);
                
                if (existingStarterCode != null)
                {
                    Console.WriteLine($"Starter code already exists for language {request.Language}");
                    return Conflict(new { error = $"Starter code for language {request.Language} already exists for problem '{problem?.Title ?? "Unknown"}'" });
                }
                Console.WriteLine($"No existing starter code found, proceeding with creation");

                // Create new starter code
                Console.WriteLine($"Creating new StarterCode object...");
                var newStarterCode = new StarterCode
                {
                    ProblemId = request.ProblemId,
                    Language = request.Language,
                    Code = request.Code?.Trim() ?? ""
                };
                Console.WriteLine($"StarterCode object created successfully");

                Console.WriteLine($"Adding to context...");
                _context.StarterCodes.Add(newStarterCode);
                
                Console.WriteLine($"Saving changes to database...");
                await _context.SaveChangesAsync();
                Console.WriteLine($"Changes saved successfully, new ID: {newStarterCode.Id}");

                // Return the created starter code with problem information
                var createdStarterCode = new CreateStarterCodeResponse
                {
                    Id = newStarterCode.Id,
                    ProblemId = newStarterCode.ProblemId,
                    Language = newStarterCode.Language,
                    Code = newStarterCode.Code,
                    ProblemTitle = problem?.Title ?? "Unknown"
                };

                return CreatedAtAction(nameof(GetStarterCodeById), new { id = newStarterCode.Id }, createdStarterCode);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the starter code", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing starter code
        /// </summary>
        /// <param name="id">Starter code ID</param>
        /// <param name="request">Update request</param>
        /// <returns>Updated starter code</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStarterCode(int id, [FromBody] UpdateStarterCodeRequest request)
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

                // Find existing starter code
                var existingStarterCode = await _context.StarterCodes
                    .FirstOrDefaultAsync(sc => sc.Id == id);
                
                if (existingStarterCode == null)
                {
                    return NotFound(new { error = $"Starter code with ID {id} not found" });
                }

                // Check if another starter code exists for the same problem and language (excluding current one)
                var duplicateStarterCode = await _context.StarterCodes
                    .FirstOrDefaultAsync(sc => sc.ProblemId == existingStarterCode.ProblemId && 
                                            sc.Language == request.Language && 
                                            sc.Id != id);
                
                if (duplicateStarterCode != null)
                {
                    return Conflict(new { error = $"Starter code for language {request.Language} already exists for this problem" });
                }

                // Update starter code
                existingStarterCode.Language = request.Language;
                existingStarterCode.Code = request.Code.Trim();

                await _context.SaveChangesAsync();

                // Return the updated starter code
                var updatedStarterCode = new CreateStarterCodeResponse
                {
                    Id = existingStarterCode.Id,
                    ProblemId = existingStarterCode.ProblemId,
                    Language = existingStarterCode.Language,
                    Code = existingStarterCode.Code,
                    ProblemTitle = _context.Problems
                        .Where(p => p.Id == existingStarterCode.ProblemId)
                        .Select(p => p.Title)
                        .FirstOrDefault() ?? ""
                };

                return Ok(updatedStarterCode);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the starter code", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a starter code
        /// </summary>
        /// <param name="id">Starter code ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStarterCode(int id)
        {
            try
            {
                var starterCode = await _context.StarterCodes.FindAsync(id);
                
                if (starterCode == null)
                {
                    return NotFound(new { error = $"Starter code with ID {id} not found" });
                }

                _context.StarterCodes.Remove(starterCode);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Starter code deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the starter code", details = ex.Message });
            }
        }
    }
}
