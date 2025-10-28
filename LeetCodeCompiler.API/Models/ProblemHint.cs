using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LeetCodeCompiler.API.Models
{
    [Table("ProblemHints")]
    public class ProblemHint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProblemId { get; set; }

        [Required]
        [StringLength(1000)]
        public required string Hint { get; set; }

        // Navigation properties
        [JsonIgnore]
        public Problem Problem { get; set; } = null!;
    }
}
