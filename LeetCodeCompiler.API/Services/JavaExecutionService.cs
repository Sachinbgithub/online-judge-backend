using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LeetCodeCompiler.API.Services
{
    public class JavaExecutionService : ICodeExecutionService
    {
        private readonly IContainerPoolService _containerPool;
        private readonly ILogger<JavaExecutionService> _logger;

        public JavaExecutionService(IContainerPoolService containerPool, ILogger<JavaExecutionService> logger)
        {
            _containerPool = containerPool;
            _logger = logger;
        }
        public async Task<ExecutionResult> ExecuteAsync(string code, string input)
        {
            // Get container from pool
            var containerId = await _containerPool.GetContainerAsync("java");
            if (string.IsNullOrEmpty(containerId))
            {
                return new ExecutionResult 
                { 
                    Stdout = "",
                    Stderr = "",
                    Error = "No available containers for Java execution",
                    Output = ""
                };
            }

            try
            {
                var sw = Stopwatch.StartNew();
                
                // 🔧 FIXED: Two-step approach - first write code to file, then compile and execute with input
                
                // Step 1: Write Java code to file
                var writeStartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"exec -i {containerId} sh -c \"cat > /tmp/Solution.java\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                var writeProcess = new Process { StartInfo = writeStartInfo };
                writeProcess.Start();
                await writeProcess.StandardInput.WriteAsync(code);
                writeProcess.StandardInput.Close();
                await writeProcess.WaitForExitAsync();
                
                if (writeProcess.ExitCode != 0)
                {
                    return new ExecutionResult 
                    { 
                        Stdout = "",
                        Stderr = "",
                        Error = $"Failed to write Java code to file. Exit code: {writeProcess.ExitCode}",
                        Output = "",
                        RuntimeMs = sw.Elapsed.TotalMilliseconds
                    };
                }
                
                // Step 2: Compile the Java file
                var compileStartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"exec {containerId} sh -c \"cd /tmp && javac Solution.java\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                var compileProcess = new Process { StartInfo = compileStartInfo };
                compileProcess.Start();
                await compileProcess.WaitForExitAsync();
                
                if (compileProcess.ExitCode != 0)
                {
                    var compileError = await compileProcess.StandardError.ReadToEndAsync();
                    return new ExecutionResult 
                    { 
                        Stdout = "",
                        Stderr = compileError,
                        Error = $"Compilation failed. Exit code: {compileProcess.ExitCode}",
                        Output = "",
                        RuntimeMs = sw.Elapsed.TotalMilliseconds
                    };
                }
                
                // Step 3: Execute the compiled Java program with input
                var execStartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"exec -i {containerId} sh -c \"cd /tmp && java Solution\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                var process = new Process { StartInfo = execStartInfo };
                process.Start();
                
                // Send input data to the Java process
                if (!string.IsNullOrEmpty(input))
                {
                    await process.StandardInput.WriteAsync(input);
                }
                process.StandardInput.Close();
                
                // 🔧 FIXED: Use longer timeout for Java compilation + execution
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                var processTask = process.WaitForExitAsync();
                var timeoutTask = Task.Delay(15000); // 15s timeout for Java compilation + execution
                
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
                _ = Task.Run(async () => await _containerPool.ReturnContainerAsync(containerId, "java"));
            }
        }

        private string EscapeJavaCode(string code)
        {
            // Escape Java code for shell execution
            return code.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("'", "\\'");
        }

        private string EscapeInput(string input)
        {
            // Escape for bash echo (handles quotes and backslashes)
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
} 