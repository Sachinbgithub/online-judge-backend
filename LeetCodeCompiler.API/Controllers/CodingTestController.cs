using Microsoft.AspNetCore.Mvc;
using LeetCodeCompiler.API.Models;
using LeetCodeCompiler.API.Services;

namespace LeetCodeCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CodingTestController : ControllerBase
    {
        private readonly ICodingTestService _codingTestService;

        public CodingTestController(ICodingTestService codingTestService)
        {
            _codingTestService = codingTestService;
        }

        // Test Management Endpoints
        /// <summary>
        /// Creates a new coding test
        /// </summary>
        /// <param name="request">The coding test creation request</param>
        /// <returns>The created coding test</returns>
        [HttpPost]
        public async Task<IActionResult> CreateCodingTest([FromBody] CreateCodingTestRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _codingTestService.CreateCodingTestAsync(request);
                return CreatedAtAction(nameof(GetCodingTest), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create coding test", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets a coding test by ID
        /// </summary>
        /// <param name="id">The coding test ID</param>
        /// <returns>The coding test details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCodingTest(int id)
        {
            try
            {
                var result = await _codingTestService.GetCodingTestByIdAsync(id);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve coding test", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets all coding tests
        /// </summary>
        /// <returns>List of all coding tests</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllCodingTests()
        {
            try
            {
                var result = await _codingTestService.GetAllCodingTestsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve coding tests", details = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetCodingTestsByUser(int userId, [FromQuery] string? subjectName = null, [FromQuery] string? topicName = null, [FromQuery] bool isEnabled = true)
        {
            try
            {
                var result = await _codingTestService.GetCodingTestsByUserAsync(userId, subjectName, topicName, isEnabled);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve user coding tests", details = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateCodingTest([FromBody] UpdateCodingTestRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _codingTestService.UpdateCodingTestAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update coding test", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCodingTest(int id)
        {
            try
            {
                var result = await _codingTestService.DeleteCodingTestAsync(id);
                if (result)
                {
                    return Ok(new { message = "Coding test deleted successfully" });
                }
                return NotFound(new { error = "Coding test not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete coding test", details = ex.Message });
            }
        }

        [HttpPost("{id}/publish")]
        public async Task<IActionResult> PublishCodingTest(int id)
        {
            try
            {
                var result = await _codingTestService.PublishCodingTestAsync(id);
                if (result)
                {
                    return Ok(new { message = "Coding test published successfully" });
                }
                return NotFound(new { error = "Coding test not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to publish coding test", details = ex.Message });
            }
        }

        [HttpPost("{id}/unpublish")]
        public async Task<IActionResult> UnpublishCodingTest(int id)
        {
            try
            {
                var result = await _codingTestService.UnpublishCodingTestAsync(id);
                if (result)
                {
                    return Ok(new { message = "Coding test unpublished successfully" });
                }
                return NotFound(new { error = "Coding test not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to unpublish coding test", details = ex.Message });
            }
        }

        // Test Attempt Endpoints
        [HttpPost("start")]
        public async Task<IActionResult> StartCodingTest([FromBody] StartCodingTestRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _codingTestService.StartCodingTestAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to start coding test", details = ex.Message });
            }
        }

        [HttpGet("attempt/{attemptId}")]
        public async Task<IActionResult> GetCodingTestAttempt(int attemptId)
        {
            try
            {
                var result = await _codingTestService.GetCodingTestAttemptAsync(attemptId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve coding test attempt", details = ex.Message });
            }
        }

        [HttpGet("user/{userId}/test/{codingTestId}/attempts")]
        public async Task<IActionResult> GetUserCodingTestAttempts(int userId, int codingTestId)
        {
            try
            {
                var result = await _codingTestService.GetUserCodingTestAttemptsAsync(userId, codingTestId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve user coding test attempts", details = ex.Message });
            }
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitCodingTest([FromBody] SubmitCodingTestRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _codingTestService.SubmitCodingTestAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to submit coding test", details = ex.Message });
            }
        }

        [HttpPost("submit-whole-test")]
        public async Task<IActionResult> SubmitWholeCodingTest([FromBody] SubmitWholeCodingTestRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _codingTestService.SubmitWholeCodingTestAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to submit whole coding test", details = ex.Message });
            }
        }

        // =============================================
        // Submission Results Endpoints
        // =============================================

        /// <summary>
        /// Gets coding test submissions with optional filters
        /// </summary>
        /// <param name="request">Filter request</param>
        /// <returns>List of submissions</returns>
        [HttpPost("submissions")]
        public async Task<IActionResult> GetCodingTestSubmissions([FromBody] GetCodingTestSubmissionsRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _codingTestService.GetCodingTestSubmissionsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve submissions", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets a specific submission by ID
        /// </summary>
        /// <param name="submissionId">Submission ID</param>
        /// <returns>Submission details</returns>
        [HttpGet("submissions/{submissionId}")]
        public async Task<IActionResult> GetCodingTestSubmissionById(long submissionId)
        {
            try
            {
                var result = await _codingTestService.GetCodingTestSubmissionByIdAsync(submissionId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve submission", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets test case results for a specific submission
        /// </summary>
        /// <param name="submissionId">Submission ID</param>
        /// <returns>List of test case results</returns>
        [HttpGet("submissions/{submissionId}/test-cases")]
        public async Task<IActionResult> GetSubmissionTestCaseResults(long submissionId)
        {
            try
            {
                var result = await _codingTestService.GetSubmissionTestCaseResultsAsync(submissionId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve test case results", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets statistics for a coding test
        /// </summary>
        /// <param name="codingTestId">Coding test ID</param>
        /// <returns>Test statistics</returns>
        [HttpGet("{codingTestId}/statistics")]
        public async Task<IActionResult> GetCodingTestStatistics(int codingTestId)
        {
            try
            {
                var result = await _codingTestService.GetCodingTestStatisticsAsync(codingTestId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve statistics", details = ex.Message });
            }
        }

        // =============================================
        // Test Status Management Endpoint
        // =============================================

        /// <summary>
        /// Ends a coding test for a user
        /// </summary>
        /// <param name="request">End test request with status</param>
        /// <returns>Test status</returns>
        [HttpPost("end-test")]
        public async Task<IActionResult> EndTest([FromBody] EndTestRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _codingTestService.EndTestAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to end test", details = ex.Message });
            }
        }

        [HttpPost("attempt/{attemptId}/abandon")]
        public async Task<IActionResult> AbandonCodingTest(int attemptId, [FromBody] int userId)
        {
            try
            {
                var result = await _codingTestService.AbandonCodingTestAsync(attemptId, userId);
                if (result)
                {
                    return Ok(new { message = "Coding test abandoned successfully" });
                }
                return NotFound(new { error = "Coding test attempt not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to abandon coding test", details = ex.Message });
            }
        }

        // Question Attempt Endpoints
        [HttpPost("attempt/{codingTestAttemptId}/question/{questionId}/start")]
        public async Task<IActionResult> StartQuestionAttempt(int codingTestAttemptId, int questionId, [FromBody] int userId)
        {
            try
            {
                var result = await _codingTestService.StartQuestionAttemptAsync(codingTestAttemptId, questionId, userId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to start question attempt", details = ex.Message });
            }
        }

        [HttpGet("question-attempt/{questionAttemptId}")]
        public async Task<IActionResult> GetQuestionAttempt(int questionAttemptId)
        {
            try
            {
                var result = await _codingTestService.GetQuestionAttemptAsync(questionAttemptId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve question attempt", details = ex.Message });
            }
        }

        [HttpGet("attempt/{codingTestAttemptId}/questions")]
        public async Task<IActionResult> GetQuestionAttemptsForTest(int codingTestAttemptId)
        {
            try
            {
                var result = await _codingTestService.GetQuestionAttemptsForTestAsync(codingTestAttemptId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve question attempts", details = ex.Message });
            }
        }

        [HttpPost("question/submit")]
        public async Task<IActionResult> SubmitQuestion([FromBody] SubmitQuestionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _codingTestService.SubmitQuestionAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to submit question", details = ex.Message });
            }
        }

        // Analytics and Reports Endpoints
        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetCodingTestsByStatus(string status)
        {
            try
            {
                var result = await _codingTestService.GetCodingTestsByStatusAsync(status);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve coding tests by status", details = ex.Message });
            }
        }


        [HttpGet("{id}/analytics")]
        public async Task<IActionResult> GetCodingTestAnalytics(int id)
        {
            try
            {
                var result = await _codingTestService.GetCodingTestAnalyticsAsync(id);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve coding test analytics", details = ex.Message });
            }
        }

        [HttpGet("{id}/results")]
        public async Task<IActionResult> GetCodingTestResults(int id)
        {
            try
            {
                var result = await _codingTestService.GetCodingTestResultsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve coding test results", details = ex.Message });
            }
        }

        // Validation Endpoints
        [HttpPost("{id}/validate-access")]
        public async Task<IActionResult> ValidateAccessCode(int id, [FromBody] string accessCode)
        {
            try
            {
                var result = await _codingTestService.ValidateAccessCodeAsync(id, accessCode);
                return Ok(new { isValid = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to validate access code", details = ex.Message });
            }
        }

        [HttpGet("{id}/can-attempt")]
        public async Task<IActionResult> CanUserAttemptTest(int id, [FromQuery] int userId)
        {
            try
            {
                var result = await _codingTestService.CanUserAttemptTestAsync(userId, id);
                return Ok(new { canAttempt = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to check if user can attempt test", details = ex.Message });
            }
        }

        [HttpGet("{id}/is-active")]
        public async Task<IActionResult> IsTestActive(int id)
        {
            try
            {
                var result = await _codingTestService.IsTestActiveAsync(id);
                return Ok(new { isActive = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to check if test is active", details = ex.Message });
            }
        }

        [HttpGet("{id}/is-expired")]
        public async Task<IActionResult> IsTestExpired(int id)
        {
            try
            {
                var result = await _codingTestService.IsTestExpiredAsync(id);
                return Ok(new { isExpired = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to check if test is expired", details = ex.Message });
            }
        }

        // =============================================
        // Assignment Endpoints
        // =============================================

        /// <summary>
        /// Assigns a coding test to a user
        /// </summary>
        /// <param name="request">Assignment request</param>
        /// <returns>Assignment details</returns>
        [HttpPost("assign")]
        public async Task<IActionResult> AssignCodingTest([FromBody] AssignCodingTestRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _codingTestService.AssignCodingTestAsync(request);
                return CreatedAtAction(nameof(GetAssignedTestsByUser), 
                    new { userId = request.AssignedToUserId, userType = request.AssignedToUserType }, result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to assign coding test", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets all assigned tests for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="userType">User type</param>
        /// <param name="testType">Optional test type filter</param>
        /// <param name="classId">Optional class ID filter</param>
        /// <returns>List of assigned tests</returns>
        [HttpGet("assigned/user/{userId}")]
        public async Task<IActionResult> GetAssignedTestsByUser(long userId, byte userType, 
            [FromQuery] int? testType = null, [FromQuery] long? classId = null)
        {
            try
            {
                var result = await _codingTestService.GetAssignedTestsByUserAsync(userId, userType, testType, classId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve assigned tests", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets all users assigned to a specific test
        /// </summary>
        /// <param name="codingTestId">Coding test ID</param>
        /// <returns>List of assigned users</returns>
        [HttpGet("assigned/test/{codingTestId}")]
        public async Task<IActionResult> GetAssignedTestsByTest(int codingTestId)
        {
            try
            {
                var result = await _codingTestService.GetAssignedTestsByTestAsync(codingTestId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve test assignments", details = ex.Message });
            }
        }

        /// <summary>
        /// Unassigns a coding test from a user
        /// </summary>
        /// <param name="assignedId">Assignment ID</param>
        /// <param name="unassignedByUserId">User ID who is unassigning</param>
        /// <returns>Success status</returns>
        [HttpDelete("assigned/{assignedId}")]
        public async Task<IActionResult> UnassignCodingTest(long assignedId, [FromQuery] long unassignedByUserId)
        {
            try
            {
                var result = await _codingTestService.UnassignCodingTestAsync(assignedId, unassignedByUserId);
                if (result)
                {
                    return Ok(new { success = true, message = "Test unassigned successfully" });
                }
                else
                {
                    return NotFound(new { error = "Assignment not found or already unassigned" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to unassign coding test", details = ex.Message });
            }
        }

        /// <summary>
        /// Diagnostic endpoint to check why a user cannot attempt a test
        /// </summary>
        /// <param name="codingTestId">Test ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>Detailed diagnostic information</returns>
        [HttpGet("{codingTestId}/diagnose-user-access/{userId}")]
        public async Task<IActionResult> DiagnoseUserAccess(int codingTestId, int userId)
        {
            try
            {
                var codingTest = await _codingTestService.GetCodingTestByIdAsync(codingTestId);
                var now = DateTime.UtcNow;
                
                // Check assignment
                var assignments = await _codingTestService.GetAssignedTestsByUserAsync(userId, 25, 1002, null);
                var userAssignment = assignments.FirstOrDefault(a => a.CodingTestId == codingTestId);
                
                // Check existing attempts
                var existingAttempts = await _codingTestService.GetUserCodingTestAttemptsAsync(userId, codingTestId);
                
                var result = new
                {
                    canAttempt = await _codingTestService.CanUserAttemptTestAsync(userId, codingTestId),
                    testExists = codingTest != null,
                    testActive = codingTest?.IsActive ?? false,
                    testPublished = codingTest?.IsPublished ?? false,
                    withinDateRange = codingTest != null && now >= codingTest.StartDate && now <= codingTest.EndDate,
                    startDate = codingTest?.StartDate,
                    endDate = codingTest?.EndDate,
                    currentTime = now,
                    allowMultipleAttempts = codingTest?.AllowMultipleAttempts ?? false,
                    maxAttempts = codingTest?.MaxAttempts ?? 0,
                    existingAttemptCount = existingAttempts.Count,
                    hasReachedAttemptLimit = !(codingTest?.AllowMultipleAttempts ?? false) ? existingAttempts.Any() : existingAttempts.Count >= (codingTest?.MaxAttempts ?? 0),
                    assignment = userAssignment != null ? new
                    {
                        assignedId = userAssignment.AssignedId,
                        assignedDate = userAssignment.AssignedDate,
                        testType = userAssignment.TestType,
                        testMode = userAssignment.TestMode,
                        status = userAssignment.Status
                    } : null,
                    allAssignments = assignments.Select(a => new { a.AssignedId, a.CodingTestId, a.TestName, a.Status }).ToList()
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to diagnose user access", details = ex.Message });
            }
        }

        // =============================================
        // Comprehensive Test Results Endpoint
        // =============================================

        /// <summary>
        /// Gets comprehensive test results for a user and test, combining data from both CodingTestSubmissions and CodingTestSubmissionResults tables
        /// </summary>
        /// <param name="request">Request containing UserId, CodingTestId, and optional AttemptNumber</param>
        /// <returns>Comprehensive test results with detailed test case results and summary</returns>
        [HttpPost("results/comprehensive")]
        public async Task<IActionResult> GetComprehensiveTestResults([FromBody] GetTestResultsRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _codingTestService.GetComprehensiveTestResultsAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve comprehensive test results", details = ex.Message });
            }
        }

        /// <summary>
        /// Gets comprehensive test results for a user and test using query parameters (alternative endpoint)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="codingTestId">Coding Test ID</param>
        /// <param name="attemptNumber">Optional attempt number</param>
        /// <returns>Comprehensive test results with detailed test case results and summary</returns>
        [HttpGet("results/comprehensive")]
        public async Task<IActionResult> GetComprehensiveTestResultsByQuery(
            [FromQuery] long userId, 
            [FromQuery] int codingTestId, 
            [FromQuery] int? attemptNumber = null)
        {
            try
            {
                var request = new GetTestResultsRequest
                {
                    UserId = userId,
                    CodingTestId = codingTestId,
                    AttemptNumber = attemptNumber
                };

                var result = await _codingTestService.GetComprehensiveTestResultsAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve comprehensive test results", details = ex.Message });
            }
        }

        [HttpGet("debug/data")]
        public async Task<IActionResult> GetDebugData(
            [FromQuery] long userId, 
            [FromQuery] int codingTestId, 
            [FromQuery] int? problemId = null)
        {
            try
            {
                var debugInfo = await _codingTestService.GetDebugDataAsync(userId, codingTestId, problemId);
                return Ok(debugInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve debug data", details = ex.Message });
            }
        }
    }
}
