# Copilot Instructions

## Build, validation, and smoke-test commands

- The .NET projects in `src\web` and `src\loadtest` both target `net10.0`.
- The Node demos expect Node.js 18+.

### Primary ASP.NET app (`src\web`)

```powershell
dotnet build .\src\web\DemoMonitorApp.csproj
dotnet run --project .\src\web\DemoMonitorApp.csproj
dotnet publish .\src\web\DemoMonitorApp.csproj -c Release -o .\src\web\bin\Release\publish
```

### Azure Functions load generator (`src\loadtest`)

```powershell
dotnet build .\src\loadtest\LoadTestFunction.csproj
dotnet publish .\src\loadtest\LoadTestFunction.csproj -c Release -o .\src\loadtest\bin\Release\publish
```

### Alternative Node demo (`src\webapp-simple`)

```powershell
Set-Location .\src\webapp-simple
npm install
npm start
```

### Infrastructure and script validation

```powershell
az deployment group validate --resource-group <rg> --template-file .\infra\main.json --parameters .\infra\main.parameters.json
```

```powershell
Get-ChildItem -Path .\scripts -Filter "*.ps1" | ForEach-Object {
  $errors = $null
  $null = [System.Management.Automation.PSParser]::Tokenize((Get-Content $_.FullName -Raw), [ref]$errors)
  if ($errors) { throw "Syntax errors in $($_.Name): $errors" }
}
```

### Smoke tests

```powershell
.\scripts\final-test.ps1 -AppUrl https://<app>.azurewebsites.net -RequestCount 20
```

Single endpoint check:

```powershell
Invoke-WebRequest -Uri "https://<app>.azurewebsites.net/api/health" -Method GET
```

End-to-end demo traffic:

```powershell
.\scripts\demo-final.ps1
```

## High-level architecture

- `infra\main.json` is the source of truth for the Azure topology. It provisions a Log Analytics workspace, workspace-based Application Insights, Storage account, Azure SQL server/database, App Service plan, ASP.NET web app, Azure Functions load generator, and metric alerts.
- `scripts\deploy.ps1` is the orchestration entry point. It creates the resource group, deploys the ARM template, publishes `src\web` and `src\loadtest`, zip-deploys both apps, and then prints the manual SQL commands required to grant the web app managed identity database access.
- `src\web\Program.cs` is the current primary app that gets deployed. It is a minimal API that redirects `/` to Swagger and intentionally emits Application Insights events, metrics, exceptions, and dependency telemetry from demo endpoints such as `/api/health`, `/api/products`, `/api/simulate-error`, `/api/load-test`, and `/api/memory-test`.
- `src\loadtest\LoadGenerator.cs` exists to keep telemetry flowing without manual clicks. The function has both an HTTP trigger and a 5-minute timer trigger, and it calls the web app through `TARGET_WEB_APP_URL`.
- `src\webapp-simple\server.js` and `src\web-node` are alternative Node demos. They expose a similar idea, but the current ARM deployment, smoke-test scripts, and Azure Functions traffic generator are aligned to the ASP.NET app in `src\web`.

## Key conventions

- Treat `src\web` as the primary deployed application, even though older documentation still describes the Node demo first.
- Keep endpoint changes synchronized across `src\web\Program.cs`, `src\loadtest\LoadGenerator.cs`, and the PowerShell scripts in `scripts\`. This repo relies on those three surfaces staying aligned.
- Be careful with legacy endpoint names in older docs. The active ASP.NET demo uses `/api/*` routes; some older markdown still references `/error`, `/load`, `/memory`, or `/dependencies` from the Node version.
- Telemetry is intentionally explicit and demo-oriented. When adding or changing endpoints, preserve the pattern of manually tracking events, metrics, exceptions, and dependency telemetry inside the handler.
- The deployed web app is wired for managed-identity SQL access. If you change database access or ARM outputs, also review `scripts\deploy.ps1` because it prints the required post-deployment SQL role grants.
- For local Azure Functions work, keep `src\loadtest\local.settings.json` plaintext with `"IsEncrypted": false`.
- CI coverage is narrow: `.github\workflows\validate.yml` validates the ARM template and PowerShell syntax only. There is no formal unit-test project in the repo today.
