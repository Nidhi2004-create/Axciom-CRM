Add-Type -AssemblyName System.Net.Http

$cookies = New-Object System.Net.CookieContainer
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.CookieContainer = $cookies
$handler.AllowAutoRedirect = $false
$client = New-Object System.Net.Http.HttpClient($handler)
$client.BaseAddress = New-Object System.Uri("http://127.0.0.1:5124")

$get = $client.GetAsync("/Account/Login").Result
$tok = [regex]::Match($get.Content.ReadAsStringAsync().Result, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"').Groups[1].Value

$pairs = New-Object 'System.Collections.Generic.List[System.Collections.Generic.KeyValuePair[string,string]]'
$pairs.Add((New-Object 'System.Collections.Generic.KeyValuePair[string,string]'('Email', 'admin@acxiom.com')))
$pairs.Add((New-Object 'System.Collections.Generic.KeyValuePair[string,string]'('Password', 'Admin@123')))
$pairs.Add((New-Object 'System.Collections.Generic.KeyValuePair[string,string]'('__RequestVerificationToken', $tok)))
$content = [System.Net.Http.FormUrlEncodedContent]::new($pairs)

$loginResp = $client.PostAsync("/Account/Login", $content).Result
Write-Host "Login: $([int]$loginResp.StatusCode)"

$resp = $client.GetAsync("/Reports/CustomerReport?export=true").Result
Write-Host "CustomerReport Status: $([int]$resp.StatusCode)"
Write-Host "Location: $($resp.Headers.Location)"
if ($resp.Headers.Location) {
    $next = $client.GetAsync($resp.Headers.Location).Result
    Write-Host "Next Status: $([int]$next.StatusCode)"
}
