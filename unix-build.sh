#!/bin/bash

# Builds the executable
# use the flag --ucsf to use module for the ucsf radiology cluster

set -e

if [[ "$1" == "--ucsf" ]]; then
    module load morrisonlab
    module load dotnet-sdk/8.0.401
else

    DOTNET_DIR=$(mktemp -d)
    echo "$DOTNET_DIR"

    trap '[[ -d "$DOTNET_DIR" ]] && rm -rf "$DOTNET_DIR"' EXIT INT TERM
    mkdir -p "$DOTNET_DIR"
    echo "Downloading dotnet sdk"
    curl -sSL "https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.411/dotnet-sdk-8.0.411-linux-x64.tar.gz" | tar -xz -C "$DOTNET_DIR"
    export DOTNET_ROOT="$DOTNET_DIR"
    export PATH="$DOTNET_DIR:$PATH"
fi

echo "Building Directory Created Watcher"
dotnet publish DirectoryCreatedWatcher/DirectoryCreatedWatcher.csproj -r linux-x64
echo "Built Directory Created Watcher"