# Run from the folder that contains this file:   ./setup.ps1
# Creates the solution, picks the right .NET version, builds, runs the tests, creates the database.
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

function Run([string]$what, [scriptblock]$command) {
    Write-Host "`n==> $what" -ForegroundColor Cyan
    & $command
    if ($LASTEXITCODE -ne 0) { throw "Step failed: $what" }
}

# 1) Match the project to the installed SDK (8, 9 or 10)
$sdk = (dotnet --version).Trim()
$major = [int]($sdk.Split('.')[0])
if ($major -lt 8) { throw "Please install the .NET 8 SDK or newer. Found: $sdk" }
Write-Host "Using .NET SDK $sdk  (target: net$major.0)" -ForegroundColor Green

@"
<Project>
  <PropertyGroup>
    <TargetFramework>net$major.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <MicrosoftPackagesVersion>$major.0.*</MicrosoftPackagesVersion>
  </PropertyGroup>
</Project>
"@ | Set-Content -Path "Directory.Build.props" -Encoding UTF8

# 2) Solution
if (-not (Test-Path "ServiceDeskManagement.sln") -and -not (Test-Path "ServiceDeskManagement.slnx")) {
    Run "Create solution" { dotnet new sln -n ServiceDeskManagement }
}
dotnet sln add src/ServiceDesk.Domain src/ServiceDesk.Application src/ServiceDesk.Infrastructure src/ServiceDesk.API tests/ServiceDesk.UnitTests | Out-Null

# 3) Build and test
Run "Restore packages" { dotnet restore }
Run "Build"            { dotnet build --no-restore }
Run "Unit tests"       { dotnet test --no-build }

# 4) Database (SQL Server LocalDB by default - see appsettings.json)
dotnet tool update --global dotnet-ef | Out-Null
if ($LASTEXITCODE -ne 0) { dotnet tool install --global dotnet-ef }

if (-not (Test-Path "src/ServiceDesk.Infrastructure/Persistence/Migrations")) {
    Run "Create the first migration" {
        dotnet ef migrations add InitialCreate `
            --project src/ServiceDesk.Infrastructure `
            --startup-project src/ServiceDesk.API `
            --output-dir Persistence/Migrations
    }
}

Run "Create / update the database" {
    dotnet ef database update `
        --project src/ServiceDesk.Infrastructure `
        --startup-project src/ServiceDesk.API
}

Write-Host "`nAll done. Start the API with:" -ForegroundColor Green
Write-Host "  dotnet run --project src/ServiceDesk.API --launch-profile ServiceDesk.API"
Write-Host "Swagger: https://localhost:7100/swagger"
