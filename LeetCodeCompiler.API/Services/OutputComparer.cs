namespace LeetCodeCompiler.API.Services
{
    /// <summary>
    /// Canonical output comparison for judge pass/fail. Leading spaces per line are significant;
    /// trailing whitespace and trailing blank lines are ignored.
    /// </summary>
    public static class OutputComparer
    {
        public static bool Matches(string? actual, string expected)
        {
            var actualLines = SplitOutputLines(actual ?? "");
            var expectedLines = SplitOutputLines(expected);

            if (actualLines.Count != expectedLines.Count)
                return false;

            for (var i = 0; i < expectedLines.Count; i++)
            {
                if (actualLines[i] != expectedLines[i])
                    return false;
            }

            return true;
        }

        private static List<string> SplitOutputLines(string output)
        {
            var normalized = output.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = normalized.Split('\n').Select(l => l.TrimEnd()).ToList();

            while (lines.Count > 0 && lines[^1] == "")
                lines.RemoveAt(lines.Count - 1);

            return lines;
        }
    }
}
