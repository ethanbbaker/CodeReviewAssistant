using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeReviewAssistant.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RepositoryOwner = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RepositoryName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PullRequestNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TotalFindings = table.Column<int>(type: "INTEGER", nullable: false),
                    CriticalFindings = table.Column<int>(type: "INTEGER", nullable: false),
                    FindingsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    ReviewDuration = table.Column<long>(type: "INTEGER", nullable: false),
                    TokensUsed = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CreatedAt",
                table: "Reviews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_Repository",
                table: "Reviews",
                columns: new[] { "RepositoryOwner", "RepositoryName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reviews");
        }
    }
}
