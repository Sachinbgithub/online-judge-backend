using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LeetCodeCompiler.API.Services
{
    public class CppExecutionService : ICodeExecutionService
    {
        private readonly IContainerPoolService _containerPool;
        private readonly ILogger<CppExecutionService> _logger;

        public CppExecutionService(IContainerPoolService containerPool, ILogger<CppExecutionService> logger)
        {
            _containerPool = containerPool;
            _logger = logger;
        }
        public async Task<ExecutionResult> ExecuteAsync(string code, string input)
        {
            // Get container from pool
            var containerId = await _containerPool.GetContainerAsync("cpp");
            if (string.IsNullOrEmpty(containerId))
            {
                return new ExecutionResult 
                { 
                    Stdout = "",
                    Stderr = "",
                    Error = "No available containers for C++ execution",
                    Output = ""
                };
            }

            try
            {
                var sw = Stopwatch.StartNew();
                
                // 🚀 OPTIMIZED: Use bash here-document for C++ (more reliable than echo escaping)
                var cppCode = code.Replace("$", "\\$").Replace("`", "\\`").Replace("\\", "\\\\");
                var startInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"exec -i {containerId} bash -c \"cat > /tmp/main.cpp && cd /tmp && g++ main.cpp -o main && ./main\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                var process = new Process { StartInfo = startInfo };
                process.Start();
                
                // 🚀 OPTIMIZED: Write C++ code directly to stdin (no shell escaping)
                await process.StandardInput.WriteAsync(cppCode);
                await process.StandardInput.WriteAsync("\n");
                
                // Send input to the compiled program if provided
                if (!string.IsNullOrEmpty(input))
                {
                    await process.StandardInput.WriteAsync(input);
                }
                process.StandardInput.Close();
                
                // 🚀 OPTIMIZED: Use async timeout with better performance (C++ needs time for compilation)
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                var processTask = process.WaitForExitAsync();
                var timeoutTask = Task.Delay(5000); // 5s timeout for C++ compilation + execution
                
                var completedTask = await Task.WhenAny(processTask, timeoutTask);
                sw.Stop();
                
                if (completedTask == timeoutTask)
                {
                    // Timeout occurred
                    try { process.Kill(); } catch { }
                    return new ExecutionResult 
                    { 
                        Stdout = "",
                        Stderr = "",
                        Error = "Timeout", 
                        Output = "",
                        RuntimeMs = sw.Elapsed.TotalMilliseconds 
                    };
                }
                
                // Process completed normally
                var output = await outputTask;
                var error = await errorTask;

                return new ExecutionResult
                {
                    Output = output?.Trim() ?? "",
                    Stdout = output?.Trim() ?? "",
                    Stderr = error?.Trim() ?? "",
                    Error = process.ExitCode == 0 ? "" : $"Exit code: {process.ExitCode}",
                    RuntimeMs = sw.Elapsed.TotalMilliseconds
                };
            }
            finally
            {
                // 🚀 OPTIMIZED: Non-blocking container return for faster response
                _ = Task.Run(async () => await _containerPool.ReturnContainerAsync(containerId, "cpp"));
            }
        }

        private string EscapeCppCode(string code)
        {
            // Escape C++ code for shell execution
            return code.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("'", "\\'");
        }

        private string EscapeInput(string input)
        {
            // Escape for bash echo (handles quotes and backslashes)
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
} 