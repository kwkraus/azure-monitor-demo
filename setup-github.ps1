# 🚀 Script para configurar GitHub y subir el repositorio
# Ejecutar paso a paso para conectar con GitHub

Write-Host "🎯 CONFIGURACIÓN DE GITHUB - Azure Monitor Demo" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# Paso 1: Verificar configuración Git actual
Write-Host "`n📋 Paso 1: Verificación de configuración Git" -ForegroundColor Yellow
git config --list --global

# Paso 2: Mostrar estado actual del repositorio
Write-Host "`n📦 Paso 2: Estado actual del repositorio" -ForegroundColor Yellow
git status
git log --oneline -5

# Paso 3: Instrucciones para crear repositorio en GitHub
Write-Host "`n🌐 Paso 3: Crear repositorio en GitHub" -ForegroundColor Yellow
Write-Host "Ve a: https://github.com/new" -ForegroundColor Green
Write-Host "Configuración recomendada:" -ForegroundColor White
Write-Host "  - Repository name: azure-monitor-demo" -ForegroundColor Gray
Write-Host "  - Description: Complete Azure Monitor and Application Insights demo environment for client presentations" -ForegroundColor Gray
Write-Host "  - Public repository" -ForegroundColor Gray
Write-Host "  - NO inicializar con README (ya tenemos uno)" -ForegroundColor Gray
Write-Host "  - NO agregar .gitignore (ya tenemos uno)" -ForegroundColor Gray
Write-Host "  - Selecciona MIT License si quieres" -ForegroundColor Gray

# Pausa para que el usuario cree el repositorio
Write-Host "`n⏸️  PAUSA: Crea el repositorio en GitHub y presiona Enter para continuar..." -ForegroundColor Red
Read-Host

# Paso 4: Obtener URL del repositorio
Write-Host "`n🔗 Paso 4: Configuración del remote" -ForegroundColor Yellow
$githubUsername = Read-Host "Ingresa tu username de GitHub"
$repoName = Read-Host "Ingresa el nombre del repositorio (default: azure-monitor-demo)"
if ([string]::IsNullOrEmpty($repoName)) {
    $repoName = "azure-monitor-demo"
}

$repoUrl = "https://github.com/$githubUsername/$repoName.git"
Write-Host "URL del repositorio: $repoUrl" -ForegroundColor Green

# Paso 5: Agregar remote y subir código
Write-Host "`n🚀 Paso 5: Conectar y subir al repositorio" -ForegroundColor Yellow
try {
    git remote add origin $repoUrl
    Write-Host "✅ Remote agregado exitosamente" -ForegroundColor Green
    
    git branch -M main
    Write-Host "✅ Branch renombrado a 'main'" -ForegroundColor Green
    
    Write-Host "🔄 Subiendo código a GitHub..." -ForegroundColor Yellow
    git push -u origin main
    Write-Host "✅ ¡Código subido exitosamente!" -ForegroundColor Green
    
    # Abrir repositorio en navegador
    Write-Host "`n🌐 Abriendo repositorio en navegador..." -ForegroundColor Yellow
    Start-Process $repoUrl.Replace('.git', '')
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "💡 Comandos manuales de respaldo:" -ForegroundColor Yellow
    Write-Host "git remote add origin $repoUrl" -ForegroundColor Gray
    Write-Host "git branch -M main" -ForegroundColor Gray
    Write-Host "git push -u origin main" -ForegroundColor Gray
}

Write-Host "`n🎉 ¡REPOSITORIO CONFIGURADO!" -ForegroundColor Green
Write-Host "Tu proyecto está ahora en: $repoUrl" -ForegroundColor Green
Write-Host "`n📊 URLs importantes:" -ForegroundColor Cyan
Write-Host "- GitHub Repo: $($repoUrl.Replace('.git', ''))" -ForegroundColor White
Write-Host "- Demo App: https://app-7mxh7u3uxpfy6.azurewebsites.net" -ForegroundColor White
Write-Host "- Demo Guide: docs/DEMO-GUIDE.md" -ForegroundColor White
