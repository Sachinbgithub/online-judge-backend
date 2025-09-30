# 🧪 Coding Test API Documentation

## 📋 Overview

The Coding Test API provides a comprehensive system for creating, managing, and conducting coding tests. This system allows educators to create tests with multiple programming problems, track student attempts, and analyze performance.

## 🏗️ Architecture

### **Database Models**

#### **CodingTest**
- Main test entity containing test configuration and metadata
- Links to questions and attempts
- Supports access codes, time limits, and multiple attempt policies

#### **CodingTestQuestion**
- Links existing problems to tests
- Defines question order, marks, and time limits
- References existing Problem entities

#### **CodingTestAttempt**
- Tracks individual student test attempts
- Records start/end times, scores, and completion status
- Links to question attempts

#### **CodingTestQuestionAttempt**
- Tracks individual question attempts within a test
- Records code submissions, execution results, and scores
- Integrates with existing code execution system

---

## 🎯 API Endpoints

### **Test Management**

#### **Create Coding Test**
```http
POST /api/CodingTest
Content-Type: application/json

{
  "testName": "Data Structures Practice Test",
  "description": "Test covering arrays, linked lists, and trees",
  "createdBy": 101,
  "startDate": "2024-01-20T10:00:00Z",
  "endDate": "2024-01-20T12:00:00Z",
  "durationMinutes": 120,
  "totalQuestions": 5,
  "totalMarks": 100,
  "testType": "Practice",
  "difficulty": "Medium",
  "category": "Data Structures",
  "instructions": "Solve all problems within the time limit",
  "allowMultipleAttempts": true,
  "maxAttempts": 3,
  "showResultsImmediately": true,
  "allowCodeReview": false,
  "accessCode": "TEST123",
  "tags": "arrays,linked-lists,trees",
  "questions": [
    {
      "problemId": 1,
      "questionOrder": 1,
      "marks": 20,
      "timeLimitMinutes": 30,
      "customInstructions": "Focus on time complexity"
    },
    {
      "problemId": 2,
      "questionOrder": 2,
      "marks": 25,
      "timeLimitMinutes": 25,
      "customInstructions": "Consider edge cases"
    }
  ]
}
```

#### **Get Coding Test**
```http
GET /api/CodingTest/{id}
```

#### **Get All Coding Tests**
```http
GET /api/CodingTest
```

#### **Get User's Coding Tests**
```http
GET /api/CodingTest/user/{userId}
```

#### **Update Coding Test**
```http
PUT /api/CodingTest
Content-Type: application/json

{
  "id": 1,
  "testName": "Updated Test Name",
  "description": "Updated description",
  "isActive": true,
  "isPublished": true
}
```

#### **Delete Coding Test**
```http
DELETE /api/CodingTest/{id}
```

#### **Publish/Unpublish Test**
```http
POST /api/CodingTest/{id}/publish
POST /api/CodingTest/{id}/unpublish
```

---

### **Test Attempts**

#### **Start Coding Test**
```http
POST /api/CodingTest/start
Content-Type: application/json

{
  "codingTestId": 1,
  "userId": 201,
  "accessCode": "TEST123"
}
```

#### **Get Test Attempt**
```http
GET /api/CodingTest/attempt/{attemptId}
```

#### **Get User's Test Attempts**
```http
GET /api/CodingTest/user/{userId}/test/{codingTestId}/attempts
```

#### **Submit Coding Test**
```http
POST /api/CodingTest/submit
Content-Type: application/json

{
  "codingTestAttemptId": 1,
  "userId": 201,
  "notes": "Completed all questions"
}
```

#### **Abandon Test**
```http
POST /api/CodingTest/attempt/{attemptId}/abandon
Content-Type: application/json

201
```

---

### **Question Attempts**

#### **Start Question Attempt**
```http
POST /api/CodingTest/attempt/{codingTestAttemptId}/question/{questionId}/start
Content-Type: application/json

201
```

#### **Get Question Attempt**
```http
GET /api/CodingTest/question-attempt/{questionAttemptId}
```

#### **Get Question Attempts for Test**
```http
GET /api/CodingTest/attempt/{codingTestAttemptId}/questions
```

