using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemHintsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProblemHintsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all problem hints
        /// </summary>
        /// <returns>List of all problem hints with problem information</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllProblemHints()
        {
            try
            {
                var problemHints = await _context.ProblemHints
                    .Select(ph => new CreateProblemHintResponse
                    {
                        Id = ph.Id,
                        ProblemId = ph.ProblemId,
                        Hint = ph.Hint,
                        ProblemTitle = _context.Problems
                            .Where(p => p.Id == ph.ProblemId)
                            .Select(p => p.Title)
                            .FirstOrDefault() ?? "Unknown"
                    })
                    .OrderBy(ph => ph.ProblemId)
                    .ThenBy(ph => ph.Id)
                    .ToListAsync();

                return Ok(problemHints);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving problem hints", details = ex.Message });
            }
        }

        /// <summary>
        /// Get problem hint by ID
        /// </summary>
        /// <param name="id">Problem hint ID</param>
        /// <returns>Problem hint details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProblemHintById(int id)
        {
            try
            {
                var problemHint = await _context.ProblemHints
                    .Where(ph => ph.Id == id)
                    .Select(ph => new CreateProblemHintResponse
                    {
                        Id = ph.Id,
                        ProblemId = ph.ProblemId,
                        Hint = ph.Hint,
                        ProblemTitle = _context.Problems
                            .Where(p => p.Id == ph.ProblemId)
                            .Select(p => p.Title)
                            .FirstOrDefault() ?? "Unknown"
                    })
                    .FirstOrDefaultAsync();

                if (problemHint == null)
                {
                    return NotFound(new { error = $"Problem hint with ID {id} not found" });
                }

                return Ok(problemHint);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the problem hint", details = ex.Message });
            }
        }

        /// <summary>
        /// Get problem hints by problem ID
        /// </summary>
        /// <param name="problemId">Problem ID</param>
        /// <returns>List of hints for the specified problem</returns>
        [HttpGet("problem/{problemId}")]
        public async Task<IActionResult> GetProblemHintsByProblemId(int problemId)
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

                var problemHints = await _context.ProblemHints
                    .Where(ph => ph.ProblemId == problemId)
                    .Select(ph => new CreateProblemHintResponse
                    {
                        Id = ph.Id,
                        ProblemId = ph.ProblemId,
                        Hint = ph.Hint,
                        ProblemTitle = _context.Problems
                            .Where(p => p.Id == ph.ProblemId)
                            .Select(p => p.Title)
                            .FirstOrDefault() ?? "Unknown"
                    })
                    .OrderBy(ph => ph.Id)
                    .ToListAsync();

                return Ok(problemHints);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving problem hints for the problem", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new problem hint
        /// </summary>
        /// <param name="request">Problem hint creation request</param>
        /// <returns>Created problem hint</returns>
        [HttpPost]
        public async Task<IActionResult> CreateProblemHint([FromBody] CreateProblemHintRequest request)
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

                // Create new problem hint
                var newProblemHint = new ProblemHint
                {
                    ProblemId = request.ProblemId,
                    Hint = request.Hint.Trim()
                };

                _context.ProblemHints.Add(newProblemHint);
                await _context.SaveChangesAsync();

                // Return the created problem hint with problem information
                var createdProblemHint = new CreateProblemHintResponse
                {
                    Id = newProblemHint.Id,
                    ProblemId = newProblemHint.ProblemId,
                    Hint = newProblemHint.Hint,
                    ProblemTitle = problem.Title
                };

                return CreatedAtAction(nameof(GetProblemHintById), new { id = newProblemHint.Id }, createdProblemHint);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the problem hint", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing problem hint
        /// </summary>
        /// <param name="id">Problem hint ID</param>
        /// <param name="request">Problem hint update request</param>
        /// <returns>Updated problem hint</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProblemHint(int id, [FromBody] UpdateProblemHintRequest request)
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

                // Find the problem hint
                var problemHint = await _context.ProblemHints
                    .FirstOrDefaultAsync(ph => ph.Id == id);

                if (problemHint == null)
                {
                    return NotFound(new { error = $"Problem hint with ID {id} not found" });
                }

                // Update the problem hint
                problemHint.Hint = request.Hint.Trim();

                await _context.SaveChangesAsync();

                // Get problem title for response
                var problemTitle = await _context.Problems
                    .Where(p => p.Id == problemHint.ProblemId)
                    .Select(p => p.Title)
                    .FirstOrDefaultAsync() ?? "Unknown";

                // Return the updated problem hint
                var updatedProblemHint = new CreateProblemHintResponse
                {
                    Id = problemHint.Id,
                    ProblemId = problemHint.ProblemId,
                    Hint = problemHint.Hint,
                    ProblemTitle = problemTitle
                };

                return Ok(updatedProblemHint);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the problem hint", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a problem hint
        /// </summary>
        /// <param name="id">Problem hint ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProblemHint(int id)
        {
            try
            {
                var problemHint = await _context.ProblemHints
                    .FirstOrDefaultAsync(ph => ph.Id == id);

                if (problemHint == null)
                {
                    return NotFound(new { error = $"Problem hint with ID {id} not found" });
                }

                _context.ProblemHints.Remove(problemHint);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Problem hint with ID {id} has been deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the problem hint", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete all problem hints for a specific problem
        /// </summary>
        /// <param name="problemId">Problem ID</param>
        /// <returns>Success message with count of deleted hints</returns>
        [HttpDelete("problem/{problemId}")]
        public async Task<IActionResult> DeleteProblemHintsByProblemId(int problemId)
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

                var problemHints = await _context.ProblemHints
                    .Where(ph => ph.ProblemId == problemId)
                    .ToListAsync();

                var count = problemHints.Count;

                if (count == 0)
                {
                    return Ok(new { message = $"No problem hints found for problem ID {problemId}" });
                }

                _context.ProblemHints.RemoveRange(problemHints);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"{count} problem hint(s) for problem ID {problemId} have been deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting problem hints for the problem", details = ex.Message });
            }
        }

        /// <summary>
        /// Get problem hints count for a specific problem
        /// </summary>
        /// <param name="problemId">Problem ID</param>
        /// <returns>Count of hints for the problem</returns>
        [HttpGet("problem/{problemId}/count")]
        public async Task<IActionResult> GetProblemHintsCount(int problemId)
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

                var count = await _context.ProblemHints
                    .CountAsync(ph => ph.ProblemId == problemId);

                var problemTitle = await _context.Problems
                    .Where(p => p.Id == problemId)
                    .Select(p => p.Title)
                    .FirstOrDefaultAsync() ?? "Unknown";

                return Ok(new
                {
                    ProblemId = problemId,
                    ProblemTitle = problemTitle,
                    HintsCount = count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving problem hints count", details = ex.Message });
            }
        }
    }
}
