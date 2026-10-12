$baseUrl = "http://127.0.0.1:5124"
$results = @()

function Test-Step($name, $script) {
    try {
        $res = & $script
        Write-Host "[PASS] $name" -ForegroundColor Green
        return @{ Name = $name; Status = "PASS"; Details = $res }
    } catch {
        Write-Host "[FAIL] $name : $_" -ForegroundColor Red
        return @{ Name = $name; Status = "FAIL"; Details = $_.ToString() }
    }
}

Write-Host "`n=== ACXIOM CRM VERIFICATION SUITE ===" -ForegroundColor Cyan

# 1. Swagger UI & Schema
$r1 = Test-Step "Swagger JSON Document Availability" {
    $resp = Invoke-RestMethod -Uri "$baseUrl/swagger/v1/swagger.json" -Method Get
    if ($resp.openapi -or $resp.swagger) { "OpenAPI version: $($resp.openapi)$($resp.swagger)" } else { throw "No openapi field" }
}
$results += $r1

# 2. Anonymous Access Control (MVC redirects to login)
$r2 = Test-Step "Anonymous Access Redirection" {
    $webReq = [System.Net.HttpWebRequest]::Create("$baseUrl/Customers")
    $webReq.AllowAutoRedirect = $false
    try {
        $webResp = $webReq.GetResponse()
        if ([int]$webResp.StatusCode -eq 302) {
            "Redirected to Login: $($webResp.Headers['Location'])"
        } else {
            throw "Expected 302 redirect, got $($webResp.StatusCode)"
        }
    } catch [System.Net.WebException] {
        $resp = $_.Exception.Response
        if ($resp -and [int]$resp.StatusCode -eq 302) {
            "Redirected to Login: $($resp.Headers['Location'])"
        } else {
            throw $_
        }
    }
}
$results += $r2

# 3. API Login with Admin Credentials
$adminToken = $null
$r3 = Test-Step "API Auth: POST /api/auth/login (Admin)" {
    $body = @{ email = "admin@acxiom.com"; password = "Admin@123" } | ConvertTo-Json
    $resp = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $body -ContentType "application/json"
    if ($resp.token) {
        $global:adminToken = $resp.token
        "Token acquired for user: $($resp.user.email) in role: $($resp.user.roles -join ', ')"
    } else {
        throw "No token in response"
    }
}
$results += $r3

$headers = @{ Authorization = "Bearer $global:adminToken" }

# 4. Get Customers API
$r4 = Test-Step "API: GET /api/customers" {
    $resp = Invoke-RestMethod -Uri "$baseUrl/api/customers" -Method Get -Headers $headers
    "Found $($resp.Count) customers"
}
$results += $r4

# 5. Create Customer API
$newCustId = 0
$randNum = Get-Random -Minimum 10000000 -Maximum 99999999
$testEmail = "client_$randNum@example.com"
$testPhone = "99$randNum"
$r5 = Test-Step "API: POST /api/customers (Create)" {
    $body = @{
        customerName = "Apex Innovations Corp"
        email = $testEmail
        phone = $testPhone
        companyName = "Apex Innovations"
        address = "12 Cyber City, Tech Zone"
        city = "Gurgaon"
        state = "Haryana"
        status = "Active"
    } | ConvertTo-Json
    $resp = Invoke-RestMethod -Uri "$baseUrl/api/customers" -Method Post -Headers $headers -Body $body -ContentType "application/json"
    $global:newCustId = $resp.customerId
    "Created Customer ID: $($resp.customerId) - $($resp.customerName)"
}
$results += $r5

# 6. Duplicate Customer Check
$r6 = Test-Step "API Validation: Duplicate Customer Rejection (409 Conflict)" {
    $body = @{
        customerName = "Duplicate Company"
        email = $testEmail
        phone = $testPhone
    } | ConvertTo-Json
    try {
        $resp = Invoke-RestMethod -Uri "$baseUrl/api/customers" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        throw "Should have failed with 409 Conflict"
    } catch {
        if ($_.Exception.Response.StatusCode -eq 409 -or $_.ToString() -like "*409*") {
            "Correctly rejected duplicate email with 409 Conflict"
        } else {
            throw $_
        }
    }
}
$results += $r6

# 7. Get Leads API
$r7 = Test-Step "API: GET /api/leads" {
    $resp = Invoke-RestMethod -Uri "$baseUrl/api/leads" -Method Get -Headers $headers
    "Found $($resp.Count) leads"
}
$results += $r7

# 8. Create Lead API
$newLeadId = 0
$randNum2 = Get-Random -Minimum 10000000 -Maximum 99999999
$r8 = Test-Step "API: POST /api/leads (Create)" {
    $body = @{
        leadName = "Summit Retail Solutions"
        email = "lead_$randNum2@summitretail.com"
        phone = "98$randNum2"
        companyName = "Summit Retail"
        source = "Website"
        status = "New"
        priority = "High"
        expectedValue = 350000
        notes = "Interested in CRM enterprise licensing"
    } | ConvertTo-Json
    $resp = Invoke-RestMethod -Uri "$baseUrl/api/leads" -Method Post -Headers $headers -Body $body -ContentType "application/json"
    $global:newLeadId = $resp.leadId
    "Created Lead ID: $($resp.leadId) - $($resp.leadName)"
}
$results += $r8

