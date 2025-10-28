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
                    Domain = s.Domain != null ? new DomainBasicDto
                    {
                        DomainId = s.Domain.DomainId,
                        DomainName = s.Domain.DomainName
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
                    Domain = s.Domain != null ? new DomainBasicDto
                    {
                        DomainId = s.Domain.DomainId,
                        DomainName = s.Domain.DomainName
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
                    Domain = s.Domain != null ? new DomainBasicDto
                    {
                        DomainId = s.Domain.DomainId,
                        DomainName = s.Domain.DomainName
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
                    SubdomainName = request.SubdomainName.Trim()
                };

                _context.Subdomains.Add(newSubdomain);
                await _context.SaveChangesAsync();

                // Return the created subdomain with domain information
                var createdSubdomain = new SubdomainDto
                {
                    SubdomainId = newSubdomain.SubdomainId,
                    DomainId = newSubdomain.DomainId,
                    SubdomainName = newSubdomain.SubdomainName,
                    Domain = new DomainBasicDto
                    {
                        DomainId = domain.DomainId,
                        DomainName = domain.DomainName
                    }
                };

                return CreatedAtAction(nameof(GetSubdomainById), new { id = newSubdomain.SubdomainId }, createdSubdomain);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the subdomain", details = ex.Message });
            }
        }
    }
}
