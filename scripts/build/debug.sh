#!/bin/bash

# Get the latest tag from the remote repository (excluding testing tags)
git fetch --tags
latestTag=$(git tag -l | grep -v '^testing_' | sort -V | tail -n 1)

if [ -z "$latestTag" ]; then
    echo "No existing tags found. Using version 1.0.0.0"
    version="1.0.0.0"
else
    echo "Latest tag: $latestTag"
    version="$latestTag"
fi

echo "Building with version: $version"

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

# Update version in repo.json
echo "Updating repo.json..."
repoJsonPath="$repoRoot/repo.json"
jq --arg version "$version" '.[0].AssemblyVersion = $version | .[0].TestingAssemblyVersion = $version' "$repoJsonPath" > tmp.$$.json && mv tmp.$$.json "$repoJsonPath"

# Build the project in Debug mode
echo "Building in Debug mode..."
dotnet build "$repoRoot/MarketBoardPlugin.sln" -c Debug

# Revert the version changes
echo "Reverting version changes..."
git checkout -- "$csprojPath" "$pluginJsonPath" "$repoJsonPath"

echo "Build complete! Version: $version"
