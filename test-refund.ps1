$ErrorActionPreference = "Stop"
$BaseUrl = "http://127.0.0.1:5000/api"

Write-Host "Registering user to get auth token..." -ForegroundColor Cyan
$registerPayload = @{
    tenantName = "Refund Test Corp"
    firstName = "Admin"
    lastName = "User"
    email = "admin-refund-test-$(Get-Random)@example.com"
    password = "Password123!"
}
$authResponse = Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body ($registerPayload | ConvertTo-Json) -ContentType "application/json"
$token = $authResponse.Token
$headers = @{ "Authorization" = "Bearer $token" }


Write-Host "Creating a new customer..." -ForegroundColor Cyan
$customerPayload = @{
    name = "Refund Test Customer"
    email = "refund-test@example.com"
}
$customerId = Invoke-RestMethod -Uri "$BaseUrl/customers" -Method Post -Body ($customerPayload | ConvertTo-Json) -ContentType "application/json" -Headers $headers

Write-Host "Creating an invoice for customer $customerId..." -ForegroundColor Cyan
$invoicePayload = @{
    customerId = $customerId
    currency = "USD"
    dueDate = (Get-Date).AddDays(30).ToString("yyyy-MM-ddTHH:mm:ssZ")
    lineItems = @(
        @{
            description = "Refund Test Item"
            amount = 100.00
            quantity = 1
        }
    )
}
$invoiceId = Invoke-RestMethod -Uri "$BaseUrl/invoices" -Method Post -Body ($invoicePayload | ConvertTo-Json -Depth 5) -ContentType "application/json" -Headers $headers

Write-Host "Paying invoice $invoiceId..." -ForegroundColor Cyan
$payHeaders = $headers.Clone()
$payHeaders.Add("Idempotency-Key", [guid]::NewGuid().ToString())
Invoke-RestMethod -Uri "$BaseUrl/invoices/$invoiceId/pay" -Method Post -Headers $payHeaders

Write-Host ""
Write-Host "Fetching $invoiceId to confirm it is Paid..." -ForegroundColor Cyan

$invoices = Invoke-RestMethod -Uri "$BaseUrl/customers/$customerId/invoices" -Method Get -Headers $headers
$paidInvoice = $invoices | Where-Object { $_.Id -eq $invoiceId }

if (-not $paidInvoice -or $paidInvoice.Status -ne "Paid") {
    Write-Host "Could not find the paid invoice! Status: $($paidInvoice.Status)" -ForegroundColor Red
    exit 1
}

Write-Host "Found Paid Invoice: $invoiceId (Amount: $($paidInvoice.AmountDue))" -ForegroundColor Green

Write-Host "Refunding 5.00 of the invoice..." -ForegroundColor Cyan
$refundPayload = @{
    invoiceId = $invoiceId
    amount = 5.00
    reason = "Customer requested a partial refund"
    idempotencyKey = [guid]::NewGuid().ToString()
}

$refundResponse = Invoke-RestMethod -Uri "$BaseUrl/invoices/$invoiceId/refund" -Method Post -Body ($refundPayload | ConvertTo-Json) -ContentType "application/json" -Headers $headers
Write-Host "Refund Response:" -ForegroundColor Green
$refundResponse | Format-List

Write-Host "Fetching updated ledger entries..." -ForegroundColor Cyan
$ledger = Invoke-RestMethod -Uri "$BaseUrl/ledger" -Method Get -Headers $headers

$ledger | Format-Table Id, Type, Amount, Currency, Description, CreatedAt -AutoSize

$debits = ($ledger | Where-Object { $_.Type -eq 'Debit' } | Measure-Object -Property Amount -Sum).Sum
$credits = ($ledger | Where-Object { $_.Type -eq 'Credit' } | Measure-Object -Property Amount -Sum).Sum

Write-Host ""
Write-Host "--- LEDGER TOTALS ---" -ForegroundColor Yellow
Write-Host "Total Debits:  $debits" -ForegroundColor Green
Write-Host "Total Credits: $credits" -ForegroundColor Green

if ($debits -eq $credits) {
    Write-Host "LEDGER BALANCES!" -ForegroundColor Green
} else {
    Write-Host "LEDGER IS OUT OF BALANCE!" -ForegroundColor Red
}
