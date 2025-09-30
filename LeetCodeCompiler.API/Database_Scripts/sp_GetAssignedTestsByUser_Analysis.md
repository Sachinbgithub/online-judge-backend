# 📋 **Stored Procedure Analysis: `sp_GetAssignedTestsByUser`**

## 🎯 **Purpose**
Retrieves all tests assigned to a specific user with comprehensive filtering and status determination.

---

## 📊 **Parameters**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `@UserId` | `BIGINT` | ✅ Yes | User ID to get assigned tests for |
| `@UserType` | `TINYINT` | ✅ Yes | User type (25, 1, 3, etc.) |
| `@TestType` | `TINYINT` | ❌ Optional | Test type filter (1=Subject, 2=Topic, 3=Mock) |
| `@ClassId` | `BIGINT` | ❌ Optional | **NEW** Class filter on test masters |

---

## 🗄️ **Tables Involved**

### **Primary Tables:**
- **`AssignedTests`** - Main assignment table
- **`NewManualTest`** - Manual test definitions
- **`NewRandomTest`** - Random test definitions

### **Supporting Tables:**
- **`ManualTestTopicMap`** - Manual test topic mappings
- **`RandomTestTopicMap`** - Random test topic mappings
- **`Subject_New`** - Subject definitions
- **`All_Topic`** - Topic definitions
- **`All_Users`** - User information
- **`UserTestAttempt`** - User test attempts

---

## 🔍 **Key Features**

### **1. Dual Test Mode Support**
```sql
-- TestMode = 1: Manual Tests
LEFT JOIN NewManualTest MT ON AT.TestMode = 1 AND AT.TestId = MT.ManualTestId

-- TestMode = 2: Random Tests  
LEFT JOIN NewRandomTest RT ON AT.TestMode = 2 AND AT.TestId = RT.RandomTestId
```

### **2. Dynamic Subject Name Concatenation**
```sql
CASE 
    WHEN AT.TestMode = 1 THEN
        STUFF((SELECT DISTINCT ', ' + S.SubjectName
               FROM ManualTestTopicMap MTM
               INNER JOIN Subject_New S ON MTM.SubjectId = S.SubjectID
               WHERE MTM.ManualTestId = AT.TestId
               FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '')
    WHEN AT.TestMode = 2 THEN
        -- Similar logic for Random Tests
    ELSE 'N/A'
END AS SubjectName
```

### **3. Dynamic Topic Name Concatenation**
```sql
CASE 
    WHEN AT.TestMode = 1 THEN
        STUFF((SELECT DISTINCT ', ' + T.TopicName
               FROM ManualTestTopicMap MTM
               INNER JOIN All_Topic T ON T.TopicID = MTM.TopicId AND T.SubjectID = MTM.SubjectId
               WHERE MTM.ManualTestId = AT.TestId AND MTM.TopicId IS NOT NULL
               FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '')
    -- Similar for Random Tests
END AS TopicName
```

### **4. Status Determination Logic**
```sql
CASE
    WHEN UTA.IsSubmitted = 1 THEN 'submitted'
    WHEN GETDATE() > ISNULL(MT.EndDate, RT.EndDate) THEN 'expired'
    ELSE 'assigned'
END AS Status
```

### **5. NEW: Class-Based Filtering**
```sql
AND (
    @ClassId IS NULL
    OR (AT.TestMode = 1 AND (MT.ClassId = @ClassId OR MT.ClassId IS NULL))
    OR (AT.TestMode = 2 AND (RT.ClassId = @ClassId OR RT.ClassId IS NULL))
)
```

---

## 📋 **Returned Columns**

| Column | Description |
|--------|-------------|
| `AssignedId` | Assignment ID |
| `TestId` | Test ID |
| `TestMode` | Test mode (1=Manual, 2=Random) |
| `TestType` | Test type (1=Subject, 2=Topic, 3=Mock) |
| `TestName` | Test name (from Manual or Random test) |
| `SubjectName` | Comma-separated subject names |
| `TopicName` | Comma-separated topic names |
| `AssignedByName` | Full name of person who assigned |
| `AssignedDate` | Assignment date |
| `StartDate` | Test start date |
| `EndDate` | Test end date |
| `Status` | Test status (assigned/submitted/expired) |

