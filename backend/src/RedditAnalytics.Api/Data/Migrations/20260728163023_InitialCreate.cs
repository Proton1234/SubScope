using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RedditAnalytics.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Subreddits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SubscriberCount = table.Column<int>(type: "integer", nullable: false),
                    ActiveAccountCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subreddits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubredditSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubredditId = table.Column<int>(type: "integer", nullable: false),
                    SubscriberCount = table.Column<int>(type: "integer", nullable: false),
                    ActiveAccountCount = table.Column<int>(type: "integer", nullable: true),
                    CapturedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubredditSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubredditSnapshots_Subreddits_SubredditId",
                        column: x => x.SubredditId,
                        principalTable: "Subreddits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subreddits_Name",
                table: "Subreddits",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubredditSnapshots_SubredditId_CapturedAtUtc",
                table: "SubredditSnapshots",
                columns: new[] { "SubredditId", "CapturedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubredditSnapshots");

            migrationBuilder.DropTable(
                name: "Subreddits");
        }
    }
}
