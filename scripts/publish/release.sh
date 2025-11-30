#!/bin/bash

# Get the latest testing tag from the repository
git fetch --tags
latestTestingTag=$(git tag -l "testing_*" | sort -V | tail -n 1)

if [ -z "$latestTestingTag" ]; then
    echo "Error: No testing tags found. Please create and test a testing release before publishing a production release."
    exit 1
fi

echo "Latest testing tag: $latestTestingTag"

# Extract version from testing tag (remove 'testing_' prefix)
newTag="${latestTestingTag#testing_}"

echo "New release tag: $newTag"
echo "Using version from testing tag: $latestTestingTag"

# Get the repository root (parent of scripts folder)
scriptDir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repoRoot="$(dirname "$(dirname "$scriptDir}")"

# Update version in MarketBoardPlugin.csproj
echo "Updating MarketBoardPlugin.csproj..."
csprojPath="$repoRoot/MarketBoardPlugin/MarketBoardPlugin.csproj"
sed -i "s/<FileVersion>[0-9.]*<\/FileVersion>/<FileVersion>$newTag<\/FileVersion>/" "$csprojPath"
sed -i "s/<AssemblyVersion>[0-9.]*<\/AssemblyVersion>/<AssemblyVersion>$newTag<\/AssemblyVersion>/" "$csprojPath"

# Commit the version changes
echo "Committing version changes..."
git add "$csprojPath"
git commit -m "[CI] Update release version to $newTag"

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

echo "Successfully created and pushed tag: $newTag"
