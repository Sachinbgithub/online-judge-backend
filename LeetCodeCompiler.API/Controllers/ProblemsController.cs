using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Data;
using LeetCodeCompiler.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProblemsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ProblemsController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetProblems()
        {
            var problems = await _context.Problems
                .Include(p => p.TestCases)
                .Include(p => p.StarterCodes)
                .ToListAsync();
            
            return Ok(problems);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProblem(int id)
        {
            var problem = await _context.Problems
                .Include(p => p.TestCases)
                .Include(p => p.StarterCodes)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            return problem == null ? NotFound() : Ok(problem);
        }

        /// <summary>
        /// Get all problems by subdomain ID
        /// </summary>
        /// <param name="subdomainId">Subdomain ID</param>
        /// <returns>List of problems for the specified subdomain</returns>
        [HttpGet("subdomain/{subdomainId}")]
        public async Task<IActionResult> GetProblemsBySubdomainId(int subdomainId)
        {
            try
            {
                var problems = await _context.Problems
                    .Include(p => p.TestCases)
                    .Include(p => p.StarterCodes)
                    .Where(p => p.SubdomainId == subdomainId)
                    .ToListAsync();
                
                return Ok(problems);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving problems: {ex.Message}");
            }
        }
    }
}