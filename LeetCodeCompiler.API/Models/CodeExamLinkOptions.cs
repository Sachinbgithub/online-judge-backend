namespace LeetCodeCompiler.API.Models
{
    /// <summary>
    /// Share-link settings. Set <see cref="BaseUrl"/> in appsettings.json under "CodeExamLink"
    /// or via environment variable CodeExamLink__BaseUrl (no trailing slash).
    /// </summary>
    public class CodeExamLinkOptions
    {
        public const string SectionName = "CodeExamLink";

        /// <summary>
        /// Frontend base URL for coding exam links, e.g. https://gatetutor.in/codeexam
        /// </summary>
        public string BaseUrl { get; set; } = "";
    }
}
