using Microsoft.EntityFrameworkCore;
using SecureVideoStreaming.Models.Entities;

namespace SecureVideoStreaming.Data.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets para las tablas
        public DbSet<User> Usuarios { get; set; }
        public DbSet<UserKeys> ClavesUsuarios { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<CryptoData> DatosCriptograficosVideos { get; set; }
        public DbSet<Permission> Permisos { get; set; }
        public DbSet<AccessLog> RegistroAccesos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de User (Usuarios)
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Usuarios");
                entity.HasKey(e => e.IdUsuario);
                entity.Property(e => e.IdUsuario).ValueGeneratedOnAdd();
                
                entity.Property(e => e.NombreUsuario).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.TipoUsuario).IsRequired().HasMaxLength(20);
                entity.Property(e => e.ClavePublicaRSA).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(64);
                entity.Property(e => e.Salt).IsRequired().HasMaxLength(32);
                entity.Property(e => e.FechaRegistro).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Activo).HasDefaultValue(true);

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.NombreUsuario).IsUnique();
                entity.HasIndex(e => e.TipoUsuario);
            });

            // Configuración de UserKeys (ClavesUsuarios)
            modelBuilder.Entity<UserKeys>(entity =>
            {
                entity.ToTable("ClavesUsuarios");
                entity.HasKey(e => e.IdClaveUsuario);
                entity.Property(e => e.IdClaveUsuario).ValueGeneratedOnAdd();
                
                entity.Property(e => e.ClaveHMAC).HasMaxLength(64);
                entity.Property(e => e.FingerprintClavePublica).IsRequired().HasMaxLength(32);
                entity.Property(e => e.FechaCreacion).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Usuario)
                      .WithMany(u => u.ClavesUsuarios)
                      .HasForeignKey(e => e.IdUsuario)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.IdUsuario);
            });

            // Configuración de Video (Videos)
            modelBuilder.Entity<Video>(entity =>
            {
                entity.ToTable("Videos");
                entity.HasKey(e => e.IdVideo);
                entity.Property(e => e.IdVideo).ValueGeneratedOnAdd();
                
                entity.Property(e => e.TituloVideo).IsRequired().HasMaxLength(255);
                entity.Property(e => e.NombreArchivoOriginal).IsRequired().HasMaxLength(255);
                entity.Property(e => e.NombreArchivoCifrado).IsRequired().HasMaxLength(255);
                entity.Property(e => e.RutaAlmacenamiento).IsRequired().HasMaxLength(500);
                entity.Property(e => e.EstadoProcesamiento).IsRequired().HasMaxLength(50).HasDefaultValue("Procesando");
                entity.Property(e => e.FormatoVideo).HasMaxLength(50);
                entity.Property(e => e.FechaSubida).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Administrador)
                      .WithMany(u => u.VideosAdministrados)
                      .HasForeignKey(e => e.IdAdministrador)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.NombreArchivoCifrado).IsUnique();
                entity.HasIndex(e => e.IdAdministrador);
                entity.HasIndex(e => e.EstadoProcesamiento);
                entity.HasIndex(e => e.FechaSubida);
            });

            // Configuración de CryptoData (DatosCriptograficosVideos)
            modelBuilder.Entity<CryptoData>(entity =>
            {
                entity.ToTable("DatosCriptograficosVideos");
                entity.HasKey(e => e.IdDatoCripto);
                entity.Property(e => e.IdDatoCripto).ValueGeneratedOnAdd();
                
                entity.Property(e => e.KEKCifrada).IsRequired();
                entity.Property(e => e.AlgoritmoKEK).IsRequired().HasMaxLength(50).HasDefaultValue("ChaCha20-Poly1305");
                entity.Property(e => e.Nonce).IsRequired().HasMaxLength(12);
                entity.Property(e => e.AuthTag).IsRequired().HasMaxLength(16);
                entity.Property(e => e.HashSHA256Original).IsRequired().HasMaxLength(32);
                entity.Property(e => e.HMACDelVideo).IsRequired().HasMaxLength(64);
                entity.Property(e => e.FechaGeneracionClaves).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.VersionAlgoritmo).IsRequired().HasMaxLength(20).HasDefaultValue("1.0");

                entity.HasOne(e => e.Video)
                      .WithOne(v => v.DatosCriptograficos)
                      .HasForeignKey<CryptoData>(e => e.IdVideo)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.IdVideo).IsUnique();
            });

            // Configuración de Permission (Permisos)
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.ToTable("Permisos");
                entity.HasKey(e => e.IdPermiso);
                entity.Property(e => e.IdPermiso).ValueGeneratedOnAdd();
                
                entity.Property(e => e.TipoPermiso).IsRequired().HasMaxLength(50).HasDefaultValue("Lectura");
                entity.Property(e => e.FechaOtorgamiento).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.NumeroAccesos).HasDefaultValue(0);

                entity.HasOne(e => e.Video)
                      .WithMany(v => v.Permisos)
                      .HasForeignKey(e => e.IdVideo)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Usuario)
                      .WithMany(u => u.Permisos)
                      .HasForeignKey(e => e.IdUsuario)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.UsuarioOtorgante)
                      .WithMany()
                      .HasForeignKey(e => e.OtorgadoPor)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.UsuarioRevocador)
                      .WithMany()
                      .HasForeignKey(e => e.RevocadoPor)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => new { e.IdVideo, e.IdUsuario }).IsUnique();
                entity.HasIndex(e => e.IdVideo);
                entity.HasIndex(e => e.IdUsuario);
                entity.HasIndex(e => e.TipoPermiso);
                entity.HasIndex(e => e.FechaExpiracion);
            });

            // Configuración de AccessLog (RegistroAccesos)
            modelBuilder.Entity<AccessLog>(entity =>
            {
                entity.ToTable("RegistroAccesos");
                entity.HasKey(e => e.IdRegistro);
                entity.Property(e => e.IdRegistro).ValueGeneratedOnAdd();
                
                entity.Property(e => e.TipoAcceso).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DireccionIP).HasMaxLength(50);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.FechaHoraAcceso).HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Usuario)
                      .WithMany(u => u.RegistrosAccesos)
                      .HasForeignKey(e => e.IdUsuario)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Video)
                      .WithMany(v => v.RegistrosAccesos)
                      .HasForeignKey(e => e.IdVideo)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => e.IdUsuario);
                entity.HasIndex(e => e.IdVideo);
                entity.HasIndex(e => e.FechaHoraAcceso);
                entity.HasIndex(e => e.TipoAcceso);
            });
        }
    }
}