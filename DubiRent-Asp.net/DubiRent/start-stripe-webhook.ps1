# Script to start Stripe webhook listener for local development
# This forwards webhook events from Stripe to your local application

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "  Stripe Webhook Listener" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

# Check if Stripe CLI is installed
$stripePath = Get-Command stripe -ErrorAction SilentlyContinue
if (-not $stripePath) {
    Write-Host "❌ Stripe CLI is not installed!" -ForegroundColor Red
    Write-Host "Please run: cd DubiRent; .\install-stripe-cli-simple.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Stripe CLI found: " -NoNewline -ForegroundColor Green
stripe --version
Write-Host ""

# Check if user is logged in
Write-Host "Checking Stripe login status..." -ForegroundColor Yellow
$loginCheck = stripe config --list 2>&1
if ($LASTEXITCODE -ne 0 -or $loginCheck -match "No API key") {
    Write-Host "⚠️  You need to login to Stripe first!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Starting login process..." -ForegroundColor Cyan
    Write-Host "This will open a browser window for authentication." -ForegroundColor Gray
    Write-Host ""
    
    stripe login
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Login failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    Write-Host "✅ Login successful!" -ForegroundColor Green
    Write-Host ""
}

# Get the webhook URL (you can change this if your app runs on a different port)
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "  Configuration" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

$port = Read-Host "Enter your application port [default: 5001]"
if ([string]::IsNullOrWhiteSpace($port)) {
    $port = "5001"
}

$protocol = Read-Host "Enter protocol (https/http) [default: https]"
if ([string]::IsNullOrWhiteSpace($protocol)) {
    $protocol = "https"
}

$webhookUrl = "$protocol://localhost:$port/Payment/Webhook"

Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
Write-Host "  Starting Webhook Listener" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
Write-Host ""
Write-Host "Webhook URL: $webhookUrl" -ForegroundColor Cyan
Write-Host ""
Write-Host "⚠️  IMPORTANT:" -ForegroundColor Yellow
Write-Host "1. Make sure your application is running!" -ForegroundColor White
Write-Host "2. The webhook secret will be displayed below (whsec_...)" -ForegroundColor White
Write-Host "3. Copy it and add to appsettings.json -> Stripe -> WebhookSecret" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to stop the webhook listener" -ForegroundColor Gray
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
Write-Host ""

# Start the webhook listener
stripe listen --forward-to $webhookUrl

