using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubdomainController : ControllerBase
    {
        private readonly AppDbContext _context;
        
        public SubdomainController(AppDbContext context) => _context = context;

        /// <summary>
        /// Get all subdomains
        /// </summary>
        /// <returns>List of all subdomains with their domain information</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllSubdomains()
        {
            var subdomains = await _context.Subdomains
                .Include(s => s.Domain)
                .Select(s => new SubdomainDto
                {
                    SubdomainId = s.SubdomainId,
                    DomainId = s.DomainId,
                    SubdomainName = s.SubdomainName,
                    StreamId = s.StreamId,
                    Domain = s.Domain != null ? new DomainBasicDto
                    {
                        DomainId = s.Domain.DomainId,
                        DomainName = s.Domain.DomainName,
                        StreamId = s.Domain.StreamId
                    } : null
                })
                .ToListAsync();
            
            return Ok(subdomains);
        }

        /// <summary>
        /// Get subdomain by ID
        /// </summary>
        /// <param name="id">Subdomain ID</param>
        /// <returns>Subdomain with domain information</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSubdomainById(int id)
        {
            var subdomain = await _context.Subdomains
                .Include(s => s.Domain)
                .Where(s => s.SubdomainId == id)
                .Select(s => new SubdomainDto
                {
                    SubdomainId = s.SubdomainId,
                    DomainId = s.DomainId,
                    SubdomainName = s.SubdomainName,
                    StreamId = s.StreamId,
                    Domain = s.Domain != null ? new DomainBasicDto
                    {
                        DomainId = s.Domain.DomainId,
                        DomainName = s.Domain.DomainName,
                        StreamId = s.Domain.StreamId
                    } : null
                })
                .FirstOrDefaultAsync();
            
            if (subdomain == null)
                return NotFound($"Subdomain with ID {id} not found");
            
            return Ok(subdomain);
        }

        /// <summary>
        /// Get all subdomains by domain ID
        /// </summary>
        /// <param name="domainId">Domain ID</param>
        /// <returns>List of subdomains for the specified domain</returns>
        [HttpGet("domain/{domainId}")]
        public async Task<IActionResult> GetSubdomainsByDomainId(int domainId)
        {
            var subdomains = await _context.Subdomains
                .Include(s => s.Domain)
                .Where(s => s.DomainId == domainId)
                .Select(s => new SubdomainDto
                {
                    SubdomainId = s.SubdomainId,
                    DomainId = s.DomainId,
                    SubdomainName = s.SubdomainName,
                    StreamId = s.StreamId,
                    Domain = s.Domain != null ? new DomainBasicDto
                    {
                        DomainId = s.Domain.DomainId,
                        DomainName = s.Domain.DomainName,
                        StreamId = s.Domain.StreamId
                    } : null
                })
                .ToListAsync();
            
            return Ok(subdomains);
        }

        /// <summary>
        /// Create a new subdomain
        /// </summary>
        /// <param name="request">Subdomain creation request</param>
        /// <returns>Created subdomain</returns>
        [HttpPost]
        public async Task<IActionResult> CreateSubdomain([FromBody] CreateSubdomainRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.SubdomainName))
                {
                    return BadRequest(new { error = "Subdomain name is required" });
                }

                if (request.DomainId <= 0)
                {
                    return BadRequest(new { error = "Valid DomainId is required" });
                }

                // Check if domain exists
                var domain = await _context.Domains
                    .FirstOrDefaultAsync(d => d.DomainId == request.DomainId);
                
                if (domain == null)
                {
                    return NotFound(new { error = $"Domain with ID {request.DomainId} not found" });
                }

                // Check if subdomain already exists within the same domain
                var existingSubdomain = await _context.Subdomains
                    .FirstOrDefaultAsync(s => s.DomainId == request.DomainId && 
                                            s.SubdomainName.ToLower() == request.SubdomainName.ToLower());
                
                if (existingSubdomain != null)
                {
                    return Conflict(new { error = $"Subdomain '{request.SubdomainName}' already exists in domain '{domain.DomainName}'" });
                }

                // Create new subdomain
                var newSubdomain = new Subdomain
                {
                    DomainId = request.DomainId,
                    SubdomainName = request.SubdomainName.Trim(),
                    StreamId = request.StreamId
                };

                _context.Subdomains.Add(newSubdomain);
                await _context.SaveChangesAsync();

                // Return the created subdomain with domain information
                var createdSubdomain = new SubdomainDto
                {
                    SubdomainId = newSubdomain.SubdomainId,
                    DomainId = newSubdomain.DomainId,
                    SubdomainName = newSubdomain.SubdomainName,
                    StreamId = newSubdomain.StreamId,
                    Domain = new DomainBasicDto
                    {
                        DomainId = domain.DomainId,
                        DomainName = domain.DomainName,
                        StreamId = domain.StreamId
                    }
                };

                return CreatedAtAction(nameof(GetSubdomainById), new { id = newSubdomain.SubdomainId }, createdSubdomain);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the subdomain", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing subdomain
        /// </summary>
        /// <param name="id">Subdomain ID</param>
        /// <param name="request">Subdomain update request</param>
        /// <returns>Updated subdomain</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubdomain(int id, [FromBody] UpdateSubdomainRequest request)
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

                // Find the subdomain
                var subdomain = await _context.Subdomains
                    .Include(s => s.Domain)
                    .FirstOrDefaultAsync(s => s.SubdomainId == id);

                if (subdomain == null)
                {
                    return NotFound(new { error = $"Subdomain with ID {id} not found" });
                }

                // Check if another subdomain with the same name exists within the same domain (case-insensitive)
                var existingSubdomain = await _context.Subdomains
                    .FirstOrDefaultAsync(s => s.SubdomainId != id && 
                                            s.DomainId == subdomain.DomainId && 
                                            s.SubdomainName.ToLower() == request.SubdomainName.ToLower());

                if (existingSubdomain != null)
                {
                    return Conflict(new { error = $"Subdomain '{request.SubdomainName}' already exists in domain '{subdomain.Domain?.DomainName}'" });
                }

                // Update the subdomain
                subdomain.SubdomainName = request.SubdomainName.Trim();
                subdomain.StreamId = request.StreamId;

                await _context.SaveChangesAsync();

                // Return the updated subdomain
                var updatedSubdomain = new SubdomainDto
                {
                    SubdomainId = subdomain.SubdomainId,
                    DomainId = subdomain.DomainId,
                    SubdomainName = subdomain.SubdomainName,
                    StreamId = subdomain.StreamId,
                    Domain = subdomain.Domain != null ? new DomainBasicDto
                    {
                        DomainId = subdomain.Domain.DomainId,
                        DomainName = subdomain.Domain.DomainName,
                        StreamId = subdomain.Domain.StreamId
                    } : null
                };

                return Ok(updatedSubdomain);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the subdomain", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a subdomain
        /// </summary>
        /// <param name="id">Subdomain ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubdomain(int id)
        {
            try
            {
                var subdomain = await _context.Subdomains
                    .Include(s => s.Domain)
                    .FirstOrDefaultAsync(s => s.SubdomainId == id);

                if (subdomain == null)
                {
                    return NotFound(new { error = $"Subdomain with ID {id} not found" });
                }

                // Check if subdomain has problems
                var hasProblems = await _context.Problems
                    .AnyAsync(p => p.SubdomainId == id);

                if (hasProblems)
                {
                    return Conflict(new { error = $"Cannot delete subdomain '{subdomain.SubdomainName}' because it has problems. Please delete all problems first." });
                }

                _context.Subdomains.Remove(subdomain);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Subdomain '{subdomain.SubdomainName}' has been deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the subdomain", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all subdomains by stream ID
        /// </summary>
        /// <param name="streamId">Stream ID (optional - omit parameter or pass null to search for NULL streamId)</param>
        /// <returns>List of subdomains with the specified stream ID</returns>
        [HttpGet("stream")]
        public async Task<IActionResult> GetSubdomainsByStreamId([FromQuery] int? streamId)
        {
            try
            {
                IQueryable<Subdomain> query = _context.Subdomains.Include(s => s.Domain);

                // If streamId parameter is not provided or is explicitly null, search for NULL values
                if (streamId == null)
                {
                    query = query.Where(s => s.StreamId == null);
                }
                else
                {
                    query = query.Where(s => s.StreamId == streamId);
                }

                var subdomains = await query
                    .Select(s => new SubdomainDto
                    {
                        SubdomainId = s.SubdomainId,
                        DomainId = s.DomainId,
                        SubdomainName = s.SubdomainName,
                        StreamId = s.StreamId,
                        Domain = s.Domain != null ? new DomainBasicDto
                        {
                            DomainId = s.Domain.DomainId,
                            DomainName = s.Domain.DomainName,
                            StreamId = s.Domain.StreamId
                        } : null
                    })
                    .ToListAsync();
                
                return Ok(subdomains);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving subdomains by stream ID", details = ex.Message });
            }
        }

        /// <summary>
        /// Update stream ID for a specific subdomain
        /// </summary>
        /// <param name="id">Subdomain ID</param>
        /// <param name="streamId">New stream ID (can be null)</param>
        /// <returns>Updated subdomain</returns>
        [HttpPut("{id}/stream")]
        public async Task<IActionResult> UpdateSubdomainStreamId(int id, [FromBody] int? streamId)
        {
            try
            {
                var subdomain = await _context.Subdomains
                    .Include(s => s.Domain)
                    .FirstOrDefaultAsync(s => s.SubdomainId == id);

                if (subdomain == null)
                {
                    return NotFound(new { error = $"Subdomain with ID {id} not found" });
                }

                // Update stream ID
                subdomain.StreamId = streamId;
                await _context.SaveChangesAsync();

                // Return the updated subdomain
                var updatedSubdomain = new SubdomainDto
                {
                    SubdomainId = subdomain.SubdomainId,
                    DomainId = subdomain.DomainId,
                    SubdomainName = subdomain.SubdomainName,
                    StreamId = subdomain.StreamId,
                    Domain = subdomain.Domain != null ? new DomainBasicDto
                    {
                        DomainId = subdomain.Domain.DomainId,
                        DomainName = subdomain.Domain.DomainName,
                        StreamId = subdomain.Domain.StreamId
                    } : null
                };

                return Ok(updatedSubdomain);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating subdomain stream ID", details = ex.Message });
            }
        }

        /// <summary>
        /// Get subdomain usage statistics
        /// </summary>
        /// <param name="id">Subdomain ID</param>
        /// <returns>Subdomain usage statistics</returns>
        [HttpGet("{id}/usage")]
        public async Task<IActionResult> GetSubdomainUsage(int id)
        {
            try
            {
                var subdomain = await _context.Subdomains
                    .Include(s => s.Domain)
                    .FirstOrDefaultAsync(s => s.SubdomainId == id);

                if (subdomain == null)
                {
                    return NotFound(new { error = $"Subdomain with ID {id} not found" });
                }

                var problemsCount = await _context.Problems
                    .CountAsync(p => p.SubdomainId == id);

                var testCasesCount = await _context.TestCases
                    .Where(tc => _context.Problems
                        .Where(p => p.SubdomainId == id)
                        .Select(p => p.Id)
                        .Contains(tc.ProblemId))
                    .CountAsync();

                var starterCodesCount = await _context.StarterCodes
                    .Where(sc => _context.Problems
                        .Where(p => p.SubdomainId == id)
                        .Select(p => p.Id)
                        .Contains(sc.ProblemId))
                    .CountAsync();

                var usageStats = new
                {
                    SubdomainId = subdomain.SubdomainId,
                    SubdomainName = subdomain.SubdomainName,
                    DomainId = subdomain.DomainId,
                    DomainName = subdomain.Domain?.DomainName ?? "Unknown",
                    ProblemsCount = problemsCount,
                    TestCasesCount = testCasesCount,
                    StarterCodesCount = starterCodesCount,
                    IsUsed = problemsCount > 0
                };

                return Ok(usageStats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving subdomain usage statistics", details = ex.Message });
            }
        }
    }
}
