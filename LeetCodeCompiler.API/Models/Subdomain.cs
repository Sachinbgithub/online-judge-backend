using System.Text.Json.Serialization;

namespace LeetCodeCompiler.API.Models
{
    public class Subdomain
    {
        public int SubdomainId { get; set; }
        public int DomainId { get; set; }
        public required string SubdomainName { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public Domain Domain { get; set; } = null!;
    }
}
