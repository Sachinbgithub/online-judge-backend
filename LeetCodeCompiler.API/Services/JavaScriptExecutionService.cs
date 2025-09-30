using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LeetCodeCompiler.API.Services
{
    public class JavaScriptExecutionService : ICodeExecutionService
    {
        private readonly IContainerPoolService _containerPool;
        private readonly ILogger<JavaScriptExecutionService> _logger;

        public JavaScriptExecutionService(IContainerPoolService containerPool, ILogger<JavaScriptExecutionService> logger)
        {
            _containerPool = containerPool;
            _logger = logger;
        }
        public async Task<ExecutionResult> ExecuteAsync(string code, string input)
        {
            // Get container from pool
            var containerId = await _containerPool.GetContainerAsync("javascript");
            if (string.IsNullOrEmpty(containerId))
            {
                return new ExecutionResult 
                { 
                    Stdout = "",
                    Stderr = "",
                    Error = "No available containers for JavaScript execution",
                    Output = ""
                };
            }

            try
            {
                var sw = Stopwatch.StartNew();
                
                // 🚀 OPTIMIZED: Direct docker exec to Node.js (no command-line escaping)
                var startInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"exec -i {containerId} node",  // Direct node execution
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                var process = new Process { StartInfo = startInfo };
                process.Start();
                
                // 🚀 OPTIMIZED: Write code directly to Node.js stdin (no escaping needed)
                await process.StandardInput.WriteAsync(code);
                if (!string.IsNullOrEmpty(input))
                {
                    await process.StandardInput.WriteAsync("\n");
                    await process.StandardInput.WriteAsync(input);
                }
                process.StandardInput.Close();
                
                // 🚀 OPTIMIZED: Use async timeout with better performance
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                var processTask = process.WaitForExitAsync();
                var timeoutTask = Task.Delay(3000); // 3s timeout for better performance
                
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
                _ = Task.Run(async () => await _containerPool.ReturnContainerAsync(containerId, "javascript"));
            }
        }

        private string EscapeJavaScriptCode(string code)
        {
            // Escape JavaScript code for command line execution
            return code.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("`", "\\`");
        }

        private string EscapeInput(string input)
        {
            // Escape for bash echo (handles quotes and backslashes)
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
} 