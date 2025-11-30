# Get the latest tag from the repository
$latestTag = git describe --tags --abbrev=0 2>$null

if (-not $latestTag) {
    Write-Host "No existing tags found. Creating initial tag 1.0.0.0"
    $newTag = "1.0.0.0"
} else {
    Write-Host "Latest tag: $latestTag"
    
    # Split the tag by periods
    $parts = $latestTag -split '\.'
    
    # Increment the last portion
    $lastIndex = $parts.Length - 1
    $parts[$lastIndex] = [int]$parts[$lastIndex] + 1
    
    # Join back together
    $newTag = $parts -join '.'
}

Write-Host "New tag: $newTag"

# Check if equivalent testing tag exists
$testingTag = "testing_$newTag"
$testingTagExists = git tag -l $testingTag
if (-not $testingTagExists) {
    Write-Error "Testing tag '$testingTag' does not exist. Please create and test a testing release before publishing a production release."
    exit 1
}
Write-Host "Found testing tag: $testingTag"

# Get the repository root (parent of scripts folder)
$scriptDir = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $scriptDir

# Update version in MarketBoardPlugin.csproj
Write-Host "Updating MarketBoardPlugin.csproj..."
$csprojPath = Join-Path $repoRoot "MarketBoardPlugin\MarketBoardPlugin.csproj"
$csproj = Get-Content $csprojPath -Raw
$csproj = $csproj -replace '<FileVersion>[\d\.]+</FileVersion>', "<FileVersion>$newTag</FileVersion>"
$csproj = $csproj -replace '<AssemblyVersion>[\d\.]+</AssemblyVersion>', "<AssemblyVersion>$newTag</AssemblyVersion>"
Set-Content -Path $csprojPath -Value $csproj -NoNewline

# Update version in MarketBoardPlugin.json
Write-Host "Updating MarketBoardPlugin.json..."
$pluginJsonPath = Join-Path $repoRoot "MarketBoardPlugin\MarketBoardPlugin.json"
$pluginJson = Get-Content $pluginJsonPath -Raw | ConvertFrom-Json
$pluginJson.AssemblyVersion = $newTag
$pluginJson | ConvertTo-Json -Depth 10 | Set-Content -Path $pluginJsonPath

# Commit the version changes
Write-Host "Committing version changes..."
git add $csprojPath $pluginJsonPath
git commit -m "Bump version to $newTag"

# Push the commit first
Write-Host "Pushing version changes to main..."
git push origin main

# Verify the commit is on remote with retry logic
Write-Host "Verifying commit on remote..."
$maxAttempts = 90  # 3 minutes at 2 seconds per attempt
$attempt = 0
$verified = $false

while ($attempt -lt $maxAttempts) {
    git fetch origin main
    $localCommit = git rev-parse HEAD
    $remoteCommit = git rev-parse origin/main
    
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

Write-Host "Successfully created and pushed tag: $newTag"
