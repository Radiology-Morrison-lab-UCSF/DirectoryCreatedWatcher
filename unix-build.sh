set -e

module load morrisonlab
module load dotnet-sdk/8.0.400

dotnet build DirectoryCreatedWatcher/DirectoryCreatedWatcher.csproj -c Release --self-contained