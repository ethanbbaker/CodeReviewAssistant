# CodeReviewAI

An AI-powered pull request review tool built with .NET 10 Blazor Server. Paste a GitHub PR URL and get an instant, syntax-highlighted diff view — with multi-tab support so you can keep several PRs open at once.

## Features

- **PR diff viewer** — fetches any public GitHub pull request and renders the unified diff with syntax highlighting for additions, deletions, and context lines
- **Multi-tab sidebar** — each fetched PR opens as its own tab; switch between open PRs or return to the search form without losing your place
- **File jump navigation** — a sticky "N files changed" dropdown inside the diff view lets you jump directly to any file in the PR, similar to GitHub's own file navigator
- **File list sidebar** — shows PR metadata, branch info, and a full file list with colour-coded status dots (added / modified / deleted / renamed / binary) and ± line counts
- **Error handling** — clear messages for rate limits, private repos, bad URLs, and network errors
- **Dark UI** — purple-accented dark theme throughout

## Architecture

```
CodeReviewAssistant.sln
├── src/
│   ├── CodeReviewAssistant.Web          # Blazor Server (.NET 10)
│   ├── CodeReviewAssistant.Core         # Domain models & interfaces
│   └── CodeReviewAssistant.Infrastructure  # GitHub (Octokit) & diff parsing
└── tests/
    └── CodeReviewAssistant.Tests        # xUnit unit tests
```

**Key packages:** Octokit (GitHub API client), Bootstrap 5 (dark theme).

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 10.0+ |
| GitHub account | optional (needed for private repos or higher rate limits) |

## Running the app

```bash
# 1. Clone the repository
git clone https://github.com/<owner>/CodeReviewAssistant.git
cd CodeReviewAssistant

# 2. (Optional) Add a GitHub Personal Access Token to increase API rate limits
#    and enable access to private repositories.
#    The token needs the repo:read scope.
cd src/CodeReviewAssistant.Web
dotnet user-secrets set "GitHub:PersonalAccessToken" "ghp_your_token_here"
cd ../..

# 3. Run the web app
cd src/CodeReviewAssistant.Web
dotnet run
```

Then open **https://localhost:7067** (or http://localhost:5068) in your browser.

## Running tests

```bash
dotnet test
```

## Usage

1. Paste a GitHub pull request URL into the search field, e.g.:
   `https://github.com/owner/repo/pull/123`
2. Click **Analyze PR**.
3. The diff loads and a new tab appears in the sidebar for that PR.
4. Use the **file list** in the sidebar or the sticky **N files changed** dropdown to jump between files.
5. Open more PRs from the **Search** tab — each one gets its own sidebar tab.
6. Close a tab with the **×** button next to its name.

## Configuration

| Key | Description | Default |
|---|---|---|
| `GitHub:PersonalAccessToken` | GitHub PAT for higher rate limits / private repos | *(none — public API, 60 req/hr)* |

Set via `dotnet user-secrets` (development) or environment variable / app settings (production).
