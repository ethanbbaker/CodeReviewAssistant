using Anthropic;
using CodeReviewAssistant.Components;
using CodeReviewAssistant.Components.Services;
using CodeReviewAssistant.Core.Interfaces;
using CodeReviewAssistant.Infrastructure.Anthropic;
using CodeReviewAssistant.Infrastructure.Data;
using CodeReviewAssistant.Infrastructure.GitHub;
using Microsoft.EntityFrameworkCore;
using Octokit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// GitHub client — reads optional PAT from configuration
builder.Services.AddSingleton<IGitHubClient>(_ =>
{
    var token  = builder.Configuration["GitHub:PersonalAccessToken"];
    var client = new GitHubClient(new ProductHeaderValue("CodeReviewAssistant"));

    if (!string.IsNullOrWhiteSpace(token))
        client.Credentials = new Credentials(token);

    return client;
});

builder.Services.AddScoped<IGitHubService, GitHubService>();
builder.Services.AddScoped<IReviewStateService, ReviewStateService>();
builder.Services.AddScoped<IReviewCacheService, ReviewCacheService>();
builder.Services.AddScoped<ToastService>();

// Anthropic client — reads API key from configuration / user secrets
builder.Services.AddSingleton<AnthropicClient>(_ =>
{
    var apiKey = builder.Configuration["Anthropic:ApiKey"];
    return new AnthropicClient(new Anthropic.Core.ClientOptions
    {
        ApiKey = string.IsNullOrWhiteSpace(apiKey) ? "MISSING_KEY" : apiKey,
    });
});

builder.Services.AddScoped<ICodeReviewService, AnthropicCodeReviewService>();

// ── Review history (EF Core + SQLite) ────────────────────────────────────────
var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
var dbDir   = Path.Combine(appData, "CodeReviewAssistant");
Directory.CreateDirectory(dbDir);
var dbPath  = Path.Combine(dbDir, "history.db");

builder.Services.AddDbContext<ReviewDbContext>(o =>
    o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddScoped<IReviewHistoryRepository, ReviewHistoryRepository>();

var app = builder.Build();

// Apply any pending EF Core migrations on startup (creates the DB on first run).
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider
         .GetRequiredService<ReviewDbContext>()
         .Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
