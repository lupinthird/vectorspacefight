# Rebuild checked-in MonoGame content (Content/Prebuilt/DesktopGL/*.xnb).
# Run after editing Content.mgcb, shaders, or other pipeline assets.
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot\..

dotnet tool restore
dotnet build -t:BuildPrebuiltContent -p:UsePrebuiltContent=false

Write-Host "Prebuilt content updated under Content/Prebuilt/DesktopGL"
