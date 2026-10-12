Add-Type -AssemblyName System.Net.Http

function Get-AuthenticatedClient($email, $password) {
    $cookies = New-Object System.Net.CookieContainer
    $handler = New-Object System.Net.Http.HttpClientHandler
    $handler.CookieContainer = $cookies
    $handler.AllowAutoRedirect = $false
    $client = New-Object System.Net.Http.HttpClient($handler)
    $client.BaseAddress = New-Object System.Uri("http://127.0.0.1:5124")

    # 1. GET Login page
    $getResp = $client.GetAsync("/Account/Login").Result
    $html = $getResp.Content.ReadAsStringAsync().Result
    $tokenMatch = [regex]::Match($html, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')

    # 2. POST Login
    $pairs = New-Object 'System.Collections.Generic.List[System.Collections.Generic.KeyValuePair[string,string]]'
    $pairs.Add((New-Object 'System.Collections.Generic.KeyValuePair[string,string]'('Email', $email)))
    $pairs.Add((New-Object 'System.Collections.Generic.KeyValuePair[string,string]'('Password', $password)))
    $pairs.Add((New-Object 'System.Collections.Generic.KeyValuePair[string,string]'('RememberMe', 'false')))
    $pairs.Add((New-Object 'System.Collections.Generic.KeyValuePair[string,string]'('__RequestVerificationToken', $tokenMatch.Groups[1].Value)))
    $content = [System.Net.Http.FormUrlEncodedContent]::new($pairs)

    $postResp = $client.PostAsync("/Account/Login", $content).Result
    if ($postResp.StatusCode -ne 302) {
        throw "Login failed for $email with status $($postResp.StatusCode)"
    }
    return $client
}

Write-Host "=== MVC & ROLE SECURITY TEST SUITE ===" -ForegroundColor Cyan

# 1. Test Admin Access across all modules
$adminClient = Get-AuthenticatedClient "admin@acxiom.com" "Admin@123"
Write-Host "[PASS] Admin successfully authenticated with Cookie" -ForegroundColor Green

$adminPages = @("/Dashboard", "/Customers", "/Leads", "/Opportunities", "/FollowUps", "/Activities", "/Reports", "/Users", "/AuditLogs")
foreach ($p in $adminPages) {
    $resp = $adminClient.GetAsync($p).Result
    if ($resp.StatusCode -eq 200) {
        Write-Host "[PASS] Admin can access $p (200 OK)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Admin cannot access $p ($($resp.StatusCode))" -ForegroundColor Red
    }
}

# 2. Test All 8 Report CSV Exports
$reports = @("CustomerReport", "LeadReport", "FollowUpReport", "OpportunityReport", "PipelineReport", "ConversionReport", "UserActivityReport", "AuditReport")
foreach ($r in $reports) {
    $resp = $adminClient.GetAsync("/Reports/$r?export=true").Result
    $ct = $resp.Content.Headers.ContentType.MediaType
    if ($resp.StatusCode -eq 200 -and $ct -like "*csv*") {
        Write-Host "[PASS] Export /Reports/$r -> 200 OK ($ct)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Export /Reports/$r -> $($resp.StatusCode)" -ForegroundColor Red
    }
}

# 3. Test Sales Executive Authorization (Must NOT be able to access /Users or /AuditLogs)
$salesClient = Get-AuthenticatedClient "sales@acxiom.com" "Sales@123"
Write-Host "`n[PASS] Sales Executive authenticated" -ForegroundColor Green

$forbiddenPages = @("/Users", "/AuditLogs")
foreach ($p in $forbiddenPages) {
    $resp = $salesClient.GetAsync($p).Result
    # Should either return 403 or redirect to AccessDenied (302)
    if ($resp.StatusCode -eq 403 -or ($resp.StatusCode -eq 302 -and $resp.Headers.Location -like "*AccessDenied*")) {
        Write-Host "[PASS] Sales Executive blocked from $p (Status: $($resp.StatusCode), Location: $($resp.Headers.Location))" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Sales Executive unexpectedly accessed $p (Status: $($resp.StatusCode))" -ForegroundColor Red
    }
}

# 4. Test Manager Authorization (Can access /AuditLogs, but NOT /Users which is Admin-only)
$mgrClient = Get-AuthenticatedClient "manager@acxiom.com" "Manager@123"
Write-Host "`n[PASS] Manager authenticated" -ForegroundColor Green

$mgrAuditResp = $mgrClient.GetAsync("/AuditLogs").Result
if ($mgrAuditResp.StatusCode -eq 200) {
    Write-Host "[PASS] Manager can access /AuditLogs (200 OK)" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Manager cannot access /AuditLogs ($($mgrAuditResp.StatusCode))" -ForegroundColor Red
}

$mgrUsersResp = $mgrClient.GetAsync("/Users").Result
if ($mgrUsersResp.StatusCode -eq 403 -or ($mgrUsersResp.StatusCode -eq 302 -and $mgrUsersResp.Headers.Location -like "*AccessDenied*")) {
    Write-Host "[PASS] Manager blocked from /Users (Admin only) (Status: $($mgrUsersResp.StatusCode))" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Manager unexpectedly accessed /Users ($($mgrUsersResp.StatusCode))" -ForegroundColor Red
}

Write-Host "`n=== COMPLETED ALL MVC & ROLE SECURITY TESTS ===" -ForegroundColor Cyan