#### **Submit Question**
```http
POST /api/CodingTest/question/submit
Content-Type: application/json

{
  "codingTestQuestionAttemptId": 1,
  "userId": 201,
  "languageUsed": "python",
  "codeSubmitted": "def solution(nums): return sum(nums)",
  "runCount": 3,
  "submitCount": 1
}
```

---

### **Analytics & Reports**

#### **Get Tests by Status**
```http
GET /api/CodingTest/status/{status}
# status: upcoming, active, completed, expired
```

#### **Get Tests by Category**
```http
GET /api/CodingTest/category/{category}
```

#### **Get Tests by Difficulty**
```http
GET /api/CodingTest/difficulty/{difficulty}
```

#### **Get Test Analytics**
```http
GET /api/CodingTest/{id}/analytics
```

#### **Get Test Results**
```http
GET /api/CodingTest/{id}/results
```

---

### **Validation**

#### **Validate Access Code**
```http
POST /api/CodingTest/{id}/validate-access
Content-Type: application/json

"TEST123"
```

#### **Check if User Can Attempt**
```http
GET /api/CodingTest/{id}/can-attempt?userId=201
```

#### **Check Test Status**
```http
GET /api/CodingTest/{id}/is-active
GET /api/CodingTest/{id}/is-expired
```

---

## 📊 Response Examples

### **CodingTestResponse**
```json
{
  "id": 1,
  "testName": "Data Structures Practice Test",
  "description": "Test covering arrays, linked lists, and trees",
  "createdBy": 101,
  "createdAt": "2024-01-15T10:00:00Z",
  "updatedAt": null,
  "startDate": "2024-01-20T10:00:00Z",
  "endDate": "2024-01-20T12:00:00Z",
  "durationMinutes": 120,
  "totalQuestions": 5,
  "totalMarks": 100,
  "isActive": true,
  "isPublished": true,
  "testType": "Practice",
  "difficulty": "Medium",
  "category": "Data Structures",
  "instructions": "Solve all problems within the time limit",
  "allowMultipleAttempts": true,
  "maxAttempts": 3,
  "showResultsImmediately": true,
  "allowCodeReview": false,
  "accessCode": "TEST123",
  "tags": "arrays,linked-lists,trees",
  "questions": [
    {
      "id": 1,
      "codingTestId": 1,
      "problemId": 1,
      "questionOrder": 1,
      "marks": 20,
      "timeLimitMinutes": 30,
      "customInstructions": "Focus on time complexity",
      "createdAt": "2024-01-15T10:00:00Z",
      "problem": {
        "id": 1,
        "title": "Two Sum",
        "description": "Given an array of integers...",
        "examples": "Input: nums = [2,7,11,15], target = 9...",
        "constraints": "2 ≤ nums.length ≤ 10⁴...",
        "difficulty": "Easy",
        "testCases": [
          {
            "id": 1,
            "problemId": 1,
            "input": "[2,7,11,15], 9",
            "expectedOutput": "[0,1]"
          }
        ]
      }
    }
  ],
  "totalAttempts": 15,
  "completedAttempts": 12
}
```

### **CodingTestAttemptResponse**
```json
{
  "id": 1,
  "codingTestId": 1,
  "userId": 201,
  "attemptNumber": 1,
  "startedAt": "2024-01-20T10:00:00Z",
  "completedAt": "2024-01-20T11:45:00Z",
  "submittedAt": "2024-01-20T11:45:00Z",
  "status": "Submitted",
  "totalScore": 85,
  "maxScore": 100,
  "percentage": 85.0,
  "timeSpentMinutes": 105,
  "isLateSubmission": false,
  "notes": "Completed all questions",
  "createdAt": "2024-01-20T10:00:00Z",
  "updatedAt": "2024-01-20T11:45:00Z",
  "questionAttempts": [
    {
      "id": 1,
      "codingTestAttemptId": 1,
      "codingTestQuestionId": 1,
      "problemId": 1,
      "userId": 201,
      "startedAt": "2024-01-20T10:00:00Z",
      "completedAt": "2024-01-20T10:25:00Z",
      "status": "Completed",
      "languageUsed": "python",
      "codeSubmitted": "def solution(nums, target): ...",
      "score": 20,
      "maxScore": 20,
      "testCasesPassed": 5,
      "totalTestCases": 5,
      "executionTime": 0.15,
      "runCount": 3,
      "submitCount": 1,
      "isCorrect": true,
      "errorMessage": "",
      "createdAt": "2024-01-20T10:00:00Z",
      "updatedAt": "2024-01-20T10:25:00Z",
      "problem": {
        "id": 1,
        "title": "Two Sum",
        "description": "Given an array of integers...",
        "examples": "Input: nums = [2,7,11,15], target = 9...",
        "constraints": "2 ≤ nums.length ≤ 10⁴...",
        "difficulty": "Easy",
        "testCases": [...]
      }
    }
  ]
}
```

