# Steam Community Market Manager (SCMM)
SCMM is a fan-created project dedicated to collecting and analyzing data from the Steam Community Market and third-party marketplaces. Our goal is to document the history of the Steam market (and game item stores), detect market trends and patterns, and provide useful quality of life features not found in the official Steam Community Market app/website. Currently on a small subset of Steam apps are supported by SCMM, with the primary focus being [Rust](https://store.steampowered.com/app/252490/Rust/).

| | Website | Status |
|----|----|----|
|**Rust**|https://rust.scmm.app/store|✅ Fully supported.|
|**Unturned**|https://unturned.scmm.app/items|⚠ Work in progress. Basic historical data is available, but all data update jobs are disabled.|
|**CSGO**|https://csgo.scmm.app/items|⚠ Work in progress. Basic historical data is available, but all data update jobs are disabled.|

# Project architecture
SCMM started as a personal project to gain practical and hands-on experience using [Azure](https://azure.microsoft.com/en-us) and [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor). Because of this, a lot of decisions within the project may seem strange, hacky, or overengineered. Sometimes things were done they way they are just as an excuse to try out a specific technology or feature and not because it was the most sensible or pragmatic option.

- TODO: List key technologies and frameworks
- TODO: High-level component diagram
- TODO: Infrastructure architecture diagram

# How to contribute

## Preparing your development environment
TODO: Document this...

You will need:
- [Visual Studio](https://visualstudio.microsoft.com/vs/community/) (2022/v17.7+) with the following workloads and components installed:
  - ASP.NET and web development
    - .NET SDK
    - .NET 7.0 Runtime
    - .NET 7.0 WebAssembly Build Tools
  - Azure development
    - Azure Compute Emulator 
    - [Azure Storage Emulator](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-emulator#get-the-storage-emulator)
    - [Azure Data Studio](https://learn.microsoft.com/en-us/sql/azure-data-studio/download-azure-data-studio?view=sql-server-ver16&tabs=redhat-install%2Credhat-uninstall#download-azure-data-studio)
  - Data storage and processing
    - SQL Server Express LocalDB 

## Compling the project
TODO: Document this...

## Debugging the project
TODO: Document this...

## Submitting a pull request
TODO: Document this...

## Publishing new versions
TODO: Document this...
