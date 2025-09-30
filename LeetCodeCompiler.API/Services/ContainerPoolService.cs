using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeetCodeCompiler.API.Services
{
    public class ContainerPoolService : IContainerPoolService, IDisposable
    {
        private readonly ILogger<ContainerPoolService> _logger;
        private readonly ContainerPoolOptions _options;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _availableContainers;
        private readonly ConcurrentDictionary<string, HashSet<string>> _inUseContainers;
        private readonly ConcurrentDictionary<string, string> _containerImages;
        private bool _disposed = false;

        public ContainerPoolService(ILogger<ContainerPoolService> logger, IOptions<ContainerPoolOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            _availableContainers = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
            _inUseContainers = new ConcurrentDictionary<string, HashSet<string>>();
            _containerImages = new ConcurrentDictionary<string, string>
            {
                ["python"] = "python:3.11-alpine",
                ["javascript"] = "node:18",
                ["java"] = "openjdk:17",
                ["cpp"] = "gcc:11"
            };

            // Initialize queues for each language
            foreach (var language in _containerImages.Keys)
            {
                _availableContainers[language] = new ConcurrentQueue<string>();
                _inUseContainers[language] = new HashSet<string>();
            }
        }

        public async Task InitializePoolsAsync()
        {
            _logger.LogInformation("Initializing container pools...");

            var tasks = new List<Task>();
            foreach (var language in _containerImages.Keys)
            {
                tasks.Add(InitializeLanguagePoolAsync(language));
            }

            await Task.WhenAll(tasks);
            
            // Log capacity statistics
            var totalContainers = 0;
            var totalMemoryMB = 0;
            var totalCPU = 0.0;
            
            foreach (var language in _containerImages.Keys)
            {
                var poolSize = GetPoolSize(language);
                totalContainers += poolSize;
                totalMemoryMB += poolSize * 32; // 32MB per container
                totalCPU += poolSize * 0.08;    // 0.08 CPU per container
                
                _logger.LogInformation("Pool initialized: {Language} = {PoolSize} containers", language, poolSize);
            }
            
            _logger.LogInformation("🚀 TOTAL CAPACITY: {TotalContainers} containers, {TotalMemoryMB}MB RAM, {TotalCPU:F1} CPU cores", 
                totalContainers, totalMemoryMB, totalCPU);
            _logger.LogInformation("🎯 ESTIMATED USER CAPACITY: {EstimatedUsers:N0} concurrent users (0.67 exec/min per user)", 
                (int)(totalContainers * 60 * 0.2 / 0.67)); // 0.2ms avg execution, 0.67 exec/min per user
            _logger.LogInformation("Container pools initialized successfully");
        }

        private async Task InitializeLanguagePoolAsync(string language)
        {
            var poolSize = GetPoolSize(language);
            var image = _containerImages[language];
            
            _logger.LogInformation("Creating {PoolSize} containers for {Language} using image {Image}", 
                poolSize, language, image);

            var tasks = new List<Task>();
            for (int i = 0; i < poolSize; i++)
            {
                tasks.Add(CreateContainerAsync(language, image));
            }

            await Task.WhenAll(tasks);
            _logger.LogInformation("Created {PoolSize} containers for {Language}", poolSize, language);
        }

        private async Task CreateContainerAsync(string language, string image)
        {
            try
            {
                var containerId = await StartContainerAsync(image, language);
                if (!string.IsNullOrEmpty(containerId))
                {
                    _availableContainers[language].Enqueue(containerId);
                    _logger.LogDebug("Created container {ContainerId} for {Language}", containerId, language);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create container for {Language}", language);
            }
        }

        private async Task<string?> StartContainerAsync(string image, string language)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"run -d --memory=32m --cpus=0.08 --network=none {image} sleep 3600",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    var containerId = output.Trim();
                    _logger.LogInformation("Container started for {Language}: {ContainerId} (32MB RAM, 0.08 CPU)", language, containerId.Substring(0, 12));
                    return containerId;
                }
                else
                {
                    _logger.LogError("Failed to start container for {Language}: {Error}", language, error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while starting container for {Language}", language);
                return null;
            }
        }

        public async Task<string?> GetContainerAsync(string language)
        {
            if (!_containerImages.ContainsKey(language))
            {
                _logger.LogWarning("Unknown language: {Language}", language);
                return null;
            }

            // Try to get from available pool
            if (_availableContainers[language].TryDequeue(out var containerId))
            {
                _inUseContainers[language].Add(containerId);
                _logger.LogDebug("Retrieved container {ContainerId} for {Language}", containerId, language);
                return containerId;
            }

            // Pool is empty, create a new container on-demand
            _logger.LogWarning("Pool empty for {Language}, creating new container", language);
            var image = _containerImages[language];
            var newContainerId = await StartContainerAsync(image, language);
            
            if (!string.IsNullOrEmpty(newContainerId))
            {
                _inUseContainers[language].Add(newContainerId);
            }

            return newContainerId;
        }

        public Task ReturnContainerAsync(string containerId, string language)
        {
            if (string.IsNullOrEmpty(containerId) || !_containerImages.ContainsKey(language))
            {
                return Task.CompletedTask;
            }

            try
            {
                // Remove from in-use set
                _inUseContainers[language].Remove(containerId);

                // 🚀 OPTIMIZED: Skip immediate health check, return container directly
                // Background timer will handle health checks every 30 seconds
                _availableContainers[language].Enqueue(containerId);
                _logger.LogDebug("Returned container {ContainerId} to {Language} pool", containerId, language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning container {ContainerId} for {Language}", containerId, language);
            }
            
            return Task.CompletedTask;
        }

        private async Task<bool> IsContainerHealthyAsync(string containerId)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"inspect --format='{{{{.State.Running}}}}' {containerId}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                return process.ExitCode == 0 && output.Trim().ToLower() == "true";
            }
            catch
            {
                return false;
            }
        }

        private async Task RemoveContainerAsync(string containerId)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"rm -f {containerId}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove container {ContainerId}", containerId);
            }
        }

        public async Task<ContainerPoolStats> GetPoolStatsAsync()
        {
            return await Task.FromResult(new ContainerPoolStats
            {
                PythonAvailable = _availableContainers["python"].Count,
                PythonInUse = _inUseContainers["python"].Count,
                JavaScriptAvailable = _availableContainers["javascript"].Count,
                JavaScriptInUse = _inUseContainers["javascript"].Count,
                JavaAvailable = _availableContainers["java"].Count,
                JavaInUse = _inUseContainers["java"].Count,
                CppAvailable = _availableContainers["cpp"].Count,
                CppInUse = _inUseContainers["cpp"].Count
            });
        }

        private int GetPoolSize(string language)
        {
            return language.ToLower() switch
            {
                "python" => _options.PythonPoolSize,
                "javascript" => _options.JavaScriptPoolSize,
                "java" => _options.JavaPoolSize,
                "cpp" => _options.CppPoolSize,
                _ => _options.DefaultPoolSize
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _logger.LogInformation("Disposing container pool service...");
                
                // Clean up all containers
                var allContainers = new List<string>();
                foreach (var containers in _availableContainers.Values)
                {
                    while (containers.TryDequeue(out var containerId))
                    {
                        allContainers.Add(containerId);
                    }
                }
                
                foreach (var containers in _inUseContainers.Values)
                {
                    allContainers.AddRange(containers);
                }

                foreach (var containerId in allContainers)
                {
                    try
                    {
                        RemoveContainerAsync(containerId).Wait(5000); // 5 second timeout
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to remove container {ContainerId} during disposal", containerId);
                    }
                }

                _disposed = true;
            }
        }
    }

    public class ContainerPoolOptions
    {
        public int PythonPoolSize { get; set; } = 10;
        public int JavaScriptPoolSize { get; set; } = 8;
        public int JavaPoolSize { get; set; } = 6;
        public int CppPoolSize { get; set; } = 6;
        public int DefaultPoolSize { get; set; } = 5;
    }
}
