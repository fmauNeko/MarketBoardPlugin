# Get the latest production release tag
git fetch --tags
$latestReleaseTag = git tag -l | Where-Object { $_ -notmatch '^testing_' } | ForEach-Object {
    [PSCustomObject]@{
        Tag = $_
        Version = [Version]$_
    }
} | Sort-Object Version -Descending | Select-Object -First 1 -ExpandProperty Tag

# Get the latest testing tag
$latestTestingTag = git tag -l "testing_*" | ForEach-Object {
    $version = $_ -replace '^testing_', ''
    [PSCustomObject]@{
        Tag = $_
        Version = [Version]$version
    }
} | Sort-Object Version -Descending | Select-Object -First 1 -ExpandProperty Tag

Write-Host "`n=== Current Versions ===" -ForegroundColor Cyan

if ($latestReleaseTag) {
    Write-Host "Latest Release Tag:  " -NoNewline -ForegroundColor Yellow
    Write-Host $latestReleaseTag -ForegroundColor White
} else {
    Write-Host "Latest Release Tag:  " -NoNewline -ForegroundColor Yellow
    Write-Host "None found" -ForegroundColor Gray
}

if ($latestTestingTag) {
    $testingVersion = $latestTestingTag -replace '^testing_', ''
    Write-Host "Latest Testing Tag:  " -NoNewline -ForegroundColor Yellow
    Write-Host "$latestTestingTag (version: $testingVersion)" -ForegroundColor White
} else {
    Write-Host "Latest Testing Tag:  " -NoNewline -ForegroundColor Yellow
    Write-Host "None found" -ForegroundColor Gray
}

Write-Host "`n=== Next Versions ===" -ForegroundColor Cyan

# Calculate next release version
if (-not $latestReleaseTag) {
    $nextReleaseVersion = "1.0.0.0"
} else {
    $parts = $latestReleaseTag -split '\.'
    $lastIndex = $parts.Length - 1
    $parts[$lastIndex] = [int]$parts[$lastIndex] + 1
    $nextReleaseVersion = $parts -join '.'
}

Write-Host "Next Release:        " -NoNewline -ForegroundColor Green
Write-Host $nextReleaseVersion -ForegroundColor White

# Check if testing tag exists for next release
$nextReleaseTestingTag = "testing_$nextReleaseVersion"
$testingTagExists = git tag -l $nextReleaseTestingTag

if ($testingTagExists) {
    Write-Host "                     " -NoNewline
    Write-Host "✓ Testing tag exists ($nextReleaseTestingTag)" -ForegroundColor Green
} else {
    Write-Host "                     " -NoNewline
    Write-Host "⚠ Testing tag does NOT exist ($nextReleaseTestingTag)" -ForegroundColor Red
}

# Calculate next testing version
if (-not $latestTestingTag) {
    $nextTestingVersion = "1.0.0.0"
    $nextTestingTag = "testing_1.0.0.0"
} else {
    $version = $latestTestingTag -replace '^testing_', ''
    $parts = $version -split '\.'
    $lastIndex = $parts.Length - 1
    $parts[$lastIndex] = [int]$parts[$lastIndex] + 1
    $nextTestingVersion = $parts -join '.'
    $nextTestingTag = "testing_$nextTestingVersion"
}

Write-Host "Next Testing:        " -NoNewline -ForegroundColor Green
Write-Host "$nextTestingTag (version: $nextTestingVersion)" -ForegroundColor White

Write-Host ""
