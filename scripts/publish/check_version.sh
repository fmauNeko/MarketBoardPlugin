#!/bin/bash

# Get the latest production release tag
git fetch --tags
latestReleaseTag=$(git tag -l | grep -v '^testing_' | sort -V | tail -n 1)

# Get the latest testing tag
latestTestingTag=$(git tag -l "testing_*" | sort -V | tail -n 1)

echo ""
echo -e "\033[36m=== Current Versions ===\033[0m"

if [ -n "$latestReleaseTag" ]; then
    echo -e "\033[33mLatest Release Tag:  \033[0m$latestReleaseTag"
else
    echo -e "\033[33mLatest Release Tag:  \033[90mNone found\033[0m"
fi

if [ -n "$latestTestingTag" ]; then
    testingVersion=${latestTestingTag#testing_}
    echo -e "\033[33mLatest Testing Tag:  \033[0m$latestTestingTag (version: $testingVersion)"
else
    echo -e "\033[33mLatest Testing Tag:  \033[90mNone found\033[0m"
fi

echo ""
echo -e "\033[36m=== Next Versions ===\033[0m"

# Calculate next release version
if [ -z "$latestReleaseTag" ]; then
    nextReleaseVersion="1.0.0.0"
else
    IFS='.' read -r -a parts <<< "$latestReleaseTag"
    lastIndex=$((${#parts[@]} - 1))
    parts[$lastIndex]=$((${parts[$lastIndex]} + 1))
    nextReleaseVersion=$(IFS='.'; echo "${parts[*]}")
fi

echo -e "\033[32mNext Release:        \033[0m$nextReleaseVersion"

# Check if testing tag exists for next release
nextReleaseTestingTag="testing_$nextReleaseVersion"
if git tag -l "$nextReleaseTestingTag" | grep -q .; then
    echo -e "                     \033[32m✓ Testing tag exists ($nextReleaseTestingTag)\033[0m"
else
    echo -e "                     \033[31m⚠ Testing tag does NOT exist ($nextReleaseTestingTag)\033[0m"
fi

# Calculate next testing version
if [ -z "$latestTestingTag" ]; then
    nextTestingVersion="1.0.0.0"
    nextTestingTag="testing_1.0.0.0"
else
    version=${latestTestingTag#testing_}
    IFS='.' read -r -a parts <<< "$version"
    lastIndex=$((${#parts[@]} - 1))
    parts[$lastIndex]=$((${parts[$lastIndex]} + 1))
    nextTestingVersion=$(IFS='.'; echo "${parts[*]}")
    nextTestingTag="testing_$nextTestingVersion"
fi

echo -e "\033[32mNext Testing:        \033[0m$nextTestingTag (version: $nextTestingVersion)"

echo ""