### **CodingTestSummaryResponse**
```json
{
  "id": 1,
  "testName": "Data Structures Practice Test",
  "description": "Test covering arrays, linked lists, and trees",
  "startDate": "2024-01-20T10:00:00Z",
  "endDate": "2024-01-20T12:00:00Z",
  "durationMinutes": 120,
  "totalQuestions": 5,
  "totalMarks": 100,
  "isActive": true,
  "isPublished": true,
  "testType": "Practice",
  "difficulty": "Medium",
  "category": "Data Structures",
  "status": "Active",
  "totalAttempts": 15,
  "completedAttempts": 12,
  "averageScore": 78.5,
  "createdAt": "2024-01-15T10:00:00Z"
}
```

---

## 🔄 Complete Test Workflow

### **Phase 1: Test Creation**
1. **Create Test** → `POST /api/CodingTest`
2. **Add Questions** → Include questions in creation request
3. **Publish Test** → `POST /api/CodingTest/{id}/publish`

### **Phase 2: Student Test Taking**
1. **Start Test** → `POST /api/CodingTest/start`
2. **Start Question** → `POST /api/CodingTest/attempt/{attemptId}/question/{questionId}/start`
3. **Submit Question** → `POST /api/CodingTest/question/submit`
4. **Submit Test** → `POST /api/CodingTest/submit`

### **Phase 3: Results & Analytics**
1. **View Results** → `GET /api/CodingTest/{id}/results`
2. **Get Analytics** → `GET /api/CodingTest/{id}/analytics`
3. **Review Attempts** → `GET /api/CodingTest/attempt/{attemptId}`

---

## 🎯 Key Features

### **Test Configuration**
- **Flexible Scheduling**: Start/end dates with time zones
- **Multiple Attempts**: Configurable attempt limits
- **Access Control**: Optional access codes
- **Time Management**: Overall and per-question time limits
- **Scoring System**: Configurable marks per question

### **Student Experience**
- **Progressive Testing**: Start questions individually
- **Real-time Feedback**: Immediate test case results
- **Code Review**: Optional code visibility after completion
- **Resume Capability**: Continue interrupted tests
- **Late Submission**: Automatic detection and marking

### **Analytics & Reporting**
- **Performance Metrics**: Average scores, completion rates
- **Time Analysis**: Time spent per question and overall
- **Attempt Tracking**: Multiple attempt comparison
- **Category Analysis**: Performance by problem type
- **Difficulty Analysis**: Performance by difficulty level

### **Integration Features**
- **Existing Problems**: Reuses existing problem database
- **Code Execution**: Integrates with existing execution system
- **Activity Tracking**: Links with user activity logs
- **Security**: Access codes and attempt validation

---

## 🚀 Usage Examples

### **Creating a Practice Test**
```http
POST /api/CodingTest
Content-Type: application/json

{
  "testName": "Weekly Practice - Arrays",
  "description": "Practice problems on array manipulation",
  "createdBy": 101,
  "startDate": "2024-01-22T09:00:00Z",
  "endDate": "2024-01-22T23:59:59Z",
  "durationMinutes": 60,
  "totalQuestions": 3,
  "totalMarks": 60,
  "testType": "Practice",
  "difficulty": "Easy",
  "category": "Arrays",
  "instructions": "Solve all problems. You can run your code multiple times.",
  "allowMultipleAttempts": true,
  "maxAttempts": 5,
  "showResultsImmediately": true,
  "allowCodeReview": true,
  "accessCode": "",
  "tags": "arrays,loops,conditionals",
  "questions": [
    {
      "problemId": 1,
      "questionOrder": 1,
      "marks": 20,
      "timeLimitMinutes": 20,
      "customInstructions": "Focus on efficiency"
    },
    {
      "problemId": 2,
      "questionOrder": 2,
      "marks": 20,
      "timeLimitMinutes": 20,
      "customInstructions": "Handle edge cases"
    },
    {
      "problemId": 3,
      "questionOrder": 3,
      "marks": 20,
      "timeLimitMinutes": 20,
      "customInstructions": "Optimize space complexity"
    }
  ]
}
```

