Add-Type -AssemblyName System.Net.Http

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
Write-Host "Token found: $($tokenMatch.Success)"

$cookieList = $cookies.GetCookies((New-Object System.Uri("http://127.0.0.1:5124")))
Write-Host "Cookies count after GET: $($cookieList.Count)"
foreach ($c in $cookieList) { Write-Host "  Cookie: $($c.Name)" }

# 2. POST Login
$pairs = New-Object 'System.Collections.Generic.List[System.Collections.Generic.KeyValuePair[string,string]]'
$pairs.Add((New-Object 'System.Collections.Generic.KeyValuePair[string,string]'('Email', 'admin@acxiom.com')))
$pairs.Add((New-Object 'System.Collections.Generic.KeyValuePair[string,string]'('Password', 'Admin@123')))
$pairs.Add((New-Object 'System.Collections.Generic.KeyValuePair[string,string]'('RememberMe', 'false')))
$pairs.Add((New-Object 'System.Collections.Generic.KeyValuePair[string,string]'('__RequestVerificationToken', $tokenMatch.Groups[1].Value)))
$content = [System.Net.Http.FormUrlEncodedContent]::new($pairs)

$postResp = $client.PostAsync("/Account/Login", $content).Result
Write-Host "POST status: $([int]$postResp.StatusCode) $($postResp.StatusCode)"
Write-Host "Location: $($postResp.Headers.Location)"

$cookieListAfter = $cookies.GetCookies((New-Object System.Uri("http://127.0.0.1:5124")))
Write-Host "Cookies count after POST: $($cookieListAfter.Count)"
foreach ($c in $cookieListAfter) { Write-Host "  Cookie: $($c.Name)" }

# 3. Access Dashboard
$dashResp = $client.GetAsync("/Dashboard").Result
Write-Host "Dashboard GET status: $([int]$dashResp.StatusCode) $($dashResp.StatusCode)"

# 4. Access Reports CSV Export
$repResp = $client.GetAsync("/Reports/CustomerReport?export=true").Result
Write-Host "CustomerReport CSV GET status: $([int]$repResp.StatusCode) $($repResp.StatusCode) Content-Type: $($repResp.Content.Headers.ContentType)"
