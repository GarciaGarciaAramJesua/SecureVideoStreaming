using SecureVideoStreaming.Services.Business.Implementations;
using SecureVideoStreaming.Services.Business.Interfaces;
using SecureVideoStreaming.Services.Cryptography.Implementations;
using SecureVideoStreaming.Services.Cryptography.Interfaces;

namespace SecureVideoStreaming.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddCryptographyServices(this IServiceCollection services)
        {
            // Registrar servicios criptográficos como Singleton (son stateless)
            services.AddSingleton<IChaCha20Poly1305Service, ChaCha20Poly1305Service>();
            services.AddSingleton<IRsaService, RsaService>();
            services.AddSingleton<IHashService, HashService>();
            services.AddSingleton<IHmacService, HmacService>();
            services.AddSingleton<IKmacService, KmacService>();
            
            // Servicios de gestión de claves
            services.AddSingleton<IKeyManagementService, KeyManagementService>();
            services.AddSingleton<IKekService, KekService>();
            
            // Servicio de cifrado de claves privadas del cliente
            services.AddSingleton<IPrivateKeyEncryptionService, PrivateKeyEncryptionService>();
            
            // Servicio de cifrado de videos
            services.AddSingleton<IVideoEncryptionService, VideoEncryptionService>();

            return services;
        }

        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // Registrar servicios de negocio como Scoped (trabajan con DbContext)
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IVideoService, VideoService>();
            
            // Servicios de distribución de claves y permisos
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IKeyDistributionService, KeyDistributionService>();
            services.AddScoped<IVideoStreamingService, VideoStreamingService>();

            return services;
        }
    }
}