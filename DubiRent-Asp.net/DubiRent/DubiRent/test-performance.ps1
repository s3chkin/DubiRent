# Performance Testing Script
# This script tests if performance optimizations are working correctly

Write-Host "🚀 Testing DubiRent Performance Optimizations..." -ForegroundColor Cyan
Write-Host ""

# Test 1: Check if Response Compression headers are present
Write-Host "Test 1: Checking Response Compression..." -ForegroundColor Yellow
$response = Invoke-WebRequest -Uri "http://localhost:5000" -Method GET -UseBasicParsing -ErrorAction SilentlyContinue
if ($response.Headers["Content-Encoding"]) {
    Write-Host "✅ Compression is working! Content-Encoding: $($response.Headers['Content-Encoding'])" -ForegroundColor Green
} else {
    Write-Host "⚠️ Compression headers not found (may need Accept-Encoding header)" -ForegroundColor Yellow
}

# Test 2: Check Cache-Control headers on static files
Write-Host ""
Write-Host "Test 2: Checking Static File Caching..." -ForegroundColor Yellow
$cssResponse = Invoke-WebRequest -Uri "http://localhost:5000/css/site.css" -Method GET -UseBasicParsing -ErrorAction SilentlyContinue
if ($cssResponse.Headers["Cache-Control"]) {
    $cacheControl = $cssResponse.Headers["Cache-Control"]
    Write-Host "✅ Cache-Control header found: $cacheControl" -ForegroundColor Green
    if ($cacheControl -like "*immutable*") {
        Write-Host "✅ Static files are marked as immutable (excellent!)" -ForegroundColor Green
    }
} else {
    Write-Host "❌ Cache-Control header not found on static files" -ForegroundColor Red
}

# Test 3: Check Response Cache headers on pages
Write-Host ""
Write-Host "Test 3: Checking Response Caching on Pages..." -ForegroundColor Yellow
$homeResponse = Invoke-WebRequest -Uri "http://localhost:5000" -Method GET -UseBasicParsing -ErrorAction SilentlyContinue
if ($homeResponse.Headers["Cache-Control"]) {
    Write-Host "✅ Response Cache-Control: $($homeResponse.Headers['Cache-Control'])" -ForegroundColor Green
} else {
    Write-Host "⚠️ No Cache-Control header (may be configured differently)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "📊 Performance Test Summary:" -ForegroundColor Cyan
Write-Host "  - Response Compression: $(if ($response.Headers['Content-Encoding']) { '✅ Working' } else { '⚠️ Check manually' })"
Write-Host "  - Static File Caching: $(if ($cssResponse.Headers['Cache-Control']) { '✅ Working' } else { '❌ Not Working' })"
Write-Host ""
Write-Host "💡 Tip: Start the application with 'dotnet run' first!" -ForegroundColor Yellow

