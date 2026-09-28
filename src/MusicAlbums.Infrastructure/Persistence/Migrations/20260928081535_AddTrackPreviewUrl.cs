using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicAlbums.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackPreviewUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreviewUrl",
                table: "SavedTracks",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviewUrl",
                table: "SavedTracks");
        }
    }
}
