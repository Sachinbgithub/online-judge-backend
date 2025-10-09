# Comprehensive Test Results API

## Overview

The Comprehensive Test Results API provides a unified way to fetch detailed test results for a user and test, combining data from both `CodingTestSubmissions` and `CodingTestSubmissionResults` tables. This API gives you complete visibility into:

- **Final code snapshots** and submission details from `CodingTestSubmissions`
- **Detailed test case results** from `CodingTestSubmissionResults` 
- **Comprehensive analytics** and summary statistics
- **Activity tracking metrics** for behavioral analysis

## API Endpoints

### 1. POST `/api/CodingTest/results/comprehensive`

Get comprehensive test results using a request body.

**Request Body:**
```json
{
  "userId": 40115,
  "codingTestId": 1000019,
  "attemptNumber": 1  // Optional: Get specific attempt
}
```

### 2. GET `/api/CodingTest/results/comprehensive`

Get comprehensive test results using query parameters.

**Query Parameters:**
- `userId` (required): User ID
- `codingTestId` (required): Coding Test ID  
- `attemptNumber` (optional): Specific attempt number

**Example:**
```
GET /api/CodingTest/results/comprehensive?userId=40115&codingTestId=1000019&attemptNumber=1
```

## Response Structure

### ComprehensiveTestResultResponse

```json
{
  "codingTestId": 1000019,
  "testName": "Advanced Programming Test",
  "userId": 40115,
  "totalQuestions": 3,
  "totalMarks": 100,
  "startDate": "2024-01-01T09:00:00Z",
  "endDate": "2024-01-01T12:00:00Z",
  "durationMinutes": 180,
  "problemResults": [
    {
      "problemId": 6,
      "problemTitle": "Two Sum",
      "questionOrder": 1,
      "maxScore": 30,
      "languageUsed": "python",
      "finalCodeSnapshot": "def twoSum(nums, target):\n    # Implementation...",
      "totalTestCases": 5,
      "passedTestCases": 5,
      "failedTestCases": 0,
      "score": 30,
      "isCorrect": true,
      "isLateSubmission": false,
      "submissionTime": "2024-01-01T10:30:00Z",
      "executionTimeMs": 45,
      "memoryUsedKB": 1024,
      "errorMessage": null,
      "errorType": null,
      "languageSwitchCount": 0,
      "runClickCount": 3,
      "submitClickCount": 1,
      "eraseCount": 0,
      "saveCount": 2,
      "loginLogoutCount": 0,
      "isSessionAbandoned": false,
      "questionDetails": {
        "problemId": 6,
        "title": "Two Sum",
        "description": "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target...",
        "examples": "Input: nums = [2,7,11,15], target = 9\nOutput: [0,1]\nExplanation: Because nums[0] + nums[1] == 9, we return [0, 1].",
        "constraints": "2 ≤ nums.length ≤ 10⁴\n-10⁹ ≤ nums[i] ≤ 10⁹\n-10⁹ ≤ target ≤ 10⁹\nOnly one valid answer exists.",
        "hints": "A really brute force way would be to search for all possible pairs of numbers...",
        "timeLimit": 5000,
        "memoryLimit": 256,
        "subdomainId": 1,
        "difficulty": 1,
        "testCases": [
          {
            "id": 1,
            "problemId": 6,
            "input": "[2,7,11,15], 9",
            "expectedOutput": "[0,1]"
          }
          // ... more test cases
        ],
        "starterCodes": [
          {
            "id": 1,
            "problemId": 6,
            "language": "python",
            "code": "def twoSum(nums, target):\n    pass"
          }
          // ... more starter codes
        ]
      },
      "testCaseResults": [
        {
          "resultId": 12345,
          "testCaseId": 1,
          "testCaseOrder": 1,
          "input": "[2,7,11,15], 9",
          "expectedOutput": "[0,1]",
          "actualOutput": "[0,1]",
          "isPassed": true,
          "executionTimeMs": 5,
          "memoryUsedKB": 256,
          "errorMessage": null,
          "errorType": null,
          "createdAt": "2024-01-01T10:30:00Z"
        }
        // ... more test case results
      ]
    }
    // ... more problem results
  ],
  "summary": {
    "totalScore": 85,
    "maxPossibleScore": 100,
    "percentage": 85.0,
    "totalTestCases": 15,
    "passedTestCases": 13,
    "failedTestCases": 2,
    "correctProblems": 2,
    "totalProblems": 3,
    "averageExecutionTimeMs": 42.5,
    "averageMemoryUsedKB": 1156.7,
    "totalLanguageSwitches": 1,
    "totalRunClicks": 12,
    "totalSubmitClicks": 3,
    "totalEraseCount": 2,
    "totalSaveCount": 8,
    "totalLoginLogoutCount": 0,
    "abandonedSessions": 0,
    "lateSubmissions": 0,
    "firstSubmissionTime": "2024-01-01T10:15:00Z",
    "lastSubmissionTime": "2024-01-01T11:45:00Z"
  }
}
```

