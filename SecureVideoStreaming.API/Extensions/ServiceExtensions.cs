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

            return services;
        }
    }
}