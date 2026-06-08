namespace LeetCodeCompiler.API.Services
{
    /// <summary>
    /// Single source of truth for converting judge pass counts into marks.
    /// </summary>
    public static class ScoreCalculator
    {
        /// <summary>
        /// Coding and practice tests: decimal marks, 2 decimal places.
        /// </summary>
        public static decimal DecimalScore(int passed, int total, decimal maxMarks)
            => total > 0
                ? Math.Round((decimal)passed / total * maxMarks, 2, MidpointRounding.AwayFromZero)
                : 0m;
    }
}