## Data Sources

### From CodingTestSubmissions Table:
- Final code snapshots
- Submission scores and marks
- Language used
- Submission timestamps
- Activity tracking metrics (run clicks, submit clicks, etc.)
- Error messages and types
- Performance metrics (execution time, memory usage)

### From CodingTestSubmissionResults Table:
- Individual test case results
- Input/output for each test case
- Pass/fail status for each test case
- Execution time and memory usage per test case
- Error details for failed test cases
- Test case order and organization

### From Problems Table (Question Data):
- Complete problem description and examples
- Constraints and hints
- Time and memory limits
- Difficulty level and subdomain information
- All test cases for the problem
- Starter code templates for different languages

## Key Features

### 1. **Complete Test Case Visibility**
- See exactly which test cases passed/failed
- View input/output for each test case
- Analyze execution time and memory usage per test case

### 2. **Comprehensive Analytics**
- Overall test performance summary
- Problem-by-problem breakdown
- Activity tracking metrics
- Performance statistics

### 3. **Flexible Filtering**
- Get results for specific attempts
- Get all attempts for a user/test combination
- Filter by user and test ID

### 4. **Rich Metadata**
- Test configuration details
- Submission timestamps
- Late submission tracking
- Session abandonment detection

## Use Cases

### 1. **Student Performance Analysis**
```javascript
// Get comprehensive results for a student
const results = await fetch('/api/CodingTest/results/comprehensive', {
  method: 'POST',
  body: JSON.stringify({
    userId: 40115,
    codingTestId: 1000019
  })
});

// Analyze performance
console.log(`Score: ${results.summary.totalScore}/${results.summary.maxPossibleScore}`);
console.log(`Problems solved: ${results.summary.correctProblems}/${results.summary.totalProblems}`);
```

### 2. **Detailed Code Review**
```javascript
// Review specific problem solution
const problemResult = results.problemResults.find(p => p.problemId === 6);
console.log('Final Code:', problemResult.finalCodeSnapshot);
console.log('Test Cases:', problemResult.testCaseResults.length);
```

### 3. **Behavioral Analysis**
```javascript
// Analyze student behavior
const summary = results.summary;
console.log(`Total run clicks: ${summary.totalRunClicks}`);
console.log(`Language switches: ${summary.totalLanguageSwitches}`);
console.log(`Session abandoned: ${summary.abandonedSessions > 0}`);
```

### 4. **Test Case Debugging**
```javascript
// Debug failed test cases
const failedTestCases = results.problemResults
  .flatMap(p => p.testCaseResults)
  .filter(tc => !tc.isPassed);

failedTestCases.forEach(tc => {
  console.log(`Test Case ${tc.testCaseOrder}:`);
  console.log(`Input: ${tc.input}`);
  console.log(`Expected: ${tc.expectedOutput}`);
  console.log(`Actual: ${tc.actualOutput}`);
  console.log(`Error: ${tc.errorMessage}`);
});
```

### 5. **Question Analysis and Review**
```javascript
// Analyze question details and student solutions
results.problemResults.forEach(problem => {
  console.log(`\n=== ${problem.questionDetails.title} ===`);
  console.log(`Description: ${problem.questionDetails.description}`);
  console.log(`Constraints: ${problem.questionDetails.constraints}`);
  console.log(`Difficulty: ${problem.questionDetails.difficulty}`);
  console.log(`Student's Solution:`);
  console.log(problem.finalCodeSnapshot);
  console.log(`Score: ${problem.score}/${problem.maxScore}`);
  
  // Compare with starter code
  const pythonStarter = problem.questionDetails.starterCodes
    .find(sc => sc.language === 'python');
  if (pythonStarter) {
    console.log(`Starter Code: ${pythonStarter.code}`);
  }
});
```

## Error Handling

The API returns appropriate HTTP status codes:

- **200 OK**: Successful retrieval
- **400 Bad Request**: Invalid request parameters
- **404 Not Found**: Test or user not found
- **500 Internal Server Error**: Server error

## Performance Considerations

- The API efficiently combines data from both tables using Entity Framework
- Results are ordered by question order for consistent presentation
- Large result sets are handled efficiently with proper indexing
- Consider pagination for very large tests with many submissions

## Testing

Use the provided `TestComprehensiveResults.http` file to test the API endpoints with various scenarios including error cases.

## Integration Notes

- The API leverages the new `ProblemId` column in `CodingTestSubmissionResults` for efficient querying
- Results are grouped by submission and ordered by test case order
- All timestamps are in UTC format
- The API is designed to work with your existing stored procedure approach
