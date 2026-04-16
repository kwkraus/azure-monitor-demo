# Script completo para probar el entorno de Azure Monitor Demo
param(
    [Parameter(Mandatory=$false)]
    [string]$AppUrl = "https://app-7mxh7u3uxpfy6.azurewebsites.net",
    
    [Parameter(Mandatory=$false)]
    [int]$RequestCount = 20
)

Write-Host "🎯 Azure Monitor Demo - Test Suite" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan
Write-Host "App URL: $AppUrl" -ForegroundColor White
Write-Host "Request Count: $RequestCount" -ForegroundColor White
Write-Host ""

# Test endpoints
$endpoints = @(
    @{Name="Home Page"; Path=""; ExpectedCode=200},
    @{Name="Health Check"; Path="/api/health"; ExpectedCode=200},
    @{Name="Products API"; Path="/api/products"; ExpectedCode=200},
    @{Name="Error Simulation"; Path="/api/simulate-error"; ExpectedCode="200|500"},
    @{Name="Load Test"; Path="/api/load-test"; ExpectedCode=200},
    @{Name="Memory Test"; Path="/api/memory-test"; ExpectedCode=200}
)

# Test each endpoint once
Write-Host "🔍 Testing individual endpoints..." -ForegroundColor Yellow
foreach ($endpoint in $endpoints) {
    $url = "$AppUrl$($endpoint.Path)"
    try {
        $response = Invoke-WebRequest -Uri $url -Method GET -TimeoutSec 15
        if ($endpoint.ExpectedCode -match $response.StatusCode) {
            Write-Host "  ✅ $($endpoint.Name) - Status: $($response.StatusCode)" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️ $($endpoint.Name) - Unexpected status: $($response.StatusCode)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "  ❌ $($endpoint.Name) - Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    Start-Sleep -Milliseconds 500
}

Write-Host ""
Write-Host "🔄 Generating demo traffic ($RequestCount requests)..." -ForegroundColor Yellow

# Generate traffic for demo
$successCount = 0
$errorCount = 0

for ($i = 1; $i -le $RequestCount; $i++) {
    $endpoint = $endpoints[1..5] | Get-Random  # Exclude home page
    $url = "$AppUrl$($endpoint.Path)"
    
    try {
        $response = Invoke-WebRequest -Uri $url -Method GET -TimeoutSec 30
        if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 500) {
            $successCount++
            Write-Host "  [$i/$RequestCount] $($endpoint.Name) - ✅ $($response.StatusCode)" -ForegroundColor Green
        } else {
            Write-Host "  [$i/$RequestCount] $($endpoint.Name) - ⚠️ $($response.StatusCode)" -ForegroundColor Yellow
        }
    }
    catch {
        $errorCount++
        Write-Host "  [$i/$RequestCount] $($endpoint.Name) - ❌ Error" -ForegroundColor Red
    }
    
    Start-Sleep -Milliseconds (Get-Random -Minimum 200 -Maximum 800)
}

Write-Host ""
Write-Host "📊 Test Results:" -ForegroundColor Cyan
Write-Host "===============" -ForegroundColor Cyan
Write-Host "✅ Successful requests: $successCount" -ForegroundColor Green
Write-Host "❌ Failed requests: $errorCount" -ForegroundColor Red
Write-Host "📈 Success rate: $([math]::Round(($successCount / $RequestCount) * 100, 1))%" -ForegroundColor White

Write-Host ""
Write-Host "🎪 Ready for your demo!" -ForegroundColor Green
Write-Host "======================" -ForegroundColor Green
Write-Host ""
Write-Host "🌐 Application URLs:" -ForegroundColor Cyan
Write-Host "  Main App: $AppUrl" -ForegroundColor White
Write-Host "  Health: $AppUrl/api/health" -ForegroundColor White
Write-Host "  Products: $AppUrl/api/products" -ForegroundColor White
Write-Host "  Errors: $AppUrl/api/simulate-error" -ForegroundColor White
Write-Host "  Load Test: $AppUrl/api/load-test" -ForegroundColor White
Write-Host "  Memory Test: $AppUrl/api/memory-test" -ForegroundColor White

Write-Host ""
Write-Host "📊 Azure Portal Links:" -ForegroundColor Cyan
Write-Host "  Resource Group: https://portal.azure.com/#@/resource/subscriptions//resourceGroups/demo-monitor-rg/overview" -ForegroundColor White
Write-Host "  Application Insights: https://portal.azure.com/#blade/HubsExtension/BrowseResource/resourceType/microsoft.insights%2Fcomponents" -ForegroundColor White

Write-Host ""
Write-Host "💡 Demo Tips:" -ForegroundColor Yellow
Write-Host "1. Open Application Insights → Live Metrics" -ForegroundColor White
Write-Host "2. Run this script again while showing Live Metrics" -ForegroundColor White
Write-Host "3. Show Application Map for dependency visualization" -ForegroundColor White
Write-Host "4. Demonstrate alert configuration" -ForegroundColor White
Write-Host "5. Create custom dashboards with the generated metrics" -ForegroundColor White

Write-Host ""
Write-Host "🚀 To generate continuous traffic during demo:" -ForegroundColor Green
Write-Host "   .\final-test.ps1 -RequestCount 50" -ForegroundColor White
