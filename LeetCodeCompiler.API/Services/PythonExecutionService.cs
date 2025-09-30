using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace LeetCodeCompiler.API.Services
{
    public class PythonExecutionService : ICodeExecutionService
    {
        private readonly string tempDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "temp");
        private readonly IContainerPoolService _containerPool;
        private readonly ILogger<PythonExecutionService> _logger;
        private readonly IMemoryCache _cache;

        public PythonExecutionService(IContainerPoolService containerPool, ILogger<PythonExecutionService> logger, IMemoryCache cache)
        {
            _containerPool = containerPool;
            _logger = logger;
            _cache = cache;
        }
        public async Task<ExecutionResult> ExecuteAsync(string code, string input)
        {
            // 🚀 OPTIMIZED: Check cache for duplicate executions
            var cacheKey = GenerateCacheKey(code, input ?? "");
            if (_cache.TryGetValue(cacheKey, out ExecutionResult? cachedResult) && cachedResult != null)
            {
                _logger.LogDebug("Cache hit for Python execution");
                return cachedResult;
            }
            
            var result = new ExecutionResult
            {
                Stdout = "",
                Stderr = "",
                Error = "",
                Output = ""
            };
            
            // Get container from pool
            var containerId = await _containerPool.GetContainerAsync("python");
            if (string.IsNullOrEmpty(containerId))
            {
                result.Error = "No available containers for Python execution";
                return result;
            }

            try
            {
                var sw = Stopwatch.StartNew();
                
                // 🔧 FIXED: Two-step approach - first write code to file, then execute with input
                
                // Step 1: Write code to file
                var writeStartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"exec -i {containerId} sh -c \"cat > /tmp/solution.py\"",
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
                    result.Error = $"Failed to write code to file. Exit code: {writeProcess.ExitCode}";
                    return result;
                }
                
                // Step 2: Execute the file with input
                var execStartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"exec -i {containerId} python /tmp/solution.py",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                var process = new Process { StartInfo = execStartInfo };
                process.Start();
                
                // Send input data to the Python process
                if (!string.IsNullOrEmpty(input))
                {
                    await process.StandardInput.WriteAsync(input);
                }
                process.StandardInput.Close();
                
                // 🔧 FIXED: Use longer timeout for complex code
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                var processTask = process.WaitForExitAsync();
                var timeoutTask = Task.Delay(10000); // 10s timeout for complex code
                
                var completedTask = await Task.WhenAny(processTask, timeoutTask);
                sw.Stop();
                result.RuntimeMs = sw.Elapsed.TotalMilliseconds;
                
                if (completedTask == timeoutTask)
                {
                    // Timeout occurred
                    try { process.Kill(); } catch { }
                    result.Error = "Timeout";
                }
                else
                {
                    // Process completed normally
                    result.Stdout = await outputTask;
                    result.Stderr = await errorTask;
                    result.Output = result.Stdout?.Trim() ?? "";
                    
                    if (process.ExitCode != 0)
                    {
                        result.Error = $"Exit code: {process.ExitCode}";
                    }
                }
                
                result.MemoryMb = 0; // Docker handles memory management
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Python code");
                result.Error = ex.Message;
            }
            finally
            {
                // 🚀 OPTIMIZED: Non-blocking container return for faster response
                _ = Task.Run(async () => await _containerPool.ReturnContainerAsync(containerId, "python"));
            }
            
            // 🚀 OPTIMIZED: Cache successful results for 5 minutes
            if (string.IsNullOrEmpty(result.Error) && result.RuntimeMs < 10000)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            }
            
            return result;
        }

        private string GenerateCacheKey(string code, string input)
        {
            // 🚀 OPTIMIZED: Generate cache key for duplicate detection
            var combined = $"python:{code}:{input}";
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return Convert.ToBase64String(hash);
        }

        private string EscapeForShell(string code)
        {
            // Escape code for shell execution (single quotes)
            return code.Replace("'", "'\"'\"'");
        }
    }
} 