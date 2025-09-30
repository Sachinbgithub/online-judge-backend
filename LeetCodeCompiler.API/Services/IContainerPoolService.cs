using System.Threading.Tasks;

namespace LeetCodeCompiler.API.Services
{
    public interface IContainerPoolService
    {
        /// <summary>
        /// Gets a container from the pool for the specified language
        /// </summary>
        /// <param name="language">Programming language (python, javascript, java, cpp)</param>
        /// <returns>Container ID or null if no container available</returns>
        Task<string?> GetContainerAsync(string language);

        /// <summary>
        /// Returns a container to the pool after execution
        /// </summary>
        /// <param name="containerId">Container ID to return</param>
        /// <param name="language">Programming language</param>
        Task ReturnContainerAsync(string containerId, string language);

        /// <summary>
        /// Initializes the container pools for all languages
        /// </summary>
        Task InitializePoolsAsync();

        /// <summary>
        /// Gets pool statistics for monitoring
        /// </summary>
        Task<ContainerPoolStats> GetPoolStatsAsync();
    }

    public class ContainerPoolStats
    {
        public int PythonAvailable { get; set; }
        public int PythonInUse { get; set; }
        public int JavaScriptAvailable { get; set; }
        public int JavaScriptInUse { get; set; }
        public int JavaAvailable { get; set; }
        public int JavaInUse { get; set; }
        public int CppAvailable { get; set; }
        public int CppInUse { get; set; }
        public int TotalAvailable => PythonAvailable + JavaScriptAvailable + JavaAvailable + CppAvailable;
        public int TotalInUse => PythonInUse + JavaScriptInUse + JavaInUse + CppInUse;
    }
}
