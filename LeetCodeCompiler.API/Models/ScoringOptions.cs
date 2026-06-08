namespace LeetCodeCompiler.API.Models
{
    /// <summary>
    /// Configurable default marks per problem difficulty when creating coding tests.
    /// Keys: 1 = Easy, 2 = Medium, 3 = Hard.
    /// </summary>
    public class ScoringOptions
    {
        public Dictionary<int, decimal> DefaultMarksByDifficulty { get; set; } = new()
        {
            { 1, 20m },
            { 2, 30m },
            { 3, 50m }
        };

        public decimal GetDefaultMarks(int difficulty, decimal fallback = 10m)
            => DefaultMarksByDifficulty.GetValueOrDefault(difficulty, fallback);
    }
}
