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
    }
}