---

## 🔧 **Filtering Logic**

### **Core Filters:**
```sql
WHERE AT.AssignedToUserId   = @UserId
  AND AT.AssignedToUserType = @UserType
  AND AT.IsDeleted          = 0
```

### **Optional Filters:**
```sql
-- Test Type Filter
AND (@TestType IS NULL OR AT.TestType = @TestType)

-- Class Filter (NEW)
AND (
    @ClassId IS NULL
    OR (AT.TestMode = 1 AND (MT.ClassId = @ClassId OR MT.ClassId IS NULL))
    OR (AT.TestMode = 2 AND (RT.ClassId = @ClassId OR RT.ClassId IS NULL))
)
```

---

## 🎯 **Use Cases**

### **1. Get All Assigned Tests for User**
```sql
EXEC sp_GetAssignedTestsByUser @UserId = 12345, @UserType = 25
```

### **2. Get Only Subject Tests**
```sql
EXEC sp_GetAssignedTestsByUser @UserId = 12345, @UserType = 25, @TestType = 1
```

### **3. Get Tests for Specific Class**
```sql
EXEC sp_GetAssignedTestsByUser @UserId = 12345, @UserType = 25, @ClassId = 789
```

### **4. Get Mock Tests for Specific Class**
```sql
EXEC sp_GetAssignedTestsByUser @UserId = 12345, @UserType = 25, @TestType = 3, @ClassId = 789
```

---

## ⚡ **Performance Considerations**

### **Strengths:**
- ✅ Uses `LEFT JOIN` for optional data
- ✅ Filters on indexed columns (`UserId`, `UserType`)
- ✅ Uses `SET NOCOUNT ON` for efficiency

### **Potential Issues:**
- ⚠️ **XML PATH concatenation** can be expensive for large datasets
- ⚠️ **Multiple subqueries** in CASE statements
- ⚠️ **Complex WHERE clause** with multiple OR conditions

### **Optimization Suggestions:**
1. **Indexes needed:**
   ```sql
   -- On AssignedTests
   CREATE INDEX IX_AssignedTests_User ON AssignedTests(AssignedToUserId, AssignedToUserType, IsDeleted)
   
   -- On UserTestAttempt
   CREATE INDEX IX_UserTestAttempt_User ON UserTestAttempt(UserId, UserType, TestId, TestMode, IsDeleted)
   ```

2. **Consider CTEs** for complex concatenation logic
3. **Monitor execution plans** for large datasets

---

## 🔄 **Status Logic Breakdown**

| Condition | Status | Description |
|-----------|--------|-------------|
| `UTA.IsSubmitted = 1` | `submitted` | User has completed the test |
| `GETDATE() > EndDate` | `expired` | Test deadline has passed |
| Otherwise | `assigned` | Test is still available |

---

## 🆕 **Recent Enhancement**

### **Class Filtering Feature:**
- **Purpose**: Filter tests by class while including global tests
- **Logic**: Shows tests where `ClassId` matches OR is `NULL` (global)
- **Benefit**: Allows class-specific test management

---

## 📊 **Sample Output**

```sql
AssignedId | TestId | TestMode | TestType | TestName | SubjectName | TopicName | Status
-----------|--------|----------|----------|----------|-------------|----------|--------
1001       | 5001   | 1        | 1        | Math Quiz| Mathematics | Algebra  | assigned
1002       | 5002   | 2        | 2        | Science  | Physics     | Mechanics| submitted
1003       | 5003   | 1        | 3        | Mock Test| All Subjects| All Topics| expired
```

---

## 🎯 **Key Takeaways**

1. **Comprehensive**: Handles both manual and random tests
2. **Flexible**: Multiple optional filters
3. **Dynamic**: Concatenates related data (subjects/topics)
4. **Status-Aware**: Determines test status based on completion and dates
5. **Class-Aware**: **NEW** feature for class-based filtering
6. **Performance-Conscious**: Uses efficient joins and filters

This stored procedure is well-designed for a comprehensive test management system with flexible filtering capabilities.
