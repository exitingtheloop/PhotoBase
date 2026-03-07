using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoBase.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class RelaxHashUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImageAssets_HashSha256",
                table: "ImageAssets");

            migrationBuilder.CreateIndex(
                name: "IX_ImageAssets_HashSha256",
                table: "ImageAssets",
                column: "HashSha256");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImageAssets_HashSha256",
                table: "ImageAssets");

            migrationBuilder.CreateIndex(
                name: "IX_ImageAssets_HashSha256",
                table: "ImageAssets",
                column: "HashSha256",
                unique: true);
        }
    }
}