### **Student Taking Test**
```http
# 1. Start the test
POST /api/CodingTest/start
{
  "codingTestId": 1,
  "userId": 201,
  "accessCode": ""
}

# 2. Start first question
POST /api/CodingTest/attempt/1/question/1/start
201

# 3. Submit first question
POST /api/CodingTest/question/submit
{
  "codingTestQuestionAttemptId": 1,
  "userId": 201,
  "languageUsed": "python",
  "codeSubmitted": "def solution(nums): return sum(nums)",
  "runCount": 2,
  "submitCount": 1
}

# 4. Submit entire test
POST /api/CodingTest/submit
{
  "codingTestAttemptId": 1,
  "userId": 201,
  "notes": "Completed successfully"
}
```

### **Getting Test Results**
```http
# Get all results for a test
GET /api/CodingTest/1/results

# Get analytics
GET /api/CodingTest/1/analytics

# Get specific attempt details
GET /api/CodingTest/attempt/1
```

---

## 🔧 Technical Implementation

### **Database Schema**
- **CodingTests**: Main test configuration
- **CodingTestQuestions**: Test-problem relationships
- **CodingTestAttempts**: Student test attempts
- **CodingTestQuestionAttempts**: Individual question attempts

### **Service Architecture**
- **ICodingTestService**: Service interface
- **CodingTestService**: Business logic implementation
- **CodingTestController**: API endpoint controller
- **DTOs**: Request/response data transfer objects

### **Integration Points**
- **Existing Problems**: Reuses Problem and TestCase entities
- **Code Execution**: Integrates with existing execution services
- **Activity Tracking**: Links with UserCodingActivityLog
- **Authentication**: Uses existing JWT authentication

### **Performance Considerations**
- **Eager Loading**: Includes related entities in queries
- **Pagination**: Supports large result sets
- **Caching**: Can integrate with existing memory cache
- **Async Operations**: All database operations are async

---

## 📈 Future Enhancements

### **Short-term**
- **Code Execution Integration**: Full integration with existing execution system
- **Real-time Updates**: WebSocket support for live test monitoring
- **Advanced Analytics**: More detailed performance metrics
- **Bulk Operations**: Batch test creation and management

### **Medium-term**
- **AI Integration**: Automated test generation and grading
- **Proctoring**: Screen monitoring and cheating detection
- **Collaborative Tests**: Team-based coding challenges
- **Custom Grading**: Rubric-based evaluation

### **Long-term**
- **Machine Learning**: Performance prediction and optimization
- **Advanced Security**: Biometric authentication and monitoring
- **Global Features**: Multi-language support and localization
- **Enterprise Features**: Advanced reporting and analytics

---

## 🎯 Success Metrics

### **Technical Achievements**
- ✅ **Complete API System**: 25+ endpoints covering full test lifecycle
- ✅ **Database Integration**: Proper relationships and constraints
- ✅ **Service Architecture**: Clean separation of concerns
- ✅ **DTO Pattern**: Type-safe request/response handling
- ✅ **Error Handling**: Comprehensive exception management

### **Business Impact**
- ✅ **Test Management**: Complete test creation and management
- ✅ **Student Experience**: Intuitive test-taking workflow
- ✅ **Analytics**: Comprehensive performance tracking
- ✅ **Scalability**: Designed for high-volume usage
- ✅ **Integration**: Seamless integration with existing system

---

*This Coding Test API provides a complete solution for creating, managing, and conducting coding tests with comprehensive analytics and reporting capabilities. The system is designed to be scalable, secure, and user-friendly while maintaining integration with the existing LeetCode Compiler API infrastructure.*
