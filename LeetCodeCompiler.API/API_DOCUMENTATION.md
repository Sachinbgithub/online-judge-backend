# LeetCode Compiler API Documentation

## Overview
This API provides comprehensive functionality for managing coding problems, test cases, starter code, languages, domains, subdomains, and problem hints in a LeetCode-style online judge system.

**Base URL:** `http://192.168.0.102:5081/api`
**Swagger UI:** `http://192.168.0.102:5081/swagger/index.html`

---

## Table of Contents
1. [Domains API](#domains-api)
2. [Subdomains API](#subdomains-api)
3. [Problems API](#problems-api)
4. [Test Cases API](#test-cases-api)
5. [Starter Code API](#starter-code-api)
6. [Languages API](#languages-api)
7. [Problem Hints API](#problem-hints-api)

---

## Domains API

### Get All Domains
```http
GET /api/Domain
```

**Response:**
```json
[
  {
    "domainId": 1,
    "domainName": "Data Structures",
    "subdomains": [
      {
        "subdomainId": 1,
        "domainId": 1,
        "subdomainName": "Arrays"
      }
    ]
  }
]
```

### Get Domain by ID
```http
GET /api/Domain/{id}
```

### Create Domain
```http
POST /api/Domain
Content-Type: application/json

{
  "domainName": "Algorithms"
}
```

**Response (201 Created):**
```json
{
  "domainId": 2,
  "domainName": "Algorithms",
  "subdomains": []
}
```

### Update Domain
```http
PUT /api/Domain/{id}
Content-Type: application/json

{
  "domainName": "Advanced Algorithms"
}
```

**Response (200 OK):**
```json
{
  "domainId": 2,
  "domainName": "Advanced Algorithms",
  "subdomains": [
    {
      "subdomainId": 1,
      "domainId": 2,
      "subdomainName": "Dynamic Programming"
    }
  ]
}
```

### Delete Domain
```http
DELETE /api/Domain/{id}
```

**Response (200 OK):**
```json
{
  "message": "Domain 'Algorithms' has been deleted successfully"
}
```

**Note:** Domain cannot be deleted if it has subdomains. Delete all subdomains first.

### Get Domain Usage Statistics
```http
GET /api/Domain/{id}/usage
```

**Response (200 OK):**
```json
{
  "domainId": 2,
  "domainName": "Algorithms",
  "subdomainsCount": 3,
  "problemsCount": 15,
  "isUsed": true
}
```

---

## Subdomains API

### Get All Subdomains
```http
GET /api/Subdomain
```

### Get Subdomain by ID
```http
GET /api/Subdomain/{id}
```

### Get Subdomains by Domain ID
```http
GET /api/Subdomain/domain/{domainId}
```

### Create Subdomain
```http
POST /api/Subdomain
Content-Type: application/json

{
  "domainId": 1,
  "subdomainName": "Linked Lists"
}
```

**Response (201 Created):**
```json
{
  "subdomainId": 2,
  "domainId": 1,
  "subdomainName": "Linked Lists",
  "domain": {
    "domainId": 1,
    "domainName": "Data Structures"
  }
}
```

### Update Subdomain
```http
PUT /api/Subdomain/{id}
Content-Type: application/json

{
  "subdomainName": "Advanced Linked Lists"
}
```

**Response (200 OK):**
```json
{
  "subdomainId": 2,
  "domainId": 1,
  "subdomainName": "Advanced Linked Lists",
  "domain": {
    "domainId": 1,
    "domainName": "Data Structures"
  }
}
```

### Delete Subdomain
```http
DELETE /api/Subdomain/{id}
```

**Response (200 OK):**
```json
{
  "message": "Subdomain 'Linked Lists' has been deleted successfully"
}
```

**Note:** Subdomain cannot be deleted if it has problems. Delete all problems first.

### Get Subdomain Usage Statistics
```http
GET /api/Subdomain/{id}/usage
```

**Response (200 OK):**
```json
{
  "subdomainId": 2,
  "subdomainName": "Linked Lists",
  "domainId": 1,
  "domainName": "Data Structures",
  "problemsCount": 5,
  "testCasesCount": 15,
  "starterCodesCount": 10,
  "isUsed": true
}
```

---

## Problems API

### Get All Problems
```http
GET /api/Problems
```

### Get Problem by ID
```http
GET /api/Problems/{id}
```

### Create Problem
```http
POST /api/Problems
Content-Type: application/json

{
  "title": "Two Sum",
  "description": "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target.",
  "examples": "Input: nums = [2,7,11,15], target = 9\nOutput: [0,1]",
  "constraints": "2 <= nums.length <= 10^4\n-10^9 <= nums[i] <= 10^9",
  "hints": 1,
  "timeLimit": 5,
  "memoryLimit": 256,
  "subdomainId": 1,
  "difficulty": 1
}
```

**Response (201 Created):**
```json
{
  "id": 1,
  "title": "Two Sum",
  "description": "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target.",
  "examples": "Input: nums = [2,7,11,15], target = 9\nOutput: [0,1]",
  "constraints": "2 <= nums.length <= 10^4\n-10^9 <= nums[i] <= 10^9",
  "hints": 1,
  "timeLimit": 5,
  "memoryLimit": 256,
  "subdomainId": 1,
  "difficulty": 1,
  "subdomainName": "Arrays",
  "domainName": "Data Structures",
  "testCases": [],
  "starterCodes": []
}
```

### Update Problem
```http
PUT /api/Problems/{id}
Content-Type: application/json

{
  "title": "Two Sum - Advanced",
  "description": "Updated description with more details...",
  "examples": "Updated examples...",
  "constraints": "Updated constraints...",
  "hints": 2,
  "timeLimit": 10,
  "memoryLimit": 512,
  "subdomainId": 1,
  "difficulty": 2
}
```

**Response (200 OK):**
```json
{
  "id": 1,
  "title": "Two Sum - Advanced",
  "description": "Updated description with more details...",
  "examples": "Updated examples...",
  "constraints": "Updated constraints...",
  "hints": 2,
  "timeLimit": 10,
  "memoryLimit": 512,
  "subdomainId": 1,
  "difficulty": 2,
  "subdomainName": "Arrays",
  "domainName": "Data Structures",
  "testCases": [...],
  "starterCodes": [...]
}
```

### Delete Problem
```http
DELETE /api/Problems/{id}
```

**Response (200 OK):**
```json
{
  "message": "Problem 'Two Sum' has been deleted successfully",
  "deletedData": {
    "testCases": 5,
    "starterCodes": 3,
    "problemHints": 2
  }
}
```

**Note:** Deleting a problem will cascade delete all related test cases, starter codes, and problem hints.

### Get Problem Usage Statistics
```http
GET /api/Problems/{id}/usage
```

**Response (200 OK):**
```json
{
  "problemId": 1,
  "title": "Two Sum",
  "subdomainId": 1,
  "subdomainName": "Arrays",
  "domainId": 1,
  "domainName": "Data Structures",
  "testCasesCount": 5,
  "starterCodesCount": 3,
  "problemHintsCount": 2,
  "difficulty": 1,
  "timeLimit": 5,
  "memoryLimit": 256,
  "isUsed": true
}
```

**Validation Rules:**
- `title`: Required, string
- `description`: Required, string
- `examples`: Required, string
- `constraints`: Required, string
- `hints`: Optional, integer (nullable)
- `timeLimit`: Required, integer (1-60 seconds), default: 5
- `memoryLimit`: Required, integer (64-1024 MB), default: 256
- `subdomainId`: Required, integer, default: 9
- `difficulty`: Required, integer (1-3)

---

## Test Cases API

### Get All Test Cases
```http
GET /api/TestCase
```

### Get Test Case by ID
```http
GET /api/TestCase/{id}
```

### Get Test Cases by Problem ID
```http
GET /api/TestCase/problem/{problemId}
```

### Create Test Case
```http
POST /api/TestCase
Content-Type: application/json

{
  "problemId": 1,
  "input": "[2,7,11,15]",
  "expectedOutput": "[0,1]"
}
```

**Response (201 Created):**
```json
{
  "id": 1,
  "problemId": 1,
  "input": "[2,7,11,15]",
  "expectedOutput": "[0,1]",
  "problemTitle": "Two Sum"
}
```

### Update Test Case
```http
PUT /api/TestCase/{id}
Content-Type: application/json

{
  "input": "[1,2,3,4,5]",
  "expectedOutput": "[0,1]"
}
```

### Delete Test Case
```http
DELETE /api/TestCase/{id}
```

### Delete All Test Cases for Problem
```http
DELETE /api/TestCase/problem/{problemId}
```

**Validation Rules:**
- `problemId`: Required, integer
- `input`: Optional, string (max 255 characters)
- `expectedOutput`: Optional, string (max 255 characters)

---

## Starter Code API

### Get All Starter Codes
```http
GET /api/StarterCode
```

### Get Starter Code by ID
```http
GET /api/StarterCode/{id}
```

### Get Starter Codes by Problem ID
```http
GET /api/StarterCode/problem/{problemId}
```

### Create Starter Code
```http
POST /api/StarterCode
Content-Type: application/json

{
  "problemId": 1,
  "language": 1,
  "code": "def twoSum(nums, target):\n    # Your code here\n    pass"
}
```

**Response (201 Created):**
```json
{
  "id": 1,
  "problemId": 1,
  "language": 1,
  "code": "def twoSum(nums, target):\n    # Your code here\n    pass",
  "problemTitle": "Two Sum"
}
```

### Update Starter Code
```http
PUT /api/StarterCode/{id}
Content-Type: application/json

{
  "code": "def twoSum(nums, target):\n    for i in range(len(nums)):\n        for j in range(i+1, len(nums)):\n            if nums[i] + nums[j] == target:\n                return [i, j]\n    return []"
}
```

### Delete Starter Code
```http
DELETE /api/StarterCode/{id}
```

**Validation Rules:**
- `problemId`: Required, integer
- `language`: Required, integer (1-10)
- `code`: Required, string

**Language IDs:**
- 1: Python
- 2: Java
- 3: JavaScript
- 4: C++
- 5: C

---

## Languages API

### Get All Languages
```http
GET /api/Languages
```

**Response:**
```json
[
  {
    "id": 1,
    "languageName": "Python"
  },
  {
    "id": 2,
    "languageName": "Java"
  },
  {
    "id": 3,
    "languageName": "JavaScript"
  },
  {
    "id": 4,
    "languageName": "C++"
  },
  {
    "id": 5,
    "languageName": "C"
  }
]
```

### Get Language by ID
```http
GET /api/Languages/{id}
```

### Get Language by Name
```http
GET /api/Languages/name/{name}
```

### Create Language
```http
POST /api/Languages
Content-Type: application/json

{
  "languageName": "Go"
}
```

### Update Language
```http
PUT /api/Languages/{id}
Content-Type: application/json

{
  "languageName": "Golang"
}
```

### Delete Language
```http
DELETE /api/Languages/{id}
```

### Get Language Usage Statistics
```http
GET /api/Languages/{id}/usage
```

**Response:**
```json
{
  "languageId": 1,
  "languageName": "Python",
  "starterCodeCount": 15,
  "problemsCount": 8,
  "isUsed": true
}
```

### Get All Languages with Usage Statistics
```http
GET /api/Languages/with-usage
```

**Validation Rules:**
- `languageName`: Required, string (max 50 characters)

---

## Problem Hints API

### Get All Problem Hints
```http
GET /api/ProblemHints
```

### Get Problem Hint by ID
```http
GET /api/ProblemHints/{id}
```

### Get Problem Hints by Problem ID
```http
GET /api/ProblemHints/problem/{problemId}
```

### Create Problem Hint
```http
POST /api/ProblemHints
Content-Type: application/json

{
  "problemId": 1,
  "hint": "Try using a hash map to store the complement of each number as you iterate through the array."
}
```

**Response (201 Created):**
```json
{
  "id": 1,
  "problemId": 1,
  "hint": "Try using a hash map to store the complement of each number as you iterate through the array.",
  "problemTitle": "Two Sum"
}
```

### Update Problem Hint
```http
PUT /api/ProblemHints/{id}
Content-Type: application/json

{
  "hint": "Use a two-pointer approach for better time complexity."
}
```

### Delete Problem Hint
```http
DELETE /api/ProblemHints/{id}
```

### Delete All Problem Hints for Problem
```http
DELETE /api/ProblemHints/problem/{problemId}
```

### Get Problem Hints Count
```http
GET /api/ProblemHints/problem/{problemId}/count
```

**Response:**
```json
{
  "problemId": 1,
  "problemTitle": "Two Sum",
  "hintsCount": 3
}
```

**Validation Rules:**
- `problemId`: Required, integer
- `hint`: Required, string (max 1000 characters)

---

## Error Responses

### 400 Bad Request
```json
{
  "error": "Validation failed",
  "details": ["Title is required", "Difficulty must be between 1 and 3"]
}
```

### 404 Not Found
```json
{
  "error": "Problem with ID 999 not found"
}
```

### 409 Conflict
```json
{
  "error": "Problem with title 'Two Sum' already exists in subdomain 'Arrays'"
}
```

### 500 Internal Server Error
```json
{
  "error": "An error occurred while creating the problem",
  "details": "Database connection failed"
}
```

---

## Common HTTP Status Codes

- **200 OK**: Request successful
- **201 Created**: Resource created successfully
- **400 Bad Request**: Invalid request data
- **404 Not Found**: Resource not found
- **409 Conflict**: Resource already exists
- **500 Internal Server Error**: Server error

---

## Difficulty API

### Get All Difficulties
```http
GET /api/Difficulty
```

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "difficultyId": 1,
    "difficultyName": "Easy"
  },
  {
    "id": 2,
    "difficultyId": 2,
    "difficultyName": "Medium"
  },
  {
    "id": 3,
    "difficultyId": 3,
    "difficultyName": "Hard"
  }
]
```

### Get Difficulty by ID
```http
GET /api/Difficulty/{id}
```

### Get Difficulty by DifficultyId
```http
GET /api/Difficulty/by-difficulty-id/{difficultyId}
```

### Create Difficulty
```http
POST /api/Difficulty
Content-Type: application/json

{
  "difficultyId": 4,
  "difficultyName": "Expert"
}
```

**Response (201 Created):**
```json
{
  "id": 4,
  "difficultyId": 4,
  "difficultyName": "Expert"
}
```

### Update Difficulty
```http
PUT /api/Difficulty/{id}
Content-Type: application/json

{
  "difficultyId": 4,
  "difficultyName": "Advanced"
}
```

**Response (200 OK):**
```json
{
  "id": 4,
  "difficultyId": 4,
  "difficultyName": "Advanced"
}
```

### Delete Difficulty
```http
DELETE /api/Difficulty/{id}
```

**Response (200 OK):**
```json
{
  "message": "Difficulty 'Advanced' has been deleted successfully"
}
```

**Note:** Difficulty cannot be deleted if it is being used by problems. Update or delete those problems first.

### Get Difficulty Usage Statistics
```http
GET /api/Difficulty/{id}/usage
```

**Response (200 OK):**
```json
{
  "id": 1,
  "difficultyId": 1,
  "difficultyName": "Easy",
  "problemsCount": 15,
  "testCasesCount": 45,
  "starterCodesCount": 30,
  "isUsed": true
}
```

**Validation Rules:**
- `difficultyId`: Required, integer between 1-10
- `difficultyName`: Required, string, max 50 characters
- Duplicate `difficultyId` and `difficultyName` are not allowed

---

## Frontend Integration Notes

### API Base Configuration
```javascript
const API_BASE_URL = 'http://192.168.0.101:5081/api';

// Example fetch configuration
const fetchConfig = {
  headers: {
    'Content-Type': 'application/json',
  },
};
```

### Example API Service Class
```javascript
class LeetCodeAPI {
  constructor(baseURL = 'http://192.168.0.101:5081/api') {
    this.baseURL = baseURL;
  }

  async getProblems() {
    const response = await fetch(`${this.baseURL}/Problems`);
    return response.json();
  }

  async createProblem(problemData) {
    const response = await fetch(`${this.baseURL}/Problems`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(problemData),
    });
    return response.json();
  }

  async getTestCases(problemId) {
    const response = await fetch(`${this.baseURL}/TestCase/problem/${problemId}`);
    return response.json();
  }

  async createTestCase(testCaseData) {
    const response = await fetch(`${this.baseURL}/TestCase`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(testCaseData),
    });
    return response.json();
  }
}
```

### Data Flow Recommendations

1. **Load Domains/Subdomains** first for dropdowns
2. **Load Languages** for language selection
3. **Load Problems** with pagination
4. **Load Test Cases** and **Starter Code** when viewing a specific problem
5. **Load Problem Hints** when user requests hints

### Form Validation

Implement client-side validation matching the API validation rules:
- Required fields
- String length limits
- Numeric ranges (difficulty: 1-3, timeLimit: 1-60, etc.)
- Foreign key validation (check if subdomain/language exists)

---

## Testing

You can test all endpoints using the Swagger UI at:
`http://192.168.0.102:5081/swagger/index.html`

Or use the provided test files:
- `TestDomainInsertion.http`
- `TestSubdomainInsertion.http`
- `TestProblemInsertion.http`
- `TestTestCaseAPI.http`
- `TestStarterCodeInsertion.http`
- `TestLanguagesAPI.http`
- `TestProblemHintsAPI.http`

---

## Coding Test API

**Base:** `http://192.168.0.102:5081/api/CodingTest`

### Create Coding Test
```http
POST /api/CodingTest
Content-Type: application/json

{
  "testName": "DSA Mock Test 1",
  "createdBy": 101,
  "startDate": "2026-04-01T09:00:00",
  "endDate": "2026-04-01T11:00:00",
  "durationMinutes": 120,
  "totalQuestions": 5,
  "totalMarks": 100,
  "testType": 1002,
  "isGlobal": false,
  "collegeId": 10,
  "classId": 500,
  "topicData": [
    { "sectionId": 1, "domainId": 2, "subdomainId": 9 }
  ],
  "questions": [
    { "problemId": 1, "questionOrder": 1, "marks": 20, "timeLimitMinutes": 30 }
  ]
}
```

### Get Coding Test by ID
```http
GET /api/CodingTest/{id}
```

### Get All Coding Tests (Paged)
```http
GET /api/CodingTest?pageNumber=1&pageSize=10
```

### Get Tests Created by a User
```http
GET /api/CodingTest/created-by/{createdBy}?pageNumber=1&pageSize=10
```

### Get Tests by Filter
```http
GET /api/CodingTest/filter?codingTestId=&createdBy=101&classId=500&collegeId=10&assignedToUserId=&pageNumber=1&pageSize=10
```

### Get Tests Available to a User
```http
GET /api/CodingTest/user/{userId}?pageNumber=1&pageSize=10&subjectName=&topicName=&isEnabled=true
```

### Get Tests for a College
```http
GET /api/CodingTest/college/{collegeId}             # College-specific tests
GET /api/CodingTest/college/{collegeId}/available   # Global + college-specific
GET /api/CodingTest/college/{collegeId}/global      # Only global tests for college
GET /api/CodingTest/global                          # All global tests
```

### Update / Delete / Publish
```http
PUT    /api/CodingTest               # Update test (body: UpdateCodingTestRequest)
DELETE /api/CodingTest/{id}
POST   /api/CodingTest/{id}/publish
POST   /api/CodingTest/{id}/unpublish
```

### Start a Test (Student)
```http
POST /api/CodingTest/start
Content-Type: application/json

{
  "codingTestId": 1000017,
  "userId": 40115,
  "accessCode": ""
}
```

### Submit a Whole Test
```http
POST /api/CodingTest/submit-whole-test
Content-Type: application/json

{
  "userId": 40115,
  "codingTestId": 1000017,
  "attemptNumber": 1,
  "questionSubmissions": [
    {
      "problemId": 1,
      "languageUsed": "python",
      "finalCodeSnapshot": "def twoSum(nums, target): ...",
      "totalTestCases": 3,
      "passedTestCases": 2,
      "failedTestCases": 1
    }
  ],
  "totalTimeSpentMinutes": 45,
  "isLateSubmission": false,
  "classId": 500
}
```

### Submit a Single Question
```http
POST /api/CodingTest/submit
Content-Type: application/json

{
  "userId": 40115,
  "codingTestId": 1000017,
  "problemId": 1,
  "attemptNumber": 1,
  "languageUsed": "java",
  "finalCodeSnapshot": "...",
  "totalTestCases": 3,
  "passedTestCases": 3,
  "failedTestCases": 0
}
```

### End a Test
```http
POST /api/CodingTest/end-test
Content-Type: application/json

{
  "userId": 40115,
  "codingTestId": 1000017,
  "status": "Completed",
  "timeSpentMinutes": 90,
  "isLateSubmission": false
}
```

### Get Combined Results (Faculty View)
```http
GET /api/CodingTest/combined-results?userId=40115&codingTestId=1000017
```

### Assign a Test to a User
```http
POST /api/CodingTest/assign
Content-Type: application/json

{
  "codingTestId": 1000017,
  "assignedToUserId": 40115,
  "assignedToUserType": 25,
  "assignedByUserId": 101,
  "testType": 1002,
  "testMode": 5
}
```

### Get Assigned Tests for a User
```http
GET /api/CodingTest/assigned/user/{userId}?userType=25&pageNumber=1&pageSize=10&testType=1002
```

### Unassign a Test
```http
DELETE /api/CodingTest/assigned/{assignedId}?unassignedByUserId=101
```

### Submissions
```http
POST /api/CodingTest/submissions              # Get submissions (filter body)
GET  /api/CodingTest/submissions/{submissionId}
GET  /api/CodingTest/submissions/{submissionId}/test-cases
GET  /api/CodingTest/{codingTestId}/statistics
```

### Attempt Management
```http
GET  /api/CodingTest/attempt/{attemptId}
GET  /api/CodingTest/user/{userId}/test/{codingTestId}/attempts
POST /api/CodingTest/attempt/{attemptId}/abandon      # body: userId (int)
```

### Question Attempts
```http
POST /api/CodingTest/attempt/{codingTestAttemptId}/question/{questionId}/start   # body: userId
GET  /api/CodingTest/question-attempt/{questionAttemptId}
GET  /api/CodingTest/attempt/{codingTestAttemptId}/questions
POST /api/CodingTest/question/submit
```

### Validation
```http
POST /api/CodingTest/{id}/validate-access       # body: "accessCode"
GET  /api/CodingTest/{id}/can-attempt?userId=40115
GET  /api/CodingTest/{id}/is-active
GET  /api/CodingTest/{id}/is-expired
```

### Analytics
```http
GET /api/CodingTest/{id}/analytics
GET /api/CodingTest/{id}/results?pageNumber=1&pageSize=10
GET /api/CodingTest/status/{status}?pageNumber=1&pageSize=10   # status: Active, Completed, Expired
```

---

## Faculty Dashboard API

**Base:** `http://192.168.0.102:5081/api/FacultyDashboard`

```http
GET /api/FacultyDashboard/{codingTestId}/students?pageNumber=1&pageSize=10
GET /api/FacultyDashboard/{codingTestId}/leaderboard?pageNumber=1&pageSize=10
GET /api/FacultyDashboard/{practiceTestId}/practice-students?pageNumber=1&pageSize=10
GET /api/FacultyDashboard/{codingTestId}/problem-analysis?pageNumber=1&pageSize=10
```

---

## Faculty Performance API

**Base:** `http://192.168.0.102:5081/api/FacultyPerformance`

```http
GET /api/FacultyPerformance/{codingTestId}/students?pageNumber=1&pageSize=10
GET /api/FacultyPerformance/{codingTestId}/leaderboard?pageNumber=1&pageSize=10
GET /api/FacultyPerformance/{codingTestId}/problem-analysis?pageNumber=1&pageSize=10
```

---

## Faculty User Performance API

**Base:** `http://192.168.0.102:5081/api/FacultyUserPerformance`

```http
GET /api/FacultyUserPerformance/{codingTestId}/user/{userId}?pageNumber=1&pageSize=10
```

---

## Student Performance API

**Base:** `http://192.168.0.102:5081/api/StudentPerformance`

```http
GET /api/StudentPerformance/{userId}?pageNumber=1&pageSize=10
```

---

## Practice Test API

**Base:** `http://192.168.0.102:5081/api/PracticeTest`

```http
GET  /api/PracticeTest/{id}
GET  /api/PracticeTest/user/{userId}
POST /api/PracticeTest                    # Create practice test
POST /api/PracticeTest/{id}/submit        # Submit practice test
GET  /api/PracticeTest/{id}/results
```

---

## User Activity API

**Base:** `http://192.168.0.102:5081/api/UserActivity`

```http
POST /api/UserActivity/log
Content-Type: application/json

{
  "userId": 40115,
  "problemId": 1,
  "action": "code_execution",
  "metadata": { "language": "python", "runtimeMs": 150 }
}
```

---

## Code Execution API

**Base:** `http://192.168.0.102:5081/api/CodeExecution`

```http
POST /api/CodeExecution
Content-Type: application/json

{
  "language": "python",
  "code": "def twoSum(nums, target):\n    for i in range(len(nums)):\n        for j in range(i+1, len(nums)):\n            if nums[i] + nums[j] == target:\n                return [i,j]",
  "testCases": [
    {
      "id": 1,
      "problemId": 1,
      "input": "[2,7,11,15]\n9",
      "expectedOutput": "[0,1]"
    }
  ]
}
```

**Response:**
```json
{
  "results": [
    {
      "input": "[2,7,11,15]\n9",
      "output": "[0,1]",
      "expected": "[0,1]",
      "passed": true,
      "runtimeMs": 0.15,
      "memoryMb": 8.2,
      "error": null
    }
  ],
  "executionTime": 0.15
}
```

**Supported languages:** `python`, `javascript`, `java`, `cpp`, `c`

---

## Health Check

```http
GET /health
```

**Response:**
```json
{
  "status": "Healthy",
  "checks": [
    { "name": "code-execution", "status": "Healthy", "duration": "00:00:00.001" }
  ]
}
```
