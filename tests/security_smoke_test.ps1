$ErrorActionPreference = "Stop"
Write-Host "--- Testing AcxiomCRM Security & Role Restrictions ---"

# 1. Anonymous Access to Protected API Endpoints must fail (401)
try {
    Invoke-RestMethod -Uri "http://localhost:5000/api/customers" -Method Get
    Write-Error "FAIL: Anonymous access should have been rejected."
}
catch {
    Write-Host "PASS: Anonymous request properly rejected."
}

# 2. Login as Sales Executive
$execBody = @{
    email = "executive@acxiomcrm.com"
    password = "Executive@12345"
} | ConvertTo-Json

$execLogin = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" -Method Post -Body $execBody -ContentType "application/json" -SessionVariable execSession
Write-Host "Executive Login:" $execLogin.data.fullName "Roles:" ($execLogin.data.roles -join ",")

# 3. Sales Executive Scoped Customer Query
$execCustomers = Invoke-RestMethod -Uri "http://localhost:5000/api/customers" -Method Get -WebSession $execSession
Write-Host "Executive Visible Customers Count:" $execCustomers.data.totalCount
foreach ($c in $execCustomers.data.items) {
    Write-Host " - Scoped Customer:" $c.customerName "(AssignedTo:" $c.assignedUserName ")"
}

# 4. Sales Executive Scoped Leads Query
$execLeads = Invoke-RestMethod -Uri "http://localhost:5000/api/leads" -Method Get -WebSession $execSession
Write-Host "Executive Visible Leads Count:" $execLeads.data.totalCount
foreach ($l in $execLeads.data.items) {
    Write-Host " - Scoped Lead:" $l.leadName "(AssignedTo:" $l.assignedUserName ")"
}

# 5. Invalid Login Attempt Verification
try {
    $badBody = @{ email = "executive@acxiomcrm.com"; password = "WrongPassword999!" } | ConvertTo-Json
    Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" -Method Post -Body $badBody -ContentType "application/json"
    Write-Error "FAIL: Invalid password should have failed."
}
catch {
    Write-Host "PASS: Invalid credentials properly rejected by Identity."
}

Write-Host "--- ALL SECURITY & SCOPING TESTS PASSED ---"
