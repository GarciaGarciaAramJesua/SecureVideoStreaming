using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureVideoStreaming.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxAccesosToPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxAccesos",
                table: "Permisos",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxAccesos",
                table: "Permisos");
        }
    }
}
