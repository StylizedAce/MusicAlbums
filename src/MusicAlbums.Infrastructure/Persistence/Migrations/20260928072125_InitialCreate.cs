using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicAlbums.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedAlbums",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProviderAlbumId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ArtistName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CoverImageUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TrackCount = table.Column<int>(type: "INTEGER", nullable: true),
                    SavedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedAlbums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedAlbums_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedTracks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SavedAlbumId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedTracks_SavedAlbums_SavedAlbumId",
                        column: x => x.SavedAlbumId,
                        principalTable: "SavedAlbums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedAlbums_UserId_ProviderName_ProviderAlbumId",
                table: "SavedAlbums",
                columns: new[] { "UserId", "ProviderName", "ProviderAlbumId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedTracks_SavedAlbumId",
                table: "SavedTracks",
                column: "SavedAlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Name",
                table: "Users",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedTracks");

            migrationBuilder.DropTable(
                name: "SavedAlbums");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
