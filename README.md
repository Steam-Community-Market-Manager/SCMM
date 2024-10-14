# Steam Community Market Manager (SCMM)
SCMM is a fan-created project dedicated to collecting and analyzing data from the Steam Community Market and third-party marketplaces. Our goal is to document the history of the Steam market (and game item stores), detect market trends and patterns, and provide useful quality of life features not found in the official Steam Community Market app/website. Currently only a small subset of Steam apps are supported by SCMM, primarily [Rust](https://store.steampowered.com/app/252490/Rust/).

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
### 1. Fork the Repository
To make changes, first, fork the SCMM repository to your GitHub account.

1. Navigate to the [SCMM repository](https://github.com/Steam-Community-Market-Manager/SCMM).
2. In the top-right corner, click the **Fork** button.
3. Once forked, you'll have your own copy of the SCMM repository under your GitHub account.

### 2. Clone the Forked Repository
Clone the forked repository to your local machine using Git.

  1. Open a terminal on your computer.
  2. Run the following command to clone the repository (replace `<your-username>` with your GitHub username):
```bash
git clone https://github.com/<your-username>/SCMM.git
```
  4. Navigate into the cloned repository:
```bash
cd SCMM
```
### 3. Create a New Branch
Before making any changes, create a new branch to work on. Use a descriptive name for your branch that reflects the feature or bug you're working on.
```bash
git checkout -b <branch-name>
```
For Example:
```bash
git checkout -b feature/add-new-market-filter
```
### 4. Make Your Changes
Now that you're on your new branch, make the necessary changes to the codebase. You can use your preferred code editor (e.g., VS Code, Sublime Text) to make these changes.

### 5. Commit Your Changes
After you've made your changes, commit them to your branch.
  1. Stage the changes:
```bash
git add .
```
  2. Commit with a descriptive message:
```bash
git commit -m "Add new market filter functionality"
```

### 6. Push the Changes to Your Fork
Once your changes are committed, push them to your forked repository on GitHub.
```bash
git push origin <branch-name>
```
For example:
```bash
git push origin feature/add-new-market-filter
```
### 7. Create a Pull Request
Now that your branch is pushed to your forked repository, you can create a pull request (PR) to propose your changes.

1. Navigate to the SCMM repository.
2. Click the **Pull requests** tab.
3. Click the **New pull request** button.
4. Set the base repository to `Steam-Community-Market-Manager/SCMM` and the base branch to `main`.
5. Set the compare branch to your feature branch (e.g., `feature/add-new-market-filter`) from your forked repository.
6. Add a title and description to explain what your pull request does. Be as clear and concise as possible. Include details about:
   - The problem you are solving.
   - The approach you took.
   - Any trade-offs or limitations.
7. If applicable, link to relevant issues using keywords like "Fixes #issue_number" or "Closes #issue_number".
8. Click **Create pull request**.

### 8. Wait for Review
Once your pull request is created, the maintainers of the SCMM repository will review your changes. They may leave comments, suggest changes, or approve your PR. You can make additional commits to your branch if changes are requested.

### 9. Merge the Pull Request
Once your pull request is approved, it will be merged into the main branch by the maintainers.

## Publishing new versions
TODO: Document this...
