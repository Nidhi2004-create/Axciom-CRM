Add-Type -AssemblyName System.Net.Http

$handler = New-Object System.Net.Http.HttpClientHandler
$cookieContainer = New-Object System.Net.CookieContainer
$handler.CookieContainer = $cookieContainer
$client = New-Object System.Net.Http.HttpClient($handler)
$client.BaseAddress = New-Object System.Uri("http://127.0.0.1:5124")

# 1. GET login page and grab request verification token
$loginPage = $client.GetStringAsync("/Account/Login").Result
if ($loginPage -match '__RequestVerificationToken.*value="([^"]+)"') {
    $token = $matches[1]
    Write-Host "[PASS] Anti-Forgery token detected on Login page" -ForegroundColor Green
} else {
    Write-Host "[FAIL] Could not find token" -ForegroundColor Red
}

# 2. POST login as Admin
$formDict = New-Object "System.Collections.Generic.Dictionary[string,string]"
$formDict.Add("Email", "admin@acxiom.com")
$formDict.Add("Password", "Admin@123")
$formDict.Add("RememberMe", "false")
$formDict.Add("__RequestVerificationToken", $token)

$content = New-Object System.Net.Http.FormUrlEncodedContent($formDict)
$loginResp = $client.PostAsync("/Account/Login", $content).Result
Write-Host "[PASS] Admin Login status: $($loginResp.StatusCode)" -ForegroundColor Green

# 3. Access Views as Admin
$views = @("/Dashboard", "/Customers", "/Leads", "/Opportunities", "/FollowUps", "/Activities", "/Reports", "/Users", "/AuditLogs")
foreach ($v in $views) {
    $resp = $client.GetAsync($v).Result
    if ($resp.IsSuccessStatusCode) {
        Write-Host "[PASS] $v returned $([int]$resp.StatusCode)" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] $v returned $([int]$resp.StatusCode)" -ForegroundColor Red
    }
}

# 4. Test Lead Conversion Endpoint
Write-Host "`nTesting Lead Conversion Workflow:" -ForegroundColor Cyan
$convToken = $null
$leadsPage = $client.GetStringAsync("/Leads").Result
if ($leadsPage -match '__RequestVerificationToken.*value="([^"]+)"') {
    $convToken = $matches[1]
}

# 5. Test CSV Export on Reports
Write-Host "`nTesting CSV Export functionality:" -ForegroundColor Cyan
$reports = @("CustomerReport", "LeadReport", "FollowUpReport", "OpportunityReport", "PipelineReport", "ConversionReport", "UserActivityReport", "AuditReport")
foreach ($rep in $reports) {
    $csvResp = $client.GetAsync("/Reports/$rep?export=csv").Result
    $ct = $csvResp.Content.Headers.ContentType.MediaType
    if ($csvResp.IsSuccessStatusCode -and $ct -like "*csv*") {
        Write-Host "[PASS] CSV Export /Reports/$rep - Content-Type: $ct" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] CSV Export /Reports/$rep returned $([int]$csvResp.StatusCode) ($ct)" -ForegroundColor Red
    }
}
