# Demo Final - Azure Monitor & Application Insights
# Script to demonstrate all monitoring environment capabilities

Write-Host "🚀 DEMO: Azure Monitor & Application Insights" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "https://app-7mxh7u3uxpfy6.azurewebsites.net"
$resourceGroup = "rg-demo-monitorca"

# 1. Check application status
Write-Host "1. 📊 Checking application status..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/api/health" -Method Get -TimeoutSec 10
    Write-Host "   ✅ Application: HEALTHY" -ForegroundColor Green
    Write-Host "   ⏱️  Uptime: $([math]::Round($health.uptime, 2)) seconds" -ForegroundColor Green
    Write-Host "   💾 Memory: $([math]::Round($health.memory.heapUsed / 1MB, 2)) MB" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Error checking health: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 2. Test products API
Write-Host "2. 🛍️  Testing products API..." -ForegroundColor Yellow
try {
    $products = Invoke-RestMethod -Uri "$baseUrl/api/products" -Method Get -TimeoutSec 10
    $productList = @($products)
    Write-Host "   ✅ API working: $($productList.Count) products available" -ForegroundColor Green
    Write-Host "   📦 Products: $($productList | ForEach-Object { $_.Name } | Join-String -Separator ', ')" -ForegroundColor Green
} catch {
    Write-Host "   ❌ API error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# 3. Generate error telemetry
Write-Host "3. 🔥 Generating errors for telemetry..." -ForegroundColor Yellow
$errorCount = 0
for ($i = 1; $i -le 3; $i++) {
    try {
        Invoke-RestMethod -Uri "$baseUrl/error" -Method Get -TimeoutSec 5 | Out-Null
    } catch {
        $errorCount++
        Write-Host "   ✅ Error $i generated correctly (HTTP 500)" -ForegroundColor Green
    }
}
Write-Host "   📊 Total errors generated: $errorCount" -ForegroundColor Green

Write-Host ""

# 4. Load test
Write-Host "4. ⚡ Running load test..." -ForegroundColor Yellow
try {
    $loadResult = Invoke-RestMethod -Uri "$baseUrl/api/load-test?iterations=10000" -Method Get -TimeoutSec 20
    Write-Host "   ✅ Load test completed" -ForegroundColor Green
    Write-Host "   ⏱️  Duration: $($loadResult.duration) ms" -ForegroundColor Green
    Write-Host "   🔢 Iterations: $($loadResult.iterations)" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Load test error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# 5. Memory test
Write-Host "5. 💾 Running memory test..." -ForegroundColor Yellow
try {
    $memResult = Invoke-RestMethod -Uri "$baseUrl/api/memory-test?size=2000000" -Method Get -TimeoutSec 15
    Write-Host "   ✅ Memory test completed" -ForegroundColor Green
    Write-Host "   📈 Memory allocated: $([math]::Round($memResult.allocatedSize / 1MB, 2)) MB" -ForegroundColor Green
    Write-Host "   📊 Heap difference: $([math]::Round($memResult.memoryDifference.heapUsed / 1MB, 2)) MB" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Memory test error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# 6. Dependencies test
Write-Host "6. 🔗 Running dependencies test..." -ForegroundColor Yellow
try {
    $depResult = Invoke-RestMethod -Uri "$baseUrl/dependencies" -Method Get -TimeoutSec 15
    Write-Host "   ✅ Dependencies test completed" -ForegroundColor Green
    Write-Host "   🎯 Simulated dependencies:" -ForegroundColor Green
    foreach ($dep in $depResult.dependencies) {
        Write-Host "      - $($dep.name): $([math]::Round($dep.duration, 2)) ms" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ❌ Dependencies test error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# 7. Generate sustained traffic
Write-Host "7. 🌊 Generating sustained traffic..." -ForegroundColor Yellow
$successCount = 0
$totalRequests = 10
for ($i = 1; $i -le $totalRequests; $i++) {
    try {
        Invoke-RestMethod -Uri "$baseUrl/api/health" -Method Get -TimeoutSec 5 | Out-Null
        $successCount++
        if ($i % 3 -eq 0) {
            Write-Host "   📊 $i/$totalRequests requests completed..." -ForegroundColor Cyan
        }
    } catch {
        Write-Host "   ⚠️  Request $i failed" -ForegroundColor Yellow
    }
    Start-Sleep -Milliseconds 500
}
Write-Host "   ✅ Traffic completed: $successCount/$totalRequests successful requests" -ForegroundColor Green

Write-Host ""

# 8. Environment information
Write-Host "8. 📋 Monitoring environment information:" -ForegroundColor Yellow
Write-Host "   🌐 Application URL: $baseUrl" -ForegroundColor Cyan
Write-Host "   📊 Application Insights: insights-bwkinh757hlog" -ForegroundColor Cyan
Write-Host "   📁 Resource Group: $resourceGroup" -ForegroundColor Cyan
Write-Host "   📍 Region: North Europe" -ForegroundColor Cyan

Write-Host ""

# 9. Useful links
Write-Host "9. 🔗 Useful links for demo:" -ForegroundColor Yellow
Write-Host "   🌐 Web Application: $baseUrl" -ForegroundColor Cyan
Write-Host "   📊 Health Check: $baseUrl/api/health" -ForegroundColor Cyan
Write-Host "   🛍️  Products API: $baseUrl/api/products" -ForegroundColor Cyan
Write-Host "   🔥 Generate Error: $baseUrl/api/simulate-error" -ForegroundColor Cyan
Write-Host "   ⚡ Load Test: $baseUrl/api/load-test?iterations=5000" -ForegroundColor Cyan
Write-Host "   💾 Memory Test: $baseUrl/api/memory-test?size=1000000" -ForegroundColor Cyan
Write-Host "   🔗 Dependencies Test: $baseUrl/dependencies" -ForegroundColor Cyan

Write-Host ""
Write-Host "✅ DEMO COMPLETED SUCCESSFULLY" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "📌 Next steps for presentation:" -ForegroundColor White
Write-Host "   1. Open Azure Portal -> Application Insights 'insights-bwkinh757hlog'" -ForegroundColor White
Write-Host "   2. Review real-time metrics (Live Metrics)" -ForegroundColor White
Write-Host "   3. Check captured errors (Failures)" -ForegroundColor White
Write-Host "   4. Review performance (Performance)" -ForegroundColor White
Write-Host "   5. Check configured alerts (Alerts)" -ForegroundColor White
Write-Host "   6. Show logs and queries in Log Analytics" -ForegroundColor White

Write-Host ""
Write-Host "🎯 The application is generating complete telemetry for the demo!" -ForegroundColor Green
