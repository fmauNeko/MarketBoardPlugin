#!/bin/bash

# Get the latest testing tag from the repository
git fetch --tags

# Check if local branch is up to date with remote
currentBranch=$(git rev-parse --abbrev-ref HEAD)
localCommit=$(git rev-parse @)
remoteCommit=$(git rev-parse "@{u}")

if [ "$localCommit" != "$remoteCommit" ]; then
    echo "Error: Local branch '$currentBranch' is not up to date with remote. Please pull the latest changes before publishing."
    exit 1
fi

echo "Local branch is up to date with remote."

latestTag=$(git tag -l "testing_*" | sort -V | tail -n 1)

if [ -z "$latestTag" ]; then
    echo "No existing testing tags found. Creating initial tag testing_1.0.0.0"
    newTag="testing_1.0.0.0"
    version="1.0.0.0"
else
    echo "Latest testing tag: $latestTag"
    
    # Remove the "testing_" prefix to get the version
    version="${latestTag#testing_}"
    
    # Split the version by periods
    IFS='.' read -ra parts <<< "$version"
    
    # Increment the last portion
    lastIndex=$((${#parts[@]} - 1))
    parts[$lastIndex]=$((${parts[$lastIndex]} + 1))
    
    # Join back together
    version=$(IFS='.'; echo "${parts[*]}")
    newTag="testing_$version"
fi

echo "New testing tag: $newTag"
echo "Version: $version"

# Get the repository root (parent of scripts folder)
scriptDir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repoRoot="$(dirname "$(dirname "$scriptDir}")"

# Update version in MarketBoardPlugin.csproj
echo "Updating MarketBoardPlugin.csproj..."
csprojPath="$repoRoot/MarketBoardPlugin/MarketBoardPlugin.csproj"
sed -i "s/<FileVersion>[0-9.]*<\/FileVersion>/<FileVersion>$version<\/FileVersion>/" "$csprojPath"
sed -i "s/<AssemblyVersion>[0-9.]*<\/AssemblyVersion>/<AssemblyVersion>$version<\/AssemblyVersion>/" "$csprojPath"

# Update version in MarketTerror.json
echo "Updating MarketTerror.json..."
pluginJsonPath="$repoRoot/MarketBoardPlugin/MarketTerror.json"
jq --arg version "$version" '.AssemblyVersion = $version' "$pluginJsonPath" > tmp.$$.json && mv tmp.$$.json "$pluginJsonPath"

# Update LastUpdate in repo.json
echo "Updating repo.json..."
repoJsonPath="$repoRoot/repo.json"
timestamp=$(date +%s)
# Ensure repo.json is an array
if [ "$(jq -r 'type' "$repoJsonPath")" = "object" ]; then
    jq -s '.' "$repoJsonPath" > tmp.$$.json && mv tmp.$$.json "$repoJsonPath"
fi
jq --argjson timestamp "$timestamp" '.[0].LastUpdate = $timestamp | .' "$repoJsonPath" > tmp.$$.json && mv tmp.$$.json "$repoJsonPath"

# Commit the version changes
echo "Committing version changes..."
git add "$csprojPath" "$pluginJsonPath" "$repoJsonPath"
git commit -m "[CI] Update testing version to $version"

# Push the commit first
echo "Pushing version changes to develop..."
git push origin develop

# Verify the commit is on remote with retry logic
echo "Verifying commit on remote..."
maxAttempts=90  # 3 minutes at 2 seconds per attempt
attempt=0
verified=false

while [ $attempt -lt $maxAttempts ]; do
    git fetch origin develop
    localCommit=$(git rev-parse HEAD)
    remoteCommit=$(git rev-parse origin/develop)
    
    if [ "$localCommit" = "$remoteCommit" ]; then
        verified=true
        break
    fi
    
    attempt=$((attempt + 1))
    echo "Waiting for commit to sync... (Attempt $attempt/$maxAttempts)"
    sleep 2
done

if [ "$verified" = false ]; then
    echo "Error: Failed to verify commit on remote after 3 minutes. Local and remote are out of sync."
    exit 1
fi

echo "Commit verified on remote. Creating and pushing tag..."
git tag "$newTag"
git push origin "$newTag"

echo "Successfully created and pushed testing tag: $newTag"
