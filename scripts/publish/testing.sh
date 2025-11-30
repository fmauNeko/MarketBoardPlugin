#!/bin/bash

# Get the latest testing tag from the repository
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

# Update version in MarketBoardPlugin.json
echo "Updating MarketBoardPlugin.json..."
pluginJsonPath="$repoRoot/MarketBoardPlugin/MarketBoardPlugin.json"
jq --arg version "$version" '.AssemblyVersion = $version' "$pluginJsonPath" > tmp.$$.json && mv tmp.$$.json "$pluginJsonPath"

# Commit the version changes
echo "Committing version changes..."
git add "$csprojPath" "$pluginJsonPath"
git commit -m "Bump testing version to $version"

# Push the commit first
echo "Pushing version changes to main..."
git push origin main

# Verify the commit is on remote with retry logic
echo "Verifying commit on remote..."
maxAttempts=90  # 3 minutes at 2 seconds per attempt
attempt=0
verified=false

while [ $attempt -lt $maxAttempts ]; do
    git fetch origin main
    localCommit=$(git rev-parse HEAD)
    remoteCommit=$(git rev-parse origin/main)
    
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
