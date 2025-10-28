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
    }
}
