# Stripe CLI Installation Script for Windows
# This script downloads and installs Stripe CLI

Write-Host "Installing Stripe CLI..." -ForegroundColor Cyan

# Check if already installed
$stripePath = Get-Command stripe -ErrorAction SilentlyContinue
if ($stripePath) {
    Write-Host "Stripe CLI is already installed!" -ForegroundColor Green
    stripe --version
    exit 0
}

# Create temp directory
$tempDir = "$env:TEMP\stripe-cli-install"
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

try {
    # Download Stripe CLI from GitHub releases
    $latestReleaseUrl = "https://github.com/stripe/stripe-cli/releases/latest"
    
    Write-Host "Fetching latest release information..." -ForegroundColor Yellow
    
    # Get latest release version
    $response = Invoke-WebRequest -Uri "https://api.github.com/repos/stripe/stripe-cli/releases/latest" -UseBasicParsing
    $releaseInfo = $response.Content | ConvertFrom-Json
    $version = $releaseInfo.tag_name -replace 'v', ''
    
    Write-Host "Latest version: $version" -ForegroundColor Green
    
    # Download MSI installer for Windows
    # Get the MSI file from release assets
    $msiAsset = $releaseInfo.assets | Where-Object { $_.name -like "*windows*.msi" } | Select-Object -First 1
    
    if (-not $msiAsset) {
        throw "Could not find Windows MSI installer in release assets. Please download manually from: https://github.com/stripe/stripe-cli/releases/latest"
    }
    
    $msiUrl = $msiAsset.browser_download_url
    $msiPath = Join-Path $tempDir "stripe-cli.msi"
    
    Write-Host "Downloading Stripe CLI installer..." -ForegroundColor Yellow
    Write-Host "File: $($msiAsset.name)" -ForegroundColor Gray
    Write-Host "URL: $msiUrl" -ForegroundColor Gray
    
    Invoke-WebRequest -Uri $msiUrl -OutFile $msiPath -UseBasicParsing
    
    Write-Host "Installing Stripe CLI..." -ForegroundColor Yellow
    Write-Host "Please accept the installation dialog if it appears." -ForegroundColor Yellow
    
    # Install MSI silently
    Start-Process msiexec.exe -Wait -ArgumentList "/i `"$msiPath`" /quiet /norestart"
    
    # Wait a moment for installation to complete
    Start-Sleep -Seconds 3
    
    # Refresh PATH environment variable for current session
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
    
    # Verify installation
    Start-Sleep -Seconds 2
    $stripePath = Get-Command stripe -ErrorAction SilentlyContinue
    
    if ($stripePath) {
        Write-Host "`n✅ Stripe CLI installed successfully!" -ForegroundColor Green
        Write-Host "Version:" -ForegroundColor Cyan
        stripe --version
        Write-Host "`nNext steps:" -ForegroundColor Yellow
        Write-Host "1. Run: stripe login" -ForegroundColor White
        Write-Host "2. Then: stripe listen --forward-to https://localhost:5001/Payment/Webhook" -ForegroundColor White
    } else {
        Write-Host "`n⚠️ Installation completed but Stripe CLI not found in PATH." -ForegroundColor Yellow
        Write-Host "You may need to:" -ForegroundColor Yellow
        Write-Host "1. Restart your terminal/PowerShell" -ForegroundColor White
        Write-Host "2. Or manually add Stripe CLI to your PATH" -ForegroundColor White
        Write-Host "3. Default installation path: C:\Program Files\Stripe\stripe.exe" -ForegroundColor White
    }
    
} catch {
    Write-Host "`n❌ Error installing Stripe CLI:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "`nAlternative: Download manually from:" -ForegroundColor Yellow
    Write-Host "https://github.com/stripe/stripe-cli/releases/latest" -ForegroundColor Cyan
    exit 1
} finally {
    # Cleanup
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

