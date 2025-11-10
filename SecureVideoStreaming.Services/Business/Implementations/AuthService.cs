using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SecureVideoStreaming.Data.Context;
using SecureVideoStreaming.Models.DTOs.Request;
using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Models.Entities;
using SecureVideoStreaming.Services.Business.Interfaces;
using SecureVideoStreaming.Services.Cryptography.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SecureVideoStreaming.Services.Business.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHashService _hashService;
        private readonly IRsaService _rsaService;
        private readonly IHmacService _hmacService;
        private readonly IConfiguration _configuration;

        public AuthService(
            ApplicationDbContext context,
            IHashService hashService,
            IRsaService rsaService,
            IHmacService hmacService,
            IConfiguration configuration)
        {
            _context = context;
            _hashService = hashService;
            _rsaService = rsaService;
            _hmacService = hmacService;
            _configuration = configuration;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterUserRequest request)
        {
            // 1. Verificar si el usuario ya existe
            var existingUser = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email || u.NombreUsuario == request.NombreUsuario);

            if (existingUser != null)
            {
                throw new InvalidOperationException(
                    existingUser.Email == request.Email 
                        ? "El email ya está registrado" 
                        : "El nombre de usuario ya está en uso");
            }

            // 2. Generar salt
            var salt = _hashService.GenerateSalt(32);

            // 3. Derivar hash de contraseña con PBKDF2
            var passwordHash = _hashService.DeriveKey(
                request.Password,
                salt,
                iterations: 100000,
                keyLength: 64);

            // 4. Generar par de claves RSA
            var (publicKey, privateKey) = _rsaService.GenerateKeyPair(2048);

            // 5. Crear usuario
            var user = new User
            {
                NombreUsuario = request.NombreUsuario,
                Email = request.Email,
                TipoUsuario = request.TipoUsuario,
                PasswordHash = passwordHash,
                Salt = salt,
                ClavePublicaRSA = publicKey,
                FechaRegistro = DateTime.UtcNow,
                UltimoAcceso = DateTime.UtcNow,
                Activo = true
            };

            _context.Usuarios.Add(user);
            await _context.SaveChangesAsync();

            // 6. Si es administrador, crear clave HMAC
            if (request.TipoUsuario == "Administrador")
            {
                var hmacKey = _hmacService.GenerateKey(64);
                var publicKeyBytes = Encoding.UTF8.GetBytes(publicKey);
                var fingerprint = _hashService.ComputeSha256(publicKeyBytes);

                var userKeys = new UserKeys
                {
                    IdUsuario = user.IdUsuario,
                    ClaveHMAC = hmacKey,
                    FingerprintClavePublica = fingerprint,
                    FechaCreacion = DateTime.UtcNow,
                    FechaExpiracion = DateTime.UtcNow.AddYears(1)
                };

                _context.ClavesUsuarios.Add(userKeys);
                await _context.SaveChangesAsync();
            }

            // 7. Generar token JWT
            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Success = true,
                UserId = user.IdUsuario,
                Token = token,
                Email = user.Email,
                Username = user.NombreUsuario,
                UserType = user.TipoUsuario,
                Message = "Usuario registrado exitosamente"
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // 1. Buscar usuario por email
            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Email o contraseña incorrectos");
            }

            // 2. Verificar si el usuario está activo
            if (!user.Activo)
            {
                throw new UnauthorizedAccessException("El usuario está inactivo");
            }

            // 3. Verificar contraseña
            var passwordHash = _hashService.DeriveKey(
                request.Password,
                user.Salt,
                iterations: 100000,
                keyLength: 64);

            if (!passwordHash.SequenceEqual(user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Email o contraseña incorrectos");
            }

            // 4. Actualizar último acceso
            user.UltimoAcceso = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 5. Generar token JWT
            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Success = true,
                UserId = user.IdUsuario,
                Token = token,
                Email = user.Email,
                Username = user.NombreUsuario,
                UserType = user.TipoUsuario,
                Message = "Login exitoso"
            };
        }

        public async Task<UserResponse> GetCurrentUserAsync(int userId)
        {
            var user = await _context.Usuarios.FindAsync(userId);

            if (user == null || !user.Activo)
            {
                throw new UnauthorizedAccessException("Usuario no encontrado");
            }

            return new UserResponse
            {
                IdUsuario = user.IdUsuario,
                NombreUsuario = user.NombreUsuario,
                Email = user.Email,
                TipoUsuario = user.TipoUsuario,
                FechaRegistro = user.FechaRegistro,
                UltimoAcceso = user.UltimoAcceso,
                Activo = user.Activo
            };
        }

        public async Task<bool> ValidateUserCredentialsAsync(string email, string password)
        {
            try
            {
                var user = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == email && u.Activo);

                if (user == null) return false;

                var passwordHash = _hashService.DeriveKey(
                    password,
                    user.Salt,
                    iterations: 100000,
                    keyLength: 64);

                return passwordHash.SequenceEqual(user.PasswordHash);
            }
            catch
            {
                return false;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.IdUsuario.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Name, user.NombreUsuario),
                new Claim(ClaimTypes.Role, user.TipoUsuario),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
