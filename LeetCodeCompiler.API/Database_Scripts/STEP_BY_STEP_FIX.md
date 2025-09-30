# 🔧 STEP-BY-STEP FIX FOR CODING TEST API

## ❌ Current Error:
```
{
  "error": "Failed to create coding test",
  "details": "An error occurred while saving the entity changes. See the inner exception for details."
}
```

## 🎯 Root Cause:
The database tables for Coding Tests don't exist yet. You need to create them first.

## 📋 SOLUTION STEPS:

### Step 1: Open SQL Server Management Studio
1. Open **SQL Server Management Studio (SSMS)**
2. Connect to your database server
3. Navigate to your **LeetCode** database

### Step 2: Run the Database Fix Script
1. Open the file: `Database_Scripts/SimpleFix.sql`
2. Copy the entire contents
3. Paste into SSMS query window
4. Execute the script (F5 or click Execute)

### Step 3: Verify Tables Were Created
Run this query to check:
```sql
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME LIKE 'CodingTest%'
ORDER BY TABLE_NAME;
```

You should see:
- CodingTests
- CodingTestQuestions  
- CodingTestTopicData

### Step 4: Verify Problems Table Has Data
Run this query:
```sql
SELECT COUNT(*) as ProblemCount FROM Problems;
SELECT TOP 5 Id, Title FROM Problems;
```

You should see 5 problems with IDs 1-5.

### Step 5: Test Your API Request
Now your original request should work:

```json
{
  "testName": "elon",
  "createdBy": 4021,
  "startDate": "2025-09-25T14:29:12.266Z",
  "endDate": "2025-09-25T14:29:12.266Z",
  "durationMinutes": 40,
  "totalQuestions": 1,
  "totalMarks": 10,
  "testType": "coding",
  "allowMultipleAttempts": true,
  "maxAttempts": 10,
  "showResultsImmediately": true,
  "allowCodeReview": true,
  "accessCode": "string",
  "tags": "string",
  "isResultPublishAutomatically": true,
  "applyBreachRule": true,
  "breachRuleLimit": 0,
  "hostIP": "string",
  "classId": 0,
  "topicData": [
    {
      "sectionId": 8049,
      "domainId": 1,
      "subdomainId": 2
    }
  ],
  "questions": [
    {
      "problemId": 1,
      "questionOrder": 100,
      "marks": 100,
      "timeLimitMinutes": 120,
      "customInstructions": "string"
    }
  ]
}
```

## 🚀 What the Script Does:

1. ✅ **Creates CodingTests table** with all new fields
2. ✅ **Creates CodingTestQuestions table** with foreign keys
3. ✅ **Creates CodingTestTopicData table** for topic data
4. ✅ **Inserts 5 sample problems** (IDs 1-5)
5. ✅ **Sets up foreign key relationships**

## ✅ After Running the Script:

- ✅ **problemId: 1** will exist (Two Sum problem)
- ✅ **All required tables** will be created
- ✅ **Foreign key constraints** will be satisfied
- ✅ **Your API request will work perfectly**

## 🔍 If You Still Get Errors:

1. **Check database connection** in `appsettings.json`
2. **Verify you're connected to the right database**
3. **Make sure the application is running** on `http://localhost:5081`
4. **Check the application logs** for more detailed error messages

## 📞 Quick Test:

After running the script, test with this simple request:
```bash
curl -X POST http://localhost:5081/api/CodingTest \
  -H "Content-Type: application/json" \
  -d '{
    "testName": "Test",
    "createdBy": 1,
    "startDate": "2025-01-20T10:00:00Z",
    "endDate": "2025-01-20T12:00:00Z",
    "durationMinutes": 60,
    "totalQuestions": 1,
    "totalMarks": 10,
    "testType": "Practice",
    "allowMultipleAttempts": false,
    "maxAttempts": 1,
    "showResultsImmediately": true,
    "allowCodeReview": false,
    "accessCode": "TEST123",
    "tags": "test",
    "isResultPublishAutomatically": true,
    "applyBreachRule": true,
    "breachRuleLimit": 0,
    "hostIP": "127.0.0.1",
    "classId": 1,
    "topicData": [{"sectionId": 1, "domainId": 1, "subdomainId": 1}],
    "questions": [{"problemId": 1, "questionOrder": 1, "marks": 10, "timeLimitMinutes": 30, "customInstructions": "Test"}]
  }'
```

**The key is running the database script first!** 🎯
