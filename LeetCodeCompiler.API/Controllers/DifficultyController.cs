using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DifficultyController : ControllerBase
    {
        private readonly AppDbContext _context;
        
        public DifficultyController(AppDbContext context) => _context = context;

        /// <summary>
        /// Get all difficulties
        /// </summary>
        /// <returns>List of all difficulties</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllDifficulties()
        {
            try
            {
                var difficulties = await _context.Difficulties
                    .Select(d => new DifficultyDto
                    {
                        Id = d.Id,
                        DifficultyId = d.DifficultyId,
                        DifficultyName = d.DifficultyName
                    })
                    .OrderBy(d => d.DifficultyId)
                    .ToListAsync();
                
                return Ok(difficulties);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving difficulties", details = ex.Message });
            }
        }

        /// <summary>
        /// Get difficulty by ID
        /// </summary>
        /// <param name="id">Difficulty ID</param>
        /// <returns>Difficulty details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDifficultyById(int id)
        {
            try
            {
                var difficulty = await _context.Difficulties
                    .Where(d => d.Id == id)
                    .Select(d => new DifficultyDto
                    {
                        Id = d.Id,
                        DifficultyId = d.DifficultyId,
                        DifficultyName = d.DifficultyName
                    })
                    .FirstOrDefaultAsync();
                
                if (difficulty == null)
                {
                    return NotFound(new { error = $"Difficulty with ID {id} not found" });
                }
                
                return Ok(difficulty);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the difficulty", details = ex.Message });
            }
        }

        /// <summary>
        /// Get difficulty by DifficultyId
        /// </summary>
        /// <param name="difficultyId">Difficulty ID (1-3)</param>
        /// <returns>Difficulty details</returns>
        [HttpGet("by-difficulty-id/{difficultyId}")]
        public async Task<IActionResult> GetDifficultyByDifficultyId(int difficultyId)
        {
            try
            {
                var difficulty = await _context.Difficulties
                    .Where(d => d.DifficultyId == difficultyId)
                    .Select(d => new DifficultyDto
                    {
                        Id = d.Id,
                        DifficultyId = d.DifficultyId,
                        DifficultyName = d.DifficultyName
                    })
                    .FirstOrDefaultAsync();
                
                if (difficulty == null)
                {
                    return NotFound(new { error = $"Difficulty with DifficultyId {difficultyId} not found" });
                }
                
                return Ok(difficulty);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the difficulty", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new difficulty
        /// </summary>
        /// <param name="request">Difficulty creation request</param>
        /// <returns>Created difficulty</returns>
        [HttpPost]
        public async Task<IActionResult> CreateDifficulty([FromBody] CreateDifficultyRequest request)
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

                // Check if difficulty with same DifficultyId already exists
                var existingDifficulty = await _context.Difficulties
                    .FirstOrDefaultAsync(d => d.DifficultyId == request.DifficultyId);
                
                if (existingDifficulty != null)
                {
                    return Conflict(new { error = $"Difficulty with DifficultyId {request.DifficultyId} already exists" });
                }

                // Check if difficulty with same name already exists (case-insensitive)
                var existingName = await _context.Difficulties
                    .FirstOrDefaultAsync(d => d.DifficultyName.ToLower() == request.DifficultyName.ToLower());
                
                if (existingName != null)
                {
                    return Conflict(new { error = $"Difficulty with name '{request.DifficultyName}' already exists" });
                }

                // Create new difficulty
                var newDifficulty = new Difficulty
                {
                    DifficultyId = request.DifficultyId,
                    DifficultyName = request.DifficultyName.Trim()
                };

                _context.Difficulties.Add(newDifficulty);
                await _context.SaveChangesAsync();

                // Return the created difficulty
                var createdDifficulty = new CreateDifficultyResponse
                {
                    Id = newDifficulty.Id,
                    DifficultyId = newDifficulty.DifficultyId,
                    DifficultyName = newDifficulty.DifficultyName
                };

                return CreatedAtAction(nameof(GetDifficultyById), new { id = newDifficulty.Id }, createdDifficulty);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the difficulty", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing difficulty
        /// </summary>
        /// <param name="id">Difficulty ID</param>
        /// <param name="request">Difficulty update request</param>
        /// <returns>Updated difficulty</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDifficulty(int id, [FromBody] UpdateDifficultyRequest request)
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

                // Find the difficulty
                var difficulty = await _context.Difficulties
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (difficulty == null)
                {
                    return NotFound(new { error = $"Difficulty with ID {id} not found" });
                }

                // Check if another difficulty with the same DifficultyId exists
                var existingDifficultyId = await _context.Difficulties
                    .FirstOrDefaultAsync(d => d.Id != id && d.DifficultyId == request.DifficultyId);

                if (existingDifficultyId != null)
                {
                    return Conflict(new { error = $"Difficulty with DifficultyId {request.DifficultyId} already exists" });
                }

                // Check if another difficulty with the same name exists (case-insensitive)
                var existingName = await _context.Difficulties
                    .FirstOrDefaultAsync(d => d.Id != id && d.DifficultyName.ToLower() == request.DifficultyName.ToLower());

                if (existingName != null)
                {
                    return Conflict(new { error = $"Difficulty with name '{request.DifficultyName}' already exists" });
                }

                // Update the difficulty
                difficulty.DifficultyId = request.DifficultyId;
                difficulty.DifficultyName = request.DifficultyName.Trim();

                await _context.SaveChangesAsync();

                // Return the updated difficulty
                var updatedDifficulty = new UpdateDifficultyResponse
                {
                    Id = difficulty.Id,
                    DifficultyId = difficulty.DifficultyId,
                    DifficultyName = difficulty.DifficultyName
                };

                return Ok(updatedDifficulty);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the difficulty", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a difficulty
        /// </summary>
        /// <param name="id">Difficulty ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDifficulty(int id)
        {
            try
            {
                var difficulty = await _context.Difficulties
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (difficulty == null)
                {
                    return NotFound(new { error = $"Difficulty with ID {id} not found" });
                }

                // Check if difficulty is being used by problems
                var problemsCount = await _context.Problems
                    .CountAsync(p => p.Difficulty == difficulty.DifficultyId);

                if (problemsCount > 0)
                {
                    return Conflict(new { error = $"Cannot delete difficulty '{difficulty.DifficultyName}' because it is being used by {problemsCount} problem(s). Please update or delete those problems first." });
                }

                _context.Difficulties.Remove(difficulty);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Difficulty '{difficulty.DifficultyName}' has been deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the difficulty", details = ex.Message });
            }
        }

        /// <summary>
        /// Get difficulty usage statistics
        /// </summary>
        /// <param name="id">Difficulty ID</param>
        /// <returns>Difficulty usage statistics</returns>
        [HttpGet("{id}/usage")]
        public async Task<IActionResult> GetDifficultyUsage(int id)
        {
            try
            {
                var difficulty = await _context.Difficulties
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (difficulty == null)
                {
                    return NotFound(new { error = $"Difficulty with ID {id} not found" });
                }

                var problemsCount = await _context.Problems
                    .CountAsync(p => p.Difficulty == difficulty.DifficultyId);

                var testCasesCount = await _context.TestCases
                    .Where(tc => _context.Problems
                        .Where(p => p.Difficulty == difficulty.DifficultyId)
                        .Select(p => p.Id)
                        .Contains(tc.ProblemId))
                    .CountAsync();

                var starterCodesCount = await _context.StarterCodes
                    .Where(sc => _context.Problems
                        .Where(p => p.Difficulty == difficulty.DifficultyId)
                        .Select(p => p.Id)
                        .Contains(sc.ProblemId))
                    .CountAsync();

                var usageStats = new
                {
                    Id = difficulty.Id,
                    DifficultyId = difficulty.DifficultyId,
                    DifficultyName = difficulty.DifficultyName,
                    ProblemsCount = problemsCount,
                    TestCasesCount = testCasesCount,
                    StarterCodesCount = starterCodesCount,
                    IsUsed = problemsCount > 0
                };

                return Ok(usageStats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving difficulty usage statistics", details = ex.Message });
            }
        }
    }
}
