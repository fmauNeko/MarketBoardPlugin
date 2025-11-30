# Get the latest testing tag from the repository
git fetch --tags

# Check if local branch is up to date with remote
$currentBranch = git rev-parse --abbrev-ref HEAD
$localCommit = git rev-parse "@"
$remoteCommit = git rev-parse "@{u}"

if ($localCommit -ne $remoteCommit) {
    Write-Error "Local branch '$currentBranch' is not up to date with remote. Please pull the latest changes before publishing."
    exit 1
}

Write-Host "Local branch is up to date with remote."

$latestTag = git tag -l "testing_*" | ForEach-Object {
    $version = $_ -replace '^testing_', ''
    [PSCustomObject]@{
        Tag = $_
        Version = [Version]$version
    }
} | Sort-Object Version -Descending | Select-Object -First 1 -ExpandProperty Tag

if (-not $latestTag) {
    Write-Host "No existing testing tags found. Creating initial tag testing_1.0.0.0"
    $newTag = "testing_1.0.0.0"
    $version = "1.0.0.0"
} else {
    Write-Host "Latest testing tag: $latestTag"
    
    # Remove the "testing_" prefix to get the version
    $version = $latestTag -replace '^testing_', ''
    
    # Split the version by periods
    $parts = $version -split '\.'
    
    # Increment the last portion
    $lastIndex = $parts.Length - 1
    $parts[$lastIndex] = [int]$parts[$lastIndex] + 1
    
    # Join back together
    $version = $parts -join '.'
    $newTag = "testing_$version"
}

Write-Host "New testing tag: $newTag"
Write-Host "Version: $version"

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

# Commit the version changes
Write-Host "Committing version changes..."
git add $csprojPath $pluginJsonPath
git commit -m "[CI] Update testing version to $version"

# Push the commit first
Write-Host "Pushing version changes to develop..."
git push origin develop

# Verify the commit is on remote with retry logic
Write-Host "Verifying commit on remote..."
$maxAttempts = 90  # 3 minutes at 2 seconds per attempt
$attempt = 0
$verified = $false

while ($attempt -lt $maxAttempts) {
    git fetch origin develop
    $localCommit = git rev-parse HEAD
    $remoteCommit = git rev-parse origin/develop
    
    if ($localCommit -eq $remoteCommit) {
        $verified = $true
        break
    }
    
    $attempt++
    Write-Host "Waiting for commit to sync... (Attempt $attempt/$maxAttempts)"
    Start-Sleep -Seconds 2
}

if (-not $verified) {
    Write-Error "Failed to verify commit on remote after 3 minutes. Local and remote are out of sync."
    exit 1
}

Write-Host "Commit verified on remote. Creating and pushing tag..."
git tag $newTag
git push origin $newTag

Write-Host "Successfully created and pushed testing tag: $newTag"