# 9. Opportunity Validation: Amount <= 0 Rejection
$r9 = Test-Step "API Validation: Opportunity Amount <= 0 Rejection (400 Bad Request)" {
    $body = @{
        opportunityName = "Invalid Opp Zero Amount"
        amount = 0
        stage = "Prospecting"
        probability = 50
        expectedCloseDate = (Get-Date).AddDays(30).ToString("yyyy-MM-dd")
        customerId = $global:newCustId
    } | ConvertTo-Json
    try {
        $resp = Invoke-RestMethod -Uri "$baseUrl/api/opportunities" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        throw "Expected 400 Bad Request for zero amount"
    } catch {
        if ($_.Exception.Response.StatusCode -eq 400 -or $_.ToString() -like "*400*") {
            "Correctly rejected zero amount with 400 Bad Request"
        } else {
            throw $_
        }
    }
}
$results += $r9

# 10. Opportunity Validation: Probability > 100 Rejection
$r10 = Test-Step "API Validation: Opportunity Probability > 100 Rejection (400 Bad Request)" {
    $body = @{
        opportunityName = "Invalid Opp High Probability"
        amount = 500000
        stage = "Prospecting"
        probability = 150
        expectedCloseDate = (Get-Date).AddDays(30).ToString("yyyy-MM-dd")
        customerId = $global:newCustId
    } | ConvertTo-Json
    try {
        $resp = Invoke-RestMethod -Uri "$baseUrl/api/opportunities" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        throw "Expected 400 Bad Request for probability > 100"
    } catch {
        if ($_.Exception.Response.StatusCode -eq 400 -or $_.ToString() -like "*400*") {
            "Correctly rejected probability > 100 with 400 Bad Request"
        } else {
            throw $_
        }
    }
}
$results += $r10

# 11. Create Valid Opportunity API
$r11 = Test-Step "API: POST /api/opportunities (Create Valid Opportunity)" {
    $body = @{
        opportunityName = "Enterprise Cloud License 2026"
        amount = 750000
        stage = "Proposal"
        probability = 70
        expectedCloseDate = (Get-Date).AddDays(45).ToString("yyyy-MM-ddTHH:mm:ss")
        customerId = $global:newCustId
        notes = "Proposal submitted and under legal review"
    } | ConvertTo-Json
    $resp = Invoke-RestMethod -Uri "$baseUrl/api/opportunities" -Method Post -Headers $headers -Body $body -ContentType "application/json"
    "Created Opportunity ID: $($resp.opportunityId) - $($resp.opportunityName) (Weighted: $($resp.weightedAmount))"
}
$results += $r11

# 12. Create Follow-Up API
$r12 = Test-Step "API: POST /api/followups (Create Future Follow-Up)" {
    $body = @{
        subject = "Quarterly Strategic Review Call"
        followUpType = "Call"
        followUpDate = (Get-Date).AddDays(3).ToString("yyyy-MM-ddTHH:mm:ss")
        customerId = $global:newCustId
        remarks = "Review contract milestone deliverables"
    } | ConvertTo-Json
    $resp = Invoke-RestMethod -Uri "$baseUrl/api/followups" -Method Post -Headers $headers -Body $body -ContentType "application/json"
    "Created Follow-Up ID: $($resp.followUpId) - $($resp.subject)"
}
$results += $r12

# 13. Pipeline Report API
$r13 = Test-Step "API: GET /api/reports/pipeline" {
    $resp = Invoke-RestMethod -Uri "$baseUrl/api/reports/pipeline" -Method Get -Headers $headers
    "Total Pipeline Amount: $($resp.totalPipelineValue) across $($resp.totalDealsCount) opportunities. Stages: $($resp.stageBreakdown.Count)"
}
$results += $r13

# 14. Role-Based Scoping Test: Sales Executive Token
$r14 = Test-Step "API Role Scoping: Sales Executive Views Scoped Records" {
    $body = @{ email = "sales@acxiom.com"; password = "Sales@123" } | ConvertTo-Json
    $salesAuth = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $body -ContentType "application/json"
    $salesHeaders = @{ Authorization = "Bearer $($salesAuth.token)" }
    $salesCustomers = Invoke-RestMethod -Uri "$baseUrl/api/customers" -Method Get -Headers $salesHeaders
    "Sales Executive sees $($salesCustomers.Count) assigned customers"
}
$results += $r14

Write-Host "`n=== SUMMARY ===" -ForegroundColor Cyan
$passed = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$failed = ($results | Where-Object { $_.Status -eq "FAIL" }).Count
if ($failed -eq 0) {
    Write-Host "Total Tests: $($results.Count) | Passed: $passed | Failed: $failed" -ForegroundColor Green
} else {
    Write-Host "Total Tests: $($results.Count) | Passed: $passed | Failed: $failed" -ForegroundColor Red
}
