using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureVideoStreaming.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJustificacionToPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Justificacion",
                table: "Permisos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Justificacion",
                table: "Permisos");
        }
    }
}
