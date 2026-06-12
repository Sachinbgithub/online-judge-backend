$Base = "http://localhost:5081"
$Api = "$Base/api"
$results = @()

function Test-Api {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [scriptblock]$Assert
    )
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            ContentType = "application/json"
            ErrorAction = "Stop"
        }
        if ($Body) { $params.Body = ($Body | ConvertTo-Json -Depth 20) }
        $resp = Invoke-RestMethod @params
        $ok = & $Assert $resp
        $results += [pscustomobject]@{ Test = $Name; Status = if ($ok) { "PASS" } else { "FAIL" }; Detail = "" }
        Write-Host ("[{0}] {1}" -f $(if ($ok) { "PASS" } else { "FAIL" }), $Name)
    }
    catch {
        $detail = $_.Exception.Message
        if ($_.ErrorDetails.Message) { $detail = $_.ErrorDetails.Message }
        $results += [pscustomobject]@{ Test = $Name; Status = "FAIL"; Detail = $detail }
        Write-Host "[FAIL] $Name - $detail"
    }
}

$wordBreakCode = @'
class Solution:
    def wordBreak(self, s: str, wordDict: list[str]) -> None:
        word_set = set(wordDict)
        n = len(s)
        dp = [False] * (n + 1)
        dp[0] = True
        for i in range(1, n + 1):
            for j in range(i):
                if dp[j] and s[j:i] in word_set:
                    dp[i] = True
                    break
        print("true" if dp[n] else "false")

if __name__ == '__main__':
    try:
        s = input().strip()
        wordDictStr = input().strip()
        wordDict = wordDictStr.split() if wordDictStr else []
        Solution().wordBreak(s, wordDict)
    except:
        pass
'@

Write-Host "=== API Smoke Tests ===" -ForegroundColor Cyan

# 1 Health
Test-Api "Health check" GET "$Base/health" -Assert { param($r) $r.status -eq "Healthy" }

# 2 Get coding test (decimal marks)
Test-Api "GET CodingTest by id" GET "$Api/CodingTest/1015106" -Assert {
    param($r)
    [decimal]$r.totalMarks -eq 25 -and $r.questions.Count -ge 1 -and [decimal]$r.questions[0].marks -gt 0
}

# 3 Comprehensive results (decimal scores)
Test-Api "GET comprehensive results" GET "$Api/CodingTest/results/comprehensive?userId=396846&codingTestId=1015106&attemptNumber=1" -Assert {
    param($r)
    $r.codingTestId -eq 1015106 -and
    [decimal]$r.totalMarks -eq 25 -and
    $r.problemResults.Count -ge 1 -and
    $r.problemResults[0].testCaseResults.Count -ge 1
}

# 4 Combined results
Test-Api "GET combined-results" GET "$Api/CodingTest/combined-results?userId=396846&codingTestId=1015106" -Assert {
    param($r)
    $r.codingTestId -eq 1015106 -and
    [decimal]$r.totalMarks -eq 25 -and
    $r.questionSubmissions[0].testCaseResults.Count -ge 1
}

# 5 Code execution run
$runBody = @{
    language = "python"
    code = $wordBreakCode
    testCases = @(
        @{ input = "leetcode`nleet code"; expectedOutput = "true" }
        @{ input = "a`nb"; expectedOutput = "false" }
    )
}
Test-Api "POST CodeExecution run" POST "$Api/CodeExecution" -Body $runBody -Assert {
    param($r)
    $r.results.Count -eq 2 -and ($r.results | Where-Object { -not $_.passed }).Count -eq 0
}

# 6 QuestionResult submit (server judge)
$submitBody = @{
    userId = 396846
    problemId = 5348
    attemptNumber = 99
    languageUsed = "python"
    finalCodeSnapshot = $wordBreakCode
    runClickCount = 1
    submitClickCount = 1
}
Test-Api "POST QuestionResult/submit" POST "$Api/QuestionResult/submit" -Body $submitBody -Assert {
    param($r)
    $r.passedTestCases -eq 5 -and $r.results.Count -eq 5
}

# 7 Create test validation - mismatched marks should fail
$badCreate = @{
    testName = "decimal-validation-test"
    createdBy = 1
    startDate = (Get-Date).ToString("o")
    endDate = (Get-Date).AddDays(7).ToString("o")
    durationMinutes = 60
    totalQuestions = 2
    totalMarks = 25.00
    isGlobal = $false
    collegeId = 0
    topicData = @(@{ sectionId = 1; domainId = 1; subdomainId = 1 })
    questions = @(
        @{ problemId = 5348; questionOrder = 1; marks = 12.50; timeLimitMinutes = 30 }
        @{ problemId = 5347; questionOrder = 2; marks = 12.00; timeLimitMinutes = 30 }
    )
}
try {
    Invoke-RestMethod -Uri "$Api/CodingTest" -Method POST -ContentType "application/json" -Body ($badCreate | ConvertTo-Json -Depth 10) -ErrorAction Stop
    Write-Host "[FAIL] Create test mismatch validation - should have rejected"
    $results += [pscustomobject]@{ Test = "Create test rejects mark mismatch"; Status = "FAIL"; Detail = "Accepted invalid payload" }
}
catch {
    $msg = $_.ErrorDetails.Message
    if ($msg -match "must equal sum") {
        Write-Host "[PASS] Create test rejects mark mismatch"
        $results += [pscustomobject]@{ Test = "Create test rejects mark mismatch"; Status = "PASS"; Detail = "" }
    }
    else {
        Write-Host "[FAIL] Create test mismatch - unexpected error: $msg"
        $results += [pscustomobject]@{ Test = "Create test rejects mark mismatch"; Status = "FAIL"; Detail = $msg }
    }
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
$results | Format-Table -AutoSize
$passed = ($results | Where-Object Status -eq "PASS").Count
$failed = ($results | Where-Object Status -eq "FAIL").Count
Write-Host "Passed: $passed  Failed: $failed  Total: $($results.Count)"
if ($failed -gt 0) { exit 1 }
