using System.Text.Json.Serialization;

namespace LeetCodeCompiler.API.Models
{
    public class Domain
    {
        public int DomainId { get; set; }
        public required string DomainName { get; set; }
        public int? StreamId { get; set; }
        public int? CreatedByUserId { get; set; }
        public int? UpdatedByUserId { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public List<Subdomain> Subdomains { get; set; } = new();
    }
}
