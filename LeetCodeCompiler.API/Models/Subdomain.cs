using System.Text.Json.Serialization;

namespace LeetCodeCompiler.API.Models
{
    public class Subdomain
    {
        public int SubdomainId { get; set; }
        public int DomainId { get; set; }
        public required string SubdomainName { get; set; }
        public int? StreamId { get; set; }
        public int? CreatedByUserId { get; set; }
        public int? UpdatedByUserId { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public Domain Domain { get; set; } = null!;
    }
}
