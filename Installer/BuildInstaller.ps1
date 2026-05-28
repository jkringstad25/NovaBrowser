<#
BuildInstaller.ps1

Usage:
  .\BuildInstaller.ps1

This script publishes the NovaBrowser project to Installer\Output and then
compiles an Inno Setup installer if ISCC.exe is installed.
#>

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptDir "..\NovaBrowser.csproj"
$outputDir = Join-Path $scriptDir "Output"
$installerScript = Join-Path $scriptDir "NovaBrowserInstaller.iss"

Write-Host "Publishing NovaBrowser project..."
if (-Not (Test-Path $projectPath)) {
    Write-Error "Project file not found: $projectPath"
    exit 1
}

Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $outputDir
New-Item -ItemType Directory -Path $outputDir | Out-Null

$publishArgs = @(
    'publish',
    $projectPath,
    '-c', 'Release',
    '-r', 'win-x64',
    '--self-contained', 'false',
    '/p:PublishSingleFile=false',
    '/p:PublishTrimmed=false',
    '-o', $outputDir
)

$publishResult = dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed."
    exit $LASTEXITCODE
}

Write-Host "Published output to: $outputDir"

$innoCompiler = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if ($null -eq $innoCompiler) {
    Write-Warning "Inno Setup compiler (ISCC.exe) not found on PATH."
    Write-Host "You can still use the published files in the Output folder or install Inno Setup to build NovaBrowserSetup.exe."
    exit 0
}

Write-Host "Compiling installer using Inno Setup..."
& $innoCompiler.Source $installerScript
if ($LASTEXITCODE -ne 0) {
    Write-Error "Inno Setup compiler failed."
    exit $LASTEXITCODE
}

Write-Host "Installer built successfully. Look for NovaBrowserSetup.exe in: $scriptDir"
