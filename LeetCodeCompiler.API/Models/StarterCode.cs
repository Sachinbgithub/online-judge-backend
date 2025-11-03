using System.Text.Json.Serialization;

namespace LeetCodeCompiler.API.Models
{
    public class StarterCode
    {
        public int Id { get; set; }
        public int ProblemId { get; set; }
        public required int Language { get; set; } // Integer foreign key to Languages table
        public required string Code { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public Problem Problem { get; set; } = null!;
        
        [JsonIgnore]
        public Language LanguageNavigation { get; set; } = null!;
    }
} 
