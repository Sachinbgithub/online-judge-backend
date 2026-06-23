using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LeetCodeCompiler.API.Models;

namespace LeetCodeCompiler.API.Services
{
    public class StudentProfileService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StudentProfileService> _logger;
        private readonly string? _baseUrl;

        public StudentProfileService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<StudentProfileService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("StudentProfileAPI");
            _logger = logger;
            _baseUrl = configuration["StudentProfileApi:BaseUrl"]?.TrimEnd('/');
        }

        public async Task<StudentProfileData?> GetStudentProfileAsync(long userId)
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
            {
                return null;
            }

            try
            {
                var url = $"{_baseUrl}/api/UserProfileData/profile/{userId}";
                _logger.LogInformation("Fetching student profile for UserId: {UserId} from {Url}", userId, url);

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<StudentProfileApiResponse>();

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        _logger.LogInformation("Successfully retrieved student profile for UserId: {UserId}", userId);
                        return apiResponse.Data;
                    }

                    _logger.LogWarning("External API returned success=false or null data for UserId: {UserId}", userId);
                    return null;
                }

                _logger.LogWarning("External API returned status {StatusCode} for UserId: {UserId}",
                    response.StatusCode, userId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch student profile for UserId: {UserId}. Continuing without student data.", userId);
                return null;
            }
        }
    }

    public class StudentProfileApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public StudentProfileData? Data { get; set; }
    }
}
