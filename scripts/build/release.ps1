# Get the latest tag from the remote repository (excluding testing tags)
git fetch --tags
$latestTag = git tag -l | Where-Object { $_ -notmatch '^testing_' } | Sort-Object -Descending | Select-Object -First 1

if (-not $latestTag) {
    Write-Host "No existing tags found. Using version 1.0.0.0"
    $version = "1.0.0.0"
} else {
    Write-Host "Latest tag: $latestTag"
    $version = $latestTag
}

Write-Host "Building with version: $version"

# Get the repository root (parent of scripts folder)
$scriptDir = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $scriptDir

# Update version in MarketBoardPlugin.csproj
Write-Host "Updating MarketBoardPlugin.csproj..."
$csprojPath = Join-Path $repoRoot "MarketBoardPlugin\MarketBoardPlugin.csproj"
$csproj = Get-Content $csprojPath -Raw
$csproj = $csproj -replace '<FileVersion>[\d\.]+</FileVersion>', "<FileVersion>$version</FileVersion>"
$csproj = $csproj -replace '<AssemblyVersion>[\d\.]+</AssemblyVersion>', "<AssemblyVersion>$version</AssemblyVersion>"
Set-Content -Path $csprojPath -Value $csproj -NoNewline

# Update version in MarketTerror.json
Write-Host "Updating MarketTerror.json..."
$pluginJsonPath = Join-Path $repoRoot "MarketBoardPlugin\MarketTerror.json"
$pluginJson = Get-Content $pluginJsonPath -Raw | ConvertFrom-Json
$pluginJson.AssemblyVersion = $version
$pluginJson | ConvertTo-Json -Depth 10 | Set-Content -Path $pluginJsonPath

# Update version in repo.json
Write-Host "Updating repo.json..."
$repoJsonPath = Join-Path $repoRoot "repo.json"
$repoJsonRaw = Get-Content $repoJsonPath -Raw
$repoJson = $repoJsonRaw | ConvertFrom-Json
# Ensure repoJson is always an array
if ($repoJson -isnot [System.Collections.IEnumerable] -or $repoJson -is [string]) {
    $repoJson = @($repoJson)
}
$repoJson[0].AssemblyVersion = $version
$repoJson[0].TestingAssemblyVersion = $version
$repoJsonJson = $repoJson | ConvertTo-Json -Depth 10
$trimmed = $repoJsonJson.Trim()
$nl = [Environment]::NewLine
if ($trimmed.StartsWith('{')) {
    $repoJsonJson = '[' + $nl + $repoJsonJson + $nl + ']'
}
Set-Content -Path $repoJsonPath -Value $repoJsonJson

# Build the project in Release mode
Write-Host "Building in Release mode..."
$slnPath = Join-Path $repoRoot "MarketBoardPlugin.sln"
dotnet build $slnPath -c Release

# Revert the version changes
Write-Host "Reverting version changes..."
git checkout -- $csprojPath $pluginJsonPath $repoJsonPath

Write-Host "Build complete! Version: $version"
