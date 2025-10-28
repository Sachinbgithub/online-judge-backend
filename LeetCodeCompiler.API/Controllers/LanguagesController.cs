using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LanguagesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LanguagesController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all languages
        /// </summary>
        /// <returns>List of all languages</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllLanguages()
        {
            try
            {
                var languages = await _context.Languages
                    .Select(l => new CreateLanguageResponse
                    {
                        Id = l.Id,
                        LanguageName = l.LanguageName
                    })
                    .OrderBy(l => l.LanguageName)
                    .ToListAsync();

                return Ok(languages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving languages", details = ex.Message });
            }
        }

        /// <summary>
        /// Get language by ID
        /// </summary>
        /// <param name="id">Language ID</param>
        /// <returns>Language details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLanguageById(int id)
        {
            try
            {
                var language = await _context.Languages
                    .Where(l => l.Id == id)
                    .Select(l => new CreateLanguageResponse
                    {
                        Id = l.Id,
                        LanguageName = l.LanguageName
                    })
                    .FirstOrDefaultAsync();

                if (language == null)
                {
                    return NotFound(new { error = $"Language with ID {id} not found" });
                }

                return Ok(language);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the language", details = ex.Message });
            }
        }

        /// <summary>
        /// Get language by name
        /// </summary>
        /// <param name="name">Language name</param>
        /// <returns>Language details</returns>
        [HttpGet("name/{name}")]
        public async Task<IActionResult> GetLanguageByName(string name)
        {
            try
            {
                var language = await _context.Languages
                    .Where(l => l.LanguageName.ToLower() == name.ToLower())
                    .Select(l => new CreateLanguageResponse
                    {
                        Id = l.Id,
                        LanguageName = l.LanguageName
                    })
                    .FirstOrDefaultAsync();

                if (language == null)
                {
                    return NotFound(new { error = $"Language '{name}' not found" });
                }

                return Ok(language);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the language", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new language
        /// </summary>
        /// <param name="request">Language creation request</param>
        /// <returns>Created language</returns>
        [HttpPost]
        public async Task<IActionResult> CreateLanguage([FromBody] CreateLanguageRequest request)
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

                // Check if language already exists (case-insensitive)
                var existingLanguage = await _context.Languages
                    .FirstOrDefaultAsync(l => l.LanguageName.ToLower() == request.LanguageName.ToLower());

                if (existingLanguage != null)
                {
                    return Conflict(new { error = $"Language '{request.LanguageName}' already exists" });
                }

                // Get the next available ID
                var maxId = await _context.Languages
                    .MaxAsync(l => (int?)l.Id) ?? 0;
                var nextId = maxId + 1;

                // Create new language
                var newLanguage = new Language
                {
                    Id = nextId,
                    LanguageName = request.LanguageName.Trim()
                };

                _context.Languages.Add(newLanguage);
                await _context.SaveChangesAsync();

                // Return the created language
                var createdLanguage = new CreateLanguageResponse
                {
                    Id = newLanguage.Id,
                    LanguageName = newLanguage.LanguageName
                };

                return CreatedAtAction(nameof(GetLanguageById), new { id = newLanguage.Id }, createdLanguage);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the language", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing language
        /// </summary>
        /// <param name="id">Language ID</param>
        /// <param name="request">Language update request</param>
        /// <returns>Updated language</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLanguage(int id, [FromBody] UpdateLanguageRequest request)
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

                // Find the language
                var language = await _context.Languages
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (language == null)
                {
                    return NotFound(new { error = $"Language with ID {id} not found" });
                }

                // Check if another language with the same name exists (case-insensitive)
                var existingLanguage = await _context.Languages
                    .FirstOrDefaultAsync(l => l.Id != id && l.LanguageName.ToLower() == request.LanguageName.ToLower());

                if (existingLanguage != null)
                {
                    return Conflict(new { error = $"Language '{request.LanguageName}' already exists" });
                }

                // Update the language
                language.LanguageName = request.LanguageName.Trim();

                await _context.SaveChangesAsync();

                // Return the updated language
                var updatedLanguage = new CreateLanguageResponse
                {
                    Id = language.Id,
                    LanguageName = language.LanguageName
                };

                return Ok(updatedLanguage);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the language", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a language
        /// </summary>
        /// <param name="id">Language ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLanguage(int id)
        {
            try
            {
                var language = await _context.Languages
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (language == null)
                {
                    return NotFound(new { error = $"Language with ID {id} not found" });
                }

                // Check if language is being used in StarterCodes
                var isUsedInStarterCodes = await _context.StarterCodes
                    .AnyAsync(sc => sc.Language == id);

                if (isUsedInStarterCodes)
                {
                    return Conflict(new { error = $"Cannot delete language '{language.LanguageName}' because it is being used in starter codes" });
                }

                _context.Languages.Remove(language);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Language '{language.LanguageName}' has been deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the language", details = ex.Message });
            }
        }

        /// <summary>
        /// Get language usage statistics
        /// </summary>
        /// <param name="id">Language ID</param>
        /// <returns>Language usage statistics</returns>
        [HttpGet("{id}/usage")]
        public async Task<IActionResult> GetLanguageUsage(int id)
        {
            try
            {
                var language = await _context.Languages
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (language == null)
                {
                    return NotFound(new { error = $"Language with ID {id} not found" });
                }

                var usageCount = await _context.StarterCodes
                    .CountAsync(sc => sc.Language == id);

                var problemsCount = await _context.StarterCodes
                    .Where(sc => sc.Language == id)
                    .Select(sc => sc.ProblemId)
                    .Distinct()
                    .CountAsync();

                var usageStats = new
                {
                    LanguageId = language.Id,
                    LanguageName = language.LanguageName,
                    StarterCodeCount = usageCount,
                    ProblemsCount = problemsCount,
                    IsUsed = usageCount > 0
                };

                return Ok(usageStats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving language usage statistics", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all languages with usage statistics
        /// </summary>
        /// <returns>List of all languages with their usage statistics</returns>
        [HttpGet("with-usage")]
        public async Task<IActionResult> GetAllLanguagesWithUsage()
        {
            try
            {
                var languagesWithUsage = await _context.Languages
                    .Select(l => new
                    {
                        Id = l.Id,
                        LanguageName = l.LanguageName,
                        StarterCodeCount = _context.StarterCodes.Count(sc => sc.Language == l.Id),
                        ProblemsCount = _context.StarterCodes
                            .Where(sc => sc.Language == l.Id)
                            .Select(sc => sc.ProblemId)
                            .Distinct()
                            .Count()
                    })
                    .OrderBy(l => l.LanguageName)
                    .ToListAsync();

                return Ok(languagesWithUsage);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving languages with usage statistics", details = ex.Message });
            }
        }
    }
}
