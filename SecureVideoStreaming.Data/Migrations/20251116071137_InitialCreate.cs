using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureVideoStreaming.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreUsuario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TipoUsuario = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(64)", maxLength: 64, nullable: false),
                    Salt = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    ClavePublicaRSA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                });

            migrationBuilder.CreateTable(
                name: "ClavesUsuarios",
                columns: table => new
                {
                    IdClaveUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    ClaveHMAC = table.Column<byte[]>(type: "varbinary(64)", maxLength: 64, nullable: true),
                    FingerprintClavePublica = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClavesUsuarios", x => x.IdClaveUsuario);
                    table.ForeignKey(
                        name: "FK_ClavesUsuarios_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Videos",
                columns: table => new
                {
                    IdVideo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdAdministrador = table.Column<int>(type: "int", nullable: false),
                    TituloVideo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreArchivoOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NombreArchivoCifrado = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TamañoArchivo = table.Column<long>(type: "bigint", nullable: false),
                    Duracion = table.Column<int>(type: "int", nullable: true),
                    FormatoVideo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RutaAlmacenamiento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EstadoProcesamiento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Procesando"),
                    FechaSubida = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.IdVideo);
                    table.ForeignKey(
                        name: "FK_Videos_Usuarios_IdAdministrador",
                        column: x => x.IdAdministrador,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatosCriptograficosVideos",
                columns: table => new
                {
                    IdDatoCripto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdVideo = table.Column<int>(type: "int", nullable: false),
                    KEKCifrada = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    AlgoritmoKEK = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "ChaCha20-Poly1305"),
                    Nonce = table.Column<byte[]>(type: "varbinary(12)", maxLength: 12, nullable: false),
                    AuthTag = table.Column<byte[]>(type: "varbinary(16)", maxLength: 16, nullable: false),
                    AAD = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    HashSHA256Original = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    HMACDelVideo = table.Column<byte[]>(type: "varbinary(64)", maxLength: 64, nullable: false),
                    FechaGeneracionClaves = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    VersionAlgoritmo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "1.0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatosCriptograficosVideos", x => x.IdDatoCripto);
                    table.ForeignKey(
                        name: "FK_DatosCriptograficosVideos_Videos_IdVideo",
                        column: x => x.IdVideo,
                        principalTable: "Videos",
                        principalColumn: "IdVideo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    IdPermiso = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdVideo = table.Column<int>(type: "int", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    TipoPermiso = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Lectura"),
                    FechaOtorgamiento = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaRevocacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NumeroAccesos = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    UltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OtorgadoPor = table.Column<int>(type: "int", nullable: false),
                    RevocadoPor = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.IdPermiso);
                    table.ForeignKey(
                        name: "FK_Permisos_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario");
                    table.ForeignKey(
                        name: "FK_Permisos_Usuarios_OtorgadoPor",
                        column: x => x.OtorgadoPor,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario");
                    table.ForeignKey(
                        name: "FK_Permisos_Usuarios_RevocadoPor",
                        column: x => x.RevocadoPor,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario");
                    table.ForeignKey(
                        name: "FK_Permisos_Videos_IdVideo",
                        column: x => x.IdVideo,
                        principalTable: "Videos",
                        principalColumn: "IdVideo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistroAccesos",
                columns: table => new
                {
                    IdRegistro = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    IdVideo = table.Column<int>(type: "int", nullable: false),
                    TipoAcceso = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Exitoso = table.Column<bool>(type: "bit", nullable: false),
                    MensajeError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DireccionIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaHoraAcceso = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DuracionAcceso = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistroAccesos", x => x.IdRegistro);
                    table.ForeignKey(
                        name: "FK_RegistroAccesos_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario");
                    table.ForeignKey(
                        name: "FK_RegistroAccesos_Videos_IdVideo",
                        column: x => x.IdVideo,
                        principalTable: "Videos",
                        principalColumn: "IdVideo");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClavesUsuarios_IdUsuario",
                table: "ClavesUsuarios",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_DatosCriptograficosVideos_IdVideo",
                table: "DatosCriptograficosVideos",
                column: "IdVideo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_FechaExpiracion",
                table: "Permisos",
                column: "FechaExpiracion");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_IdUsuario",
                table: "Permisos",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_IdVideo",
                table: "Permisos",
                column: "IdVideo");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_IdVideo_IdUsuario",
                table: "Permisos",
                columns: new[] { "IdVideo", "IdUsuario" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_OtorgadoPor",
                table: "Permisos",
                column: "OtorgadoPor");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_RevocadoPor",
                table: "Permisos",
                column: "RevocadoPor");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_TipoPermiso",
                table: "Permisos",
                column: "TipoPermiso");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroAccesos_FechaHoraAcceso",
                table: "RegistroAccesos",
                column: "FechaHoraAcceso");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroAccesos_IdUsuario",
                table: "RegistroAccesos",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroAccesos_IdVideo",
                table: "RegistroAccesos",
                column: "IdVideo");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroAccesos_TipoAcceso",
                table: "RegistroAccesos",
                column: "TipoAcceso");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NombreUsuario",
                table: "Usuarios",
                column: "NombreUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TipoUsuario",
                table: "Usuarios",
                column: "TipoUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_EstadoProcesamiento",
                table: "Videos",
                column: "EstadoProcesamiento");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_FechaSubida",
                table: "Videos",
                column: "FechaSubida");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_IdAdministrador",
                table: "Videos",
                column: "IdAdministrador");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_NombreArchivoCifrado",
                table: "Videos",
                column: "NombreArchivoCifrado",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClavesUsuarios");

            migrationBuilder.DropTable(
                name: "DatosCriptograficosVideos");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.DropTable(
                name: "RegistroAccesos");

            migrationBuilder.DropTable(
                name: "Videos");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
