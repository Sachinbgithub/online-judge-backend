using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DomainController : ControllerBase
    {
        private readonly AppDbContext _context;
        
        public DomainController(AppDbContext context) => _context = context;

        /// <summary>
        /// Get all domains
        /// </summary>
        /// <returns>List of all domains</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllDomains()
        {
            var domains = await _context.Domains
                .Include(d => d.Subdomains)
                .Select(d => new DomainDto
                {
                    DomainId = d.DomainId,
                    DomainName = d.DomainName,
                    Subdomains = d.Subdomains.Select(s => new SubdomainDto
                    {
                        SubdomainId = s.SubdomainId,
                        DomainId = s.DomainId,
                        SubdomainName = s.SubdomainName
                    }).ToList()
                })
                .ToListAsync();
            
            return Ok(domains);
        }

        /// <summary>
        /// Get domain by ID
        /// </summary>
        /// <param name="id">Domain ID</param>
        /// <returns>Domain with subdomains</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDomainById(int id)
        {
            var domain = await _context.Domains
                .Include(d => d.Subdomains)
                .Where(d => d.DomainId == id)
                .Select(d => new DomainDto
                {
                    DomainId = d.DomainId,
                    DomainName = d.DomainName,
                    Subdomains = d.Subdomains.Select(s => new SubdomainDto
                    {
                        SubdomainId = s.SubdomainId,
                        DomainId = s.DomainId,
                        SubdomainName = s.SubdomainName
                    }).ToList()
                })
                .FirstOrDefaultAsync();
            
            if (domain == null)
                return NotFound($"Domain with ID {id} not found");
            
            return Ok(domain);
        }

        /// <summary>
        /// Create a new domain
        /// </summary>
        /// <param name="request">Domain creation request</param>
        /// <returns>Created domain</returns>
        [HttpPost]
        public async Task<IActionResult> CreateDomain([FromBody] CreateDomainRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.DomainName))
                {
                    return BadRequest(new { error = "Domain name is required" });
                }

                // Check if domain already exists
                var existingDomain = await _context.Domains
                    .FirstOrDefaultAsync(d => d.DomainName.ToLower() == request.DomainName.ToLower());
                
                if (existingDomain != null)
                {
                    return Conflict(new { error = $"Domain '{request.DomainName}' already exists" });
                }

                // Create new domain
                var newDomain = new Domain
                {
                    DomainName = request.DomainName.Trim()
                };

                _context.Domains.Add(newDomain);
                await _context.SaveChangesAsync();

                // Return the created domain
                var createdDomain = new DomainDto
                {
                    DomainId = newDomain.DomainId,
                    DomainName = newDomain.DomainName,
                    Subdomains = new List<SubdomainDto>()
                };

                return CreatedAtAction(nameof(GetDomainById), new { id = newDomain.DomainId }, createdDomain);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the domain", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing domain
        /// </summary>
        /// <param name="id">Domain ID</param>
        /// <param name="request">Domain update request</param>
        /// <returns>Updated domain</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDomain(int id, [FromBody] UpdateDomainRequest request)
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

                // Find the domain
                var domain = await _context.Domains
                    .FirstOrDefaultAsync(d => d.DomainId == id);

                if (domain == null)
                {
                    return NotFound(new { error = $"Domain with ID {id} not found" });
                }

                // Check if another domain with the same name exists (case-insensitive)
                var existingDomain = await _context.Domains
                    .FirstOrDefaultAsync(d => d.DomainId != id && d.DomainName.ToLower() == request.DomainName.ToLower());

                if (existingDomain != null)
                {
                    return Conflict(new { error = $"Domain '{request.DomainName}' already exists" });
                }

                // Update the domain
                domain.DomainName = request.DomainName.Trim();

                await _context.SaveChangesAsync();

                // Get subdomains for response
                var subdomains = await _context.Subdomains
                    .Where(s => s.DomainId == id)
                    .Select(s => new SubdomainDto
                    {
                        SubdomainId = s.SubdomainId,
                        DomainId = s.DomainId,
                        SubdomainName = s.SubdomainName
                    })
                    .ToListAsync();

                // Return the updated domain
                var updatedDomain = new DomainDto
                {
                    DomainId = domain.DomainId,
                    DomainName = domain.DomainName,
                    Subdomains = subdomains
                };

                return Ok(updatedDomain);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the domain", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a domain
        /// </summary>
        /// <param name="id">Domain ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDomain(int id)
        {
            try
            {
                var domain = await _context.Domains
                    .FirstOrDefaultAsync(d => d.DomainId == id);

                if (domain == null)
                {
                    return NotFound(new { error = $"Domain with ID {id} not found" });
                }

                // Check if domain has subdomains
                var hasSubdomains = await _context.Subdomains
                    .AnyAsync(s => s.DomainId == id);

                if (hasSubdomains)
                {
                    return Conflict(new { error = $"Cannot delete domain '{domain.DomainName}' because it has subdomains. Please delete all subdomains first." });
                }

                _context.Domains.Remove(domain);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Domain '{domain.DomainName}' has been deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the domain", details = ex.Message });
            }
        }

        /// <summary>
        /// Get domain usage statistics
        /// </summary>
        /// <param name="id">Domain ID</param>
        /// <returns>Domain usage statistics</returns>
        [HttpGet("{id}/usage")]
        public async Task<IActionResult> GetDomainUsage(int id)
        {
            try
            {
                var domain = await _context.Domains
                    .FirstOrDefaultAsync(d => d.DomainId == id);

                if (domain == null)
                {
                    return NotFound(new { error = $"Domain with ID {id} not found" });
                }

                var subdomainsCount = await _context.Subdomains
                    .CountAsync(s => s.DomainId == id);

                var problemsCount = await _context.Problems
                    .Where(p => _context.Subdomains
                        .Where(s => s.DomainId == id)
                        .Select(s => s.SubdomainId)
                        .Contains(p.SubdomainId))
                    .CountAsync();

                var usageStats = new
                {
                    DomainId = domain.DomainId,
                    DomainName = domain.DomainName,
                    SubdomainsCount = subdomainsCount,
                    ProblemsCount = problemsCount,
                    IsUsed = subdomainsCount > 0 || problemsCount > 0
                };

                return Ok(usageStats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving domain usage statistics", details = ex.Message });
            }
        }
    }
}
