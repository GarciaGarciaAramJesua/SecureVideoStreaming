using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureVideoStreaming.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicKeyFingerprintToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicKeyFingerprint",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicKeyFingerprint",
                table: "Usuarios");
        }
    }
}
