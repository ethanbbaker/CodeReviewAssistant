# CodeReviewAI

An AI-powered pull request review tool built with .NET 10 Blazor Server. Paste a GitHub PR URL, configure your review options, and get a structured, severity-graded code review from Claude — streamed live to your browser.

## Features

### AI Code Review
- **Streaming review** — Claude analyzes your PR diff and streams findings back in real time, so you see results as they arrive
- **Six review categories** — Security, Performance, Correctness / Bugs, Maintainability, Test Coverage, Style / Nitpicks; enable only the ones you care about
- **Minimum severity filter** — choose a threshold (Critical → Info) and Claude will omit everything below it; useful for large PRs where you only want blocking issues
- **Suggested fixes** — optionally include fenced code-block fixes for each finding
- **Free-text focus** — add custom guidance (e.g. "focus on SQL injection risks in the data access layer") to direct Claude's attention
- **Large PR support** — diffs exceeding the context window are automatically split into chunks and reviewed in sequence; results are concatenated into one report
- **Token & cost tracking** — input and output tokens are counted per review and displayed alongside an estimated cost (blended $2/MTok for Claude Haiku 4.5)

### Diff Viewer
- **PR diff viewer** — fetches any public GitHub pull request and renders the unified diff with syntax highlighting for additions, deletions, and context lines
- **Multi-tab sidebar** — each PR opens as its own tab; switch between open PRs or return to the search form without losing your place
- **File jump navigation** — sidebar file list with colour-coded status dots (added / modified / deleted / renamed / binary) and ± line counts; clicking a file scrolls the diff into view, accounting for the sticky header
- **PR metadata panel** — sticky header shows PR title, head → base branch, and change stats; always visible while scrolling through large diffs
- **Truncation warnings** — files with very large diffs are truncated and flagged with a visual indicator

### Review History
- **Persistent history** — every completed (or failed) review is saved to a local SQLite database; history survives app restarts
- **Dashboard** (`/history`) — at-a-glance stats: total reviews, total findings, critical finding count, cumulative tokens used, and estimated spend
- **Severity breakdown** — per-severity finding counts across all reviews
- **Recent reviews table** — last 50 reviews with status badge, PR reference, finding counts, token usage, duration, and timestamp
- **Expandable findings** — click any row to see the full structured finding list (severity, file path, description) inline

### UI & UX
- **Live progress indicator** — spinning gear icon and "live" badge while a review is running
- **Keyboard shortcuts** — press `Enter` on the search field to analyze a PR; press `Esc` to cancel a running review
- **Toast notifications** — non-blocking error toasts appear top-right for API failures, rate limits, and other errors
- **Skeleton loading** — shimmer placeholders replace spinners while the PR is being fetched and while history is loading
- **Onboarding hints** — empty state cards describe what the tool does when no PR is loaded
- **Scroll-to-top button** — floating button appears while scrolling a long diff
- **Cancellable reviews** — cancel a running review at any time; cancelled reviews are not saved to history
- **Dark theme** — purple-accented dark UI throughout

## Architecture

```
CodeReviewAssistant.slnx
├── src/
│   ├── CodeReviewAssistant.Web          # Blazor Server (.NET 10) — pages, components, services
│   ├── CodeReviewAssistant.Core         # Domain models & interfaces (no external dependencies)
│   └── CodeReviewAssistant.Infrastructure  # GitHub (Octokit), Anthropic SDK, EF Core + SQLite
└── tests/
    └── CodeReviewAssistant.Tests        # xUnit unit tests
```

**Key packages:**
- [Anthropic SDK](https://github.com/anthropics/anthropic-sdk-dotnet) 12.9.0 — streaming Messages API
- [Octokit](https://github.com/octokit/octokit.net) 14.0.0 — GitHub REST API client
- [Entity Framework Core](https://github.com/dotnet/efcore) 9.0 + SQLite — review history persistence
- [Polly](https://github.com/App-vNext/Polly) 8.6.6 — retry / exponential back-off for transient API errors
- [Markdig](https://github.com/xoofx/markdig) 0.40.0 — Markdown rendering for review output
- Bootstrap 5 (dark theme)

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 10.0+ |
| Anthropic API key | required — powers the AI review engine |
| GitHub Personal Access Token | optional — raises rate limits to 5,000 req/hr and enables private repo access |

## Running the app

```bash
# 1. Clone the repository
git clone https://github.com/<owner>/CodeReviewAssistant.git
cd CodeReviewAssistant

# 2. Add your Anthropic API key (required)
cd src/CodeReviewAssistant.Web
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..."

# 3. (Optional) Add a GitHub Personal Access Token
#    Required scope: repo:read
#    Without it the GitHub API is limited to 60 requests/hr and public repos only.
dotnet user-secrets set "GitHub:PersonalAccessToken" "ghp_..."

# 4. Run the web app
dotnet run
```

Then open **https://localhost:7067** (or http://localhost:5068) in your browser.

**Live demo:** https://aireviewtool-cchfgjfaahchgtb7.westus2-01.azurewebsites.net/

The SQLite history database is created automatically at `%AppData%\CodeReviewAssistant\history.db` on first run.

## Running tests

```bash
dotnet test
```

## Usage

1. Paste a GitHub pull request URL into the search field (e.g. `https://github.com/owner/repo/pull/123`) and press **Enter** or click **Analyze PR**.
2. The diff loads and a new tab appears in the sidebar for that PR.
3. Configure the review using the **Review** toolbar:
   - Select which **categories** to check (Security, Performance, Correctness, Maintainability, Test Coverage, Style)
   - Set the **minimum severity** to report (Critical / High / Medium / Low / Info)
   - Toggle **suggested fixes** on or off
   - Optionally add a **focus area** to direct Claude's attention
4. Click **Start Review** (or press `Esc` to cancel a review in progress).
5. Findings stream in live; the review panel auto-scrolls as content arrives.
6. Use the **file list** in the sidebar to jump between files in the diff.
7. Open more PRs from the **Search** tab — each gets its own sidebar tab.
8. Visit the **Review History** link in the sidebar to see past reviews and aggregated stats.

## Configuration

| Key | Description | Default |
|---|---|---|
| `Anthropic:ApiKey` | Anthropic API key | *(none — required)* |
| `GitHub:PersonalAccessToken` | GitHub PAT for higher rate limits and private repos | *(none — public API, 60 req/hr)* |

Set via `dotnet user-secrets` (development) or environment variable / `appsettings.json` (production).
