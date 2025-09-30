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
    }
}
