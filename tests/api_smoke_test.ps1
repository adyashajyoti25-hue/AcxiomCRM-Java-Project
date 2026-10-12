$ErrorActionPreference = "Stop"
Write-Host "--- Testing AcxiomCRM REST API Endpoints ---"

# 1. API Login as Admin
$body = @{
    email = "admin@acxiomcrm.com"
    password = "Admin@12345"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" -Method Post -Body $body -ContentType "application/json" -SessionVariable crmSession
Write-Host "Login response:" ($loginResponse | ConvertTo-Json -Compress)

# 2. Get Customers
$customers = Invoke-RestMethod -Uri "http://localhost:5000/api/customers" -Method Get -WebSession $crmSession
Write-Host "Customers TotalCount:" $customers.data.totalCount
Write-Host "First Customer Name:" $customers.data.items[0].customerName

# 3. Get Leads
$leads = Invoke-RestMethod -Uri "http://localhost:5000/api/leads" -Method Get -WebSession $crmSession
Write-Host "Leads TotalCount:" $leads.data.totalCount
Write-Host "First Lead Name:" $leads.data.items[0].leadName

# 4. Get Opportunities
$opps = Invoke-RestMethod -Uri "http://localhost:5000/api/opportunities" -Method Get -WebSession $crmSession
Write-Host "Opportunities TotalCount:" $opps.data.totalCount
Write-Host "First Opportunity Name:" $opps.data.items[0].opportunityName

# 5. Get Pipeline Report
$report = Invoke-RestMethod -Uri "http://localhost:5000/api/reports/pipeline" -Method Get -WebSession $crmSession
Write-Host "Total Open Pipeline Value:" $report.data.totalPipeline
Write-Host "Pipeline Stages Count:" $report.data.stageSummary.Count

# 6. Get Web Dashboard View
$dash = Invoke-WebRequest -Uri "http://localhost:5000/Dashboard" -Method Get -WebSession $crmSession -UseBasicParsing
Write-Host "Dashboard Web Page Status:" $dash.StatusCode

Write-Host "--- ALL SMOKE TESTS COMPLETED SUCCESSFULLY ---"
