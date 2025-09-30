using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeetCodeCompiler.API.Services
{
    public class ExecutionResult
    {
        public required string Stdout { get; set; }
        public required string Stderr { get; set; }
        public double RuntimeMs { get; set; }
        public double MemoryMb { get; set; }
        public required string Error { get; set; }
        public required string Output { get; set; }
    }

    public interface ICodeExecutionService
    {
        Task<ExecutionResult> ExecuteAsync(string code, string input);
    }
} 