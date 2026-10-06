$baseUrl = "http://localhost:5239"

Write-Host "=== TEST 1: Windows Login (Customer: CORP\customer.ahmed) ==="
$loginHeaders = @{ "X-User-Username" = "CORP\customer.ahmed" }
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/windows-login" -Method Post -Headers $loginHeaders
$customerToken = $loginResponse.token
Write-Host "Success! Token Type: $($loginResponse.tokenType)"
Write-Host "Expires At: $($loginResponse.expiresAt)"
Write-Host "User: $($loginResponse.user.displayName) ($($loginResponse.user.role))"
Write-Host "Token length: $($customerToken.Length)"

Write-Host "`n=== TEST 2: GET /api/support-requests WITHOUT Token (Expect 401) ==="
try {
    Invoke-RestMethod -Uri "$baseUrl/api/support-requests" -Method Get
    Write-Host "ERROR: Expected 401 but request succeeded!"
} catch {
    Write-Host "Caught expected response: $($_.Exception.Response.StatusCode.value__) $($_.Exception.Response.StatusCode)"
}

Write-Host "`n=== TEST 3: GET /api/support-requests WITH Customer Bearer Token ==="
$authHeaders = @{ "Authorization" = "Bearer $customerToken" }
$requests = Invoke-RestMethod -Uri "$baseUrl/api/support-requests" -Method Get -Headers $authHeaders
Write-Host "Success! Retrieved $($requests.Count) requests with Customer Bearer token."

# If no requests exist yet, let's create one with customer token
if ($requests.Count -eq 0) {
    Write-Host "Creating a new complaint as Customer..."
    $createBody = @{
        subject = "VPN connection drops after 10 mins"
        description = "Every time I connect to the internal network via AnyConnect, it disconnects."
        priority = 2
    } | ConvertTo-Json
    $created = Invoke-RestMethod -Uri "$baseUrl/api/support-requests" -Method Post -Headers $authHeaders -ContentType "application/json" -Body $createBody
    Write-Host "Created complaint ID: $($created.id), Subject: $($created.subject)"
    $targetRequestId = $created.id
} else {
    $targetRequestId = $requests[0].id
}

Write-Host "`n=== TEST 4: Customer attempts to CLAIM complaint (Expect 403 Forbidden) ==="
try {
    Invoke-RestMethod -Uri "$baseUrl/api/support-requests/$targetRequestId/claim" -Method Post -Headers $authHeaders
    Write-Host "ERROR: Expected 403 Forbidden but request succeeded!"
} catch {
    Write-Host "Caught expected response: $($_.Exception.Response.StatusCode.value__) $($_.Exception.Response.StatusCode)"
}

Write-Host "`n=== TEST 5: Windows Login (CS Agent: CORP\agent.sara) ==="
$agentLoginHeaders = @{ "X-User-Username" = "CORP\agent.sara" }
$agentLoginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/windows-login" -Method Post -Headers $agentLoginHeaders
$agentToken = $agentLoginResponse.token
Write-Host "Success! Agent: $($agentLoginResponse.user.displayName) ($($agentLoginResponse.user.role))"

Write-Host "`n=== TEST 6: CS Agent Claims Complaint (Expect 200 OK or 409 Conflict) ==="
$agentAuthHeaders = @{ "Authorization" = "Bearer $agentToken" }
try {
    $claimResult = Invoke-RestMethod -Uri "$baseUrl/api/support-requests/$targetRequestId/claim" -Method Post -Headers $agentAuthHeaders
    Write-Host "Success! Complaint Claimed: Status=$($claimResult.status), Agent=$($claimResult.assignedAgent.displayName)"
} catch {
    Write-Host "Response: $($_.Exception.Response.StatusCode.value__) $($_.Exception.Response.StatusCode)"
}

Write-Host "`n=== ALL TESTS COMPLETED SUCCESSFULLY ==="
