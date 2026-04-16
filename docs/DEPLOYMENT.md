# 📖 Deployment Guide - Azure Monitor Demo

This guide provides detailed instructions for deploying the Azure Monitor and Application Insights demo environment.

## 📋 Prerequisites

### Required Software
- **Azure CLI** 2.40.0 or higher
- **PowerShell** 5.1 or higher (Windows) / PowerShell Core 7.0+ (Cross-platform)
- **Git** (to clone the repository)

### Azure Configuration
- Active Azure subscription
- Contributor permissions on the subscription
- Azure CLI authenticated (`az login`)

### Prerequisites Verification
```powershell
# Verify Azure CLI
az --version

# Verify authentication
az account show

# Verify PowerShell
$PSVersionTable.PSVersion
```

## 🚀 Deployment Steps

### 1. Environment Preparation

#### Clone Repository
```bash
git clone https://github.com/your-username/azure-monitor-demo.git
cd azure-monitor-demo
```

#### Configure Variables (Optional)
Edit `infra/main.parameters.json` to customize:
- **Azure Region**: `location`
- **Resource Prefix**: `resourcePrefix`
- **SKU Configuration**: To optimize costs

### 2. Parameters Configuration

#### main.parameters.json File
```json
{
    "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
    "contentVersion": "1.0.0.0",
    "parameters": {
        "location": {
            "value": "North Europe"
        },
        "resourcePrefix": {
            "value": "demo"
        },
        "sqlAdminLogin": {
            "value": "sqladmin"
        },
        "sqlAdminPassword": {
            "value": "ComplexPassword123!"
        }
    }
}
```

### 3. Ejecución del Despliegue

#### Opción A: Script Automatizado (Recomendado)
```powershell
# Despliegue con valores por defecto
.\deploy.ps1

# Despliegue con parámetros personalizados
.\deploy.ps1 -ResourceGroupName "mi-demo-rg" -Location "West Europe"
```

#### Opción B: Comandos Manuales
```powershell
# Definir variables
$resourceGroup = "demo-monitor-rg"
$location = "North Europe"

# Crear resource group
az group create --name $resourceGroup --location $location

# Desplegar ARM template
az deployment group create `
    --resource-group $resourceGroup `
    --template-file "infra/main.json" `
    --parameters "infra/main.parameters.json"
```

### 4. Post-Despliegue

#### Azure SQL con Microsoft Entra ID only
La plantilla ahora configura Azure SQL con Microsoft Entra-only authentication. Para que la aplicación web acceda a la base de datos con su managed identity:

1. Asegúrate de que `sqlEntraAdminLogin`, `sqlEntraAdminObjectId` y `sqlEntraAdminTenantId` en `infra/main.parameters.json` correspondan al usuario o grupo de Microsoft Entra que administrará el servidor SQL.
2. Despliega la infraestructura.
3. Conéctate al SQL Database como ese administrador de Microsoft Entra.
4. Crea un usuario contenido para la managed identity del App Service y asígnale los roles necesarios:

```sql
CREATE USER [<web-app-name>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<web-app-name>];
ALTER ROLE db_datawriter ADD MEMBER [<web-app-name>];
ALTER ROLE db_ddladmin ADD MEMBER [<web-app-name>];
```

La cadena de conexión del App Service se publica con `Authentication=Active Directory Managed Identity`, por lo que no usa usuario/contraseña SQL para conectarse.

5. Durante el arranque, la aplicación ejecuta las migraciones de Entity Framework Core y siembra los datos iniciales del demo si la tabla `Products` está vacía.

Si la base de datos ya existe (el caso normal de este repo, porque `infra/main.json` la crea), la managed identity solo necesita permisos para aplicar cambios de esquema y leer/escribir datos. Si quieres que la aplicación también cree la base de datos en un servidor donde todavía no existe, la identidad también debe tener permisos de creación de base de datos en ese servidor.

Para desarrollo local, configura `ConnectionStrings__DefaultConnection` con una cadena de conexión de Azure SQL que use `Authentication=Active Directory Default` y conéctate después de autenticarte con `az login`.

#### Configurar la Aplicación
```powershell
# Obtener información de deployment
$appName = az deployment group show --resource-group $resourceGroup --name "main" --query "properties.outputs.appServiceName.value" --output tsv

# Configurar App Service para Node.js
az webapp config appsettings set --name $appName --resource-group $resourceGroup --settings WEBSITE_NODE_DEFAULT_VERSION=18.12.0

# Desplegar aplicación
az webapp deployment source config-zip --name $appName --resource-group $resourceGroup --src "webapp-simple-deploy.zip"
```

#### Verificar Deployment
```powershell
# Ejecutar script de verificación
.\demo-final.ps1
```

## 🔧 Configuraciones Avanzadas

### Personalización de Alertas

#### Modificar Umbrales en ARM Template
En `infra/main.json`, buscar las secciones de alertas:

```json
{
    "name": "High Response Time Alert",
    "properties": {
        "criteria": {
            "allOf": [{
                "threshold": 2000,  // Cambiar umbral aquí
                "timeAggregation": "Average"
            }]
        }
    }
}
```

### Configuración de Application Insights

#### Variables de Entorno Personalizadas
```powershell
# Configurar sampling rate
az webapp config appsettings set --name $appName --resource-group $resourceGroup --settings APPINSIGHTS_SAMPLING_PERCENTAGE=50

