# Simple Stripe CLI Installation Script
Write-Host "=== Stripe CLI Installation ===" -ForegroundColor Cyan
Write-Host ""

# Get latest release
Write-Host "Fetching latest release..." -ForegroundColor Yellow
try {
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/stripe/stripe-cli/releases/latest"
    $version = $release.tag_name
    Write-Host "Latest version: $version" -ForegroundColor Green
    
    # Find Windows installer (try ZIP first, then MSI)
    $installer = $release.assets | Where-Object { 
        ($_.name -like "*windows*x86_64.zip") -or 
        ($_.name -like "*windows*.msi") 
    } | Select-Object -First 1
    
    if (-not $installer) {
        Write-Host "❌ Could not find Windows installer!" -ForegroundColor Red
        Write-Host "Please download manually from: https://github.com/stripe/stripe-cli/releases/latest" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "Found installer: $($installer.name)" -ForegroundColor Green
    Write-Host "Download URL: $($installer.browser_download_url)" -ForegroundColor Gray
    
    # Download
    $tempDir = "$env:TEMP\stripe-cli-install"
    New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
    $installerPath = Join-Path $tempDir $installer.name
    
    Write-Host "`nDownloading..." -ForegroundColor Yellow
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $installer.browser_download_url -OutFile $installerPath -UseBasicParsing
    
    Write-Host "Downloaded to: $installerPath" -ForegroundColor Green
    
    # Install
    Write-Host "`nInstalling..." -ForegroundColor Yellow
    if ($installer.name -like "*.msi") {
        Start-Process msiexec.exe -ArgumentList "/i `"$installerPath`" /quiet /norestart" -Wait -NoNewWindow
    } elseif ($installer.name -like "*.zip") {
        # Extract ZIP
        Expand-Archive -Path $installerPath -DestinationPath $tempDir -Force
        $stripeExe = Get-ChildItem -Path $tempDir -Filter "stripe.exe" -Recurse | Select-Object -First 1
        if ($stripeExe) {
            # Install to user's local bin directory
            $installDir = "$env:LOCALAPPDATA\Stripe"
            New-Item -ItemType Directory -Force -Path $installDir | Out-Null
            Copy-Item -Path $stripeExe.FullName -Destination "$installDir\stripe.exe" -Force
            
            # Add to PATH for current session
            $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
            if ($currentPath -notlike "*$installDir*") {
                [Environment]::SetEnvironmentVariable("Path", "$currentPath;$installDir", "User")
                $env:Path += ";$installDir"
            }
            
            Write-Host "Installed to: $installDir" -ForegroundColor Green
        }
    }
    
    Start-Sleep -Seconds 2
    
    # Verify
    Write-Host "`nVerifying installation..." -ForegroundColor Yellow
    $stripeCheck = Get-Command stripe -ErrorAction SilentlyContinue
    if ($stripeCheck) {
        Write-Host "✅ Stripe CLI installed successfully!" -ForegroundColor Green
        Write-Host ""
        stripe --version
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Cyan
        Write-Host "1. Run: stripe login" -ForegroundColor White
        Write-Host "2. Then: .\start-stripe-webhook.ps1" -ForegroundColor White
    } else {
        Write-Host "⚠️ Installation completed but Stripe CLI not found in PATH." -ForegroundColor Yellow
        Write-Host "Please restart your terminal and run 'stripe --version'" -ForegroundColor Yellow
    }
    
    # Cleanup
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`nPlease download manually from:" -ForegroundColor Yellow
    Write-Host "https://github.com/stripe/stripe-cli/releases/latest" -ForegroundColor Cyan
    exit 1
}

