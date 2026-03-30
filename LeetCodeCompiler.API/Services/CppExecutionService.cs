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
                
                // Step 1: Write C++ code to file
                var writeStartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"exec -i {containerId} sh -c \"cat > /tmp/main.cpp\"",
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
                    var writeError = await writeProcess.StandardError.ReadToEndAsync();
                    return new ExecutionResult 
                    { 
                        Stdout = "",
                        Stderr = writeError,
                        Error = $"Failed to write C++ code to file. Exit code: {writeProcess.ExitCode}",
                        Output = "",
                        RuntimeMs = sw.Elapsed.TotalMilliseconds
                    };
                }
                
                // Step 2: Compile the C++ file
                var compileStartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"exec {containerId} sh -c \"cd /tmp && g++ main.cpp -o main\"",
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
                
                // Step 3: Execute the compiled program with input
                var execStartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"exec -i {containerId} /tmp/main",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                var process = new Process { StartInfo = execStartInfo };
                process.Start();
                
                // Send input to the C++ program
                if (!string.IsNullOrEmpty(input))
                {
                    await process.StandardInput.WriteAsync(input);
                }
                process.StandardInput.Close();
                
                // Timeout handling (10s for C++ compilation + execution)
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                var processTask = process.WaitForExitAsync();
                var timeoutTask = Task.Delay(10000);
                
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing C++ code");
                return new ExecutionResult
                {
                    Stdout = "",
                    Stderr = "",
                    Error = ex.Message,
                    Output = ""
                };
            }
            finally
            {
                // Non-blocking container return for faster response
                _ = Task.Run(async () => await _containerPool.ReturnContainerAsync(containerId, "cpp"));
            }
        }
    }
}