# Configurar log level
az webapp config appsettings set --name $appName --resource-group $resourceGroup --settings APPINSIGHTS_LOG_LEVEL=Information
```

### Configuración de Escalabilidad

#### Auto-scaling Rules
```powershell
# Configurar auto-scaling basado en CPU
az monitor autoscale create --resource-group $resourceGroup --resource $appName --resource-type Microsoft.Web/serverfarms --name autoscale-$appName --min-count 1 --max-count 3 --count 1

# Agregar regla de scale-out
az monitor autoscale rule create --resource-group $resourceGroup --autoscale-name autoscale-$appName --condition "Percentage CPU > 70 avg 5m" --scale out 1
```

## 📊 Validación del Despliegue

### Checklist de Verificación

#### ✅ Recursos Desplegados
- [ ] Resource Group creado
- [ ] App Service funcionando
- [ ] Application Insights configurado
- [ ] SQL Database online
- [ ] Log Analytics Workspace activo
- [ ] Storage Account disponible
- [ ] Azure Functions desplegado
- [ ] Alertas configuradas

#### ✅ Aplicación Funcionando
```powershell
# Verificar endpoints principales
$baseUrl = "https://$appName.azurewebsites.net"

# Health check
Invoke-RestMethod -Uri "$baseUrl/health"

# API de productos
Invoke-RestMethod -Uri "$baseUrl/api/products"

# Generar error (debe retornar 500)
try { Invoke-RestMethod -Uri "$baseUrl/error" } catch { "Error generado correctamente" }
```

#### ✅ Telemetría Funcionando
1. Abrir Azure Portal → Application Insights
2. Verificar Live Metrics Stream
3. Generar tráfico y verificar métricas
4. Comprobar que aparecen logs

### Scripts de Validación

#### Script de Health Check Completo
```powershell
# Archivo: validate-deployment.ps1
param(
    [string]$ResourceGroupName = "demo-monitor-rg",
    [string]$AppName
)

Write-Host "🔍 Validando deployment..." -ForegroundColor Yellow

# Verificar recursos
$resources = az resource list --resource-group $ResourceGroupName --query "[].{name:name, type:type, provisioningState:properties.provisioningState}" --output table
Write-Host "✅ Recursos desplegados:" -ForegroundColor Green
$resources

# Verificar aplicación
if ($AppName) {
    $appUrl = "https://$AppName.azurewebsites.net"
    try {
        $health = Invoke-RestMethod -Uri "$appUrl/health" -TimeoutSec 10
        Write-Host "✅ Aplicación funcionando: $($health.status)" -ForegroundColor Green
    } catch {
        Write-Host "❌ Error en aplicación: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "🎯 Validación completada" -ForegroundColor Green
```

## 🚨 Solución de Problemas

### Errores Comunes

#### Error: Resource Group ya existe
```powershell
# Verificar recursos existentes
az resource list --resource-group $resourceGroup --output table

# Eliminar si es necesario
az group delete --name $resourceGroup --yes --no-wait
```

#### Error: Deployment timeout
```powershell
# Verificar estado del deployment
az deployment group show --resource-group $resourceGroup --name "main" --query "properties.provisioningState"

# Revisar errores específicos
az deployment group show --resource-group $resourceGroup --name "main" --query "properties.error"
```

#### Error: App Service no responde
```powershell
# Verificar logs
az webapp log tail --name $appName --resource-group $resourceGroup

# Reiniciar si es necesario
az webapp restart --name $appName --resource-group $resourceGroup
```

#### Error: Sin datos en Application Insights
```powershell
# Verificar connection string
az webapp config appsettings list --name $appName --resource-group $resourceGroup --query "[?name=='APPLICATIONINSIGHTS_CONNECTION_STRING']"

# Generar tráfico de prueba
for ($i=1; $i -le 10; $i++) {
    Invoke-RestMethod -Uri "https://$appName.azurewebsites.net/health" | Out-Null
    Start-Sleep 1
}
```

### Logs y Diagnósticos

#### Habilitar Logging Detallado
```powershell
# Habilitar application logging
az webapp log config --name $appName --resource-group $resourceGroup --application-logging filesystem

# Habilitar web server logging
az webapp log config --name $appName --resource-group $resourceGroup --web-server-logging filesystem

# Ver logs en tiempo real
az webapp log tail --name $appName --resource-group $resourceGroup
```

#### Descargar Logs
```powershell
# Descargar logs de deployment
az webapp deployment source show --name $appName --resource-group $resourceGroup

# Descargar logs de aplicación
az webapp log download --name $appName --resource-group $resourceGroup --log-file app-logs.zip
```

## 🧹 Limpieza de Recursos

### Eliminar Entorno Completo
```powershell
# Eliminar resource group y todos los recursos
az group delete --name $resourceGroup --yes --no-wait

# Verificar eliminación
az group exists --name $resourceGroup
```

### Eliminar Recursos Específicos
```powershell
# Eliminar solo la aplicación
az webapp delete --name $appName --resource-group $resourceGroup

# Eliminar solo Application Insights
az monitor app-insights component delete --app $appInsightsName --resource-group $resourceGroup
```

## 📈 Optimizaciones

### Performance
- Configurar CDN para contenido estático
- Implementar caching strategies
- Optimizar queries de Application Insights

### Costos
- Usar tiers básicos para demos
- Configurar retention policies apropiadas
- Implementar auto-shutdown para entornos temporales

### Seguridad
- Configurar managed identities
- Implementar network security groups
- Configurar private endpoints para producción

---

## 📞 Soporte

Para problemas durante el despliegue:
1. Verificar prerrequisitos
2. Revisar logs de Azure CLI
3. Consultar documentación de Azure
4. Crear issue en el repositorio del proyecto

---

**¡El entorno estará listo para la demostración en 10-15 minutos!** 🚀
