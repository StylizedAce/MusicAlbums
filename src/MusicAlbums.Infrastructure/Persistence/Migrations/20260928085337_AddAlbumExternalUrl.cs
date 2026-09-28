using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicAlbums.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAlbumExternalUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalUrl",
                table: "SavedAlbums",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalUrl",
                table: "SavedAlbums");
        }
    }
}
