#!/bin/bash

# Get the latest tag from the repository (excluding testing tags)
git fetch --tags
latestTag=$(git tag -l | grep -v '^testing_' | sort -V | tail -n 1)

if [ -z "$latestTag" ]; then
    echo "No existing tags found. Creating initial tag 1.0.0.0"
    newTag="1.0.0.0"
else
    echo "Latest tag: $latestTag"
    
    # Split the tag by periods
    IFS='.' read -ra parts <<< "$latestTag"
    
    # Increment the last portion
    lastIndex=$((${#parts[@]} - 1))
    parts[$lastIndex]=$((${parts[$lastIndex]} + 1))
    
    # Join back together
    newTag=$(IFS='.'; echo "${parts[*]}")
fi

echo "New tag: $newTag"

# Check if equivalent testing tag exists
testingTag="testing_$newTag"
if ! git tag -l "$testingTag" | grep -q .; then
    echo "Error: Testing tag '$testingTag' does not exist. Please create and test a testing release before publishing a production release."
    exit 1
fi
echo "Found testing tag: $testingTag"

# Get the repository root (parent of scripts folder)
scriptDir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repoRoot="$(dirname "$(dirname "$scriptDir}")"

# Update version in MarketBoardPlugin.csproj
echo "Updating MarketBoardPlugin.csproj..."
csprojPath="$repoRoot/MarketBoardPlugin/MarketBoardPlugin.csproj"
sed -i "s/<FileVersion>[0-9.]*<\/FileVersion>/<FileVersion>$newTag<\/FileVersion>/" "$csprojPath"
sed -i "s/<AssemblyVersion>[0-9.]*<\/AssemblyVersion>/<AssemblyVersion>$newTag<\/AssemblyVersion>/" "$csprojPath"

# Update version in MarketBoardPlugin.json
echo "Updating MarketBoardPlugin.json..."
pluginJsonPath="$repoRoot/MarketBoardPlugin/MarketBoardPlugin.json"
jq --arg version "$newTag" '.AssemblyVersion = $version' "$pluginJsonPath" > tmp.$$.json && mv tmp.$$.json "$pluginJsonPath"

# Commit the version changes
echo "Committing version changes..."
git add "$csprojPath" "$pluginJsonPath"
git commit -m "Bump version to $newTag"

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

echo "Successfully created and pushed tag: $newTag"
