# 🔧 Guía Rápida: Trabajar con las Nuevas Entidades

## 📘 Ejemplos de Uso

Esta guía muestra cómo trabajar con las entidades adaptadas a la base de datos `Data_base_cripto`.

---

## 1️⃣ **Creación de Usuarios**

### **Ejemplo: Registro de Usuario Administrador**

```csharp
using SecureVideoStreaming.Data.Context;
using SecureVideoStreaming.Models.Entities;
using SecureVideoStreaming.Services.Cryptography.Interfaces;

public class UserService
{
    private readonly ApplicationDbContext _context;
    private readonly IHashService _hashService;
    private readonly IRsaService _rsaService;

    public async Task<User> RegisterUserAsync(
        string nombreUsuario, 
        string email, 
        string password, 
        string tipoUsuario)
    {
        // 1. Generar salt
        var salt = _hashService.GenerateSalt(32);
        
        // 2. Derivar hash de contraseña con PBKDF2
        var passwordHash = _hashService.DeriveKey(
            password, 
            salt, 
            iterations: 100000, 
            keyLength: 64);
        
        // 3. Generar par de claves RSA
        var (publicKey, privateKey) = _rsaService.GenerateKeyPair(2048);
        
        // 4. Crear usuario
        var user = new User
        {
            NombreUsuario = nombreUsuario,
            Email = email,
            TipoUsuario = tipoUsuario, // "Administrador" o "Usuario"
            PasswordHash = passwordHash,
            Salt = salt,
            ClavePublicaRSA = publicKey,
            FechaRegistro = DateTime.UtcNow,
            Activo = true
        };
        
        _context.Usuarios.Add(user);
        await _context.SaveChangesAsync();
        
        // 5. IMPORTANTE: Guardar la clave privada del usuario de forma segura
        // (Por ejemplo, cifrada con una clave maestra del sistema)
        
        return user;
    }
}
```

---

## 2️⃣ **Gestión de Claves de Usuario**

### **Ejemplo: Agregar Clave HMAC para Administrador**

```csharp
public async Task<UserKeys> CreateUserKeysAsync(int idUsuario)
{
    // 1. Generar clave HMAC (64 bytes para SHA-512)
    var hmacKey = RandomNumberGenerator.GetBytes(64);
    
    // 2. Obtener clave pública del usuario
    var user = await _context.Usuarios.FindAsync(idUsuario);
    if (user == null) throw new Exception("Usuario no encontrado");
    
    // 3. Calcular fingerprint de la clave pública (SHA-256)
    var publicKeyBytes = Encoding.UTF8.GetBytes(user.ClavePublicaRSA);
    var fingerprint = SHA256.HashData(publicKeyBytes);
    
    // 4. Crear registro de claves
    var userKeys = new UserKeys
    {
        IdUsuario = idUsuario,
        ClaveHMAC = hmacKey,
        FingerprintClavePublica = fingerprint,
        FechaCreacion = DateTime.UtcNow,
        FechaExpiracion = DateTime.UtcNow.AddYears(1) // Opcional
    };
    
    _context.ClavesUsuarios.Add(userKeys);
    await _context.SaveChangesAsync();
    
    return userKeys;
}
```

---

## 3️⃣ **Subida y Cifrado de Videos**

### **Ejemplo: Proceso Completo de Upload**

```csharp
public async Task<Video> UploadVideoAsync(
    int idAdministrador,
    string tituloVideo,
    string descripcion,
    Stream videoStream,
    string nombreArchivoOriginal)
{
    // 1. Generar KEK (Key Encryption Key) para el video
    var kek = _chaCha20Service.GenerateKey(); // 32 bytes
    
    // 2. Calcular hash del video original
    videoStream.Position = 0;
    var hashOriginal = _hashService.ComputeSha256(videoStream);
    
    // 3. Cifrar el video con ChaCha20-Poly1305
    videoStream.Position = 0;
    byte[] videoBytes = new byte[videoStream.Length];
    await videoStream.ReadAsync(videoBytes, 0, videoBytes.Length);
    
    var (ciphertext, nonce, authTag) = _chaCha20Service.Encrypt(
        videoBytes, 
        kek);
    
    // 4. Obtener clave HMAC del administrador
    var userKeys = await _context.ClavesUsuarios
        .FirstOrDefaultAsync(k => k.IdUsuario == idAdministrador);
    
    if (userKeys?.ClaveHMAC == null)
        throw new Exception("Administrador no tiene clave HMAC");
    
    // 5. Calcular HMAC del video cifrado
    var hmac = _hmacService.Compute(ciphertext, userKeys.ClaveHMAC);
    
    // 6. Cifrar KEK con clave pública del servidor (RSA)
    var serverPublicKey = GetServerPublicKey(); // Implementar
    var encryptedKek = _rsaService.Encrypt(kek, serverPublicKey);
    
    // 7. Guardar video cifrado en disco
    var nombreArchivoCifrado = $"{Guid.NewGuid()}.encrypted";
    var rutaAlmacenamiento = Path.Combine("Storage/Videos", nombreArchivoCifrado);
    await File.WriteAllBytesAsync(rutaAlmacenamiento, ciphertext);
    
    // 8. Crear registro de video
    var video = new Video
    {
        IdAdministrador = idAdministrador,
        TituloVideo = tituloVideo,
        Descripcion = descripcion,
        NombreArchivoOriginal = nombreArchivoOriginal,
        NombreArchivoCifrado = nombreArchivoCifrado,
        TamañoArchivo = ciphertext.Length,
        RutaAlmacenamiento = rutaAlmacenamiento,
        EstadoProcesamiento = "Disponible",
        FechaSubida = DateTime.UtcNow
    };
    
    _context.Videos.Add(video);
    await _context.SaveChangesAsync();
    
    // 9. Crear registro de datos criptográficos
    var cryptoData = new CryptoData
    {
        IdVideo = video.IdVideo,
        KEKCifrada = encryptedKek,
        AlgoritmoKEK = "ChaCha20-Poly1305",
        Nonce = nonce,
        AuthTag = authTag,
        HashSHA256Original = hashOriginal,
        HMACDelVideo = hmac,
        FechaGeneracionClaves = DateTime.UtcNow,
        VersionAlgoritmo = "1.0"
    };
    
    _context.DatosCriptograficosVideos.Add(cryptoData);
    await _context.SaveChangesAsync();
    
    return video;
}
```

---

## 4️⃣ **Otorgar Permisos de Acceso**

### **Ejemplo: Otorgar Permiso de Lectura**

```csharp
public async Task<Permission> GrantPermissionAsync(
    int idVideo,
    int idUsuario,
    int otorgadoPor,
    string tipoPermiso = "Lectura",
    DateTime? fechaExpiracion = null)
{
    // 1. Verificar que el video existe
    var video = await _context.Videos.FindAsync(idVideo);
    if (video == null) throw new Exception("Video no encontrado");
    
    // 2. Verificar que el otorgante es el administrador del video
    if (video.IdAdministrador != otorgadoPor)
        throw new UnauthorizedAccessException("Solo el administrador puede otorgar permisos");
    
    // 3. Verificar que no existe un permiso activo
    var existingPermission = await _context.Permisos
        .FirstOrDefaultAsync(p => p.IdVideo == idVideo && p.IdUsuario == idUsuario);
    
    if (existingPermission != null && existingPermission.TipoPermiso != "Revocado")
        throw new Exception("El usuario ya tiene un permiso activo");
    
    // 4. Crear permiso
    var permission = new Permission
    {
        IdVideo = idVideo,
        IdUsuario = idUsuario,
        TipoPermiso = tipoPermiso,
        FechaOtorgamiento = DateTime.UtcNow,
        FechaExpiracion = fechaExpiracion,
        OtorgadoPor = otorgadoPor,
        NumeroAccesos = 0
    };
    
    _context.Permisos.Add(permission);
    await _context.SaveChangesAsync();
    
    return permission;
}
```

---

## 5️⃣ **Descarga y Descifrado de Videos**

### **Ejemplo: Proceso Completo de Download**

```csharp
public async Task<byte[]> DownloadVideoAsync(
    int idVideo,
    int idUsuario,
    string ipAddress,
    string userAgent)
{
    try
    {
        // 1. Verificar permisos
        var permission = await _context.Permisos
            .FirstOrDefaultAsync(p => 
                p.IdVideo == idVideo && 
                p.IdUsuario == idUsuario &&
                p.TipoPermiso != "Revocado" &&
                (p.FechaExpiracion == null || p.FechaExpiracion > DateTime.UtcNow));
        
        if (permission == null)
        {
            await LogAccessAsync(idUsuario, idVideo, "Descarga", false, 
                "Permiso denegado", ipAddress, userAgent);
            throw new UnauthorizedAccessException("No tiene permiso para acceder a este video");
        }
        
        // 2. Obtener video y datos criptográficos
        var video = await _context.Videos
            .Include(v => v.DatosCriptograficos)
            .FirstOrDefaultAsync(v => v.IdVideo == idVideo);
        
        if (video?.DatosCriptograficos == null)
            throw new Exception("Video o datos criptográficos no encontrados");
        
        // 3. Leer video cifrado del disco
        var ciphertext = await File.ReadAllBytesAsync(video.RutaAlmacenamiento);
        
        // 4. Descifrar KEK con clave privada del servidor
        var serverPrivateKey = GetServerPrivateKey(); // Implementar
        var kek = _rsaService.Decrypt(
            video.DatosCriptograficos.KEKCifrada, 
            serverPrivateKey);
        
        // 5. Descifrar video con ChaCha20-Poly1305
        var plaintext = _chaCha20Service.Decrypt(
            ciphertext,
            kek,
            video.DatosCriptograficos.Nonce,
            video.DatosCriptograficos.AuthTag);
        
        // 6. Verificar integridad (SHA-256)
        var hash = _hashService.ComputeSha256(plaintext);
        if (!hash.SequenceEqual(video.DatosCriptograficos.HashSHA256Original))
            throw new Exception("Verificación de integridad fallida");
        
        // 7. Verificar HMAC del administrador
        var adminKeys = await _context.ClavesUsuarios
            .FirstOrDefaultAsync(k => k.IdUsuario == video.IdAdministrador);
        
        if (adminKeys?.ClaveHMAC != null)
        {
            var isHmacValid = _hmacService.Verify(
                ciphertext,
                video.DatosCriptograficos.HMACDelVideo,
                adminKeys.ClaveHMAC);
            
            if (!isHmacValid)
                throw new Exception("Verificación de HMAC fallida");
        }
        
        // 8. Actualizar estadísticas de permiso
        permission.NumeroAccesos++;
        permission.UltimoAcceso = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        // 9. Registrar acceso exitoso
        await LogAccessAsync(idUsuario, idVideo, "Descarga", true, 
            null, ipAddress, userAgent);
        
        return plaintext;
    }
    catch (Exception ex)
    {
        await LogAccessAsync(idUsuario, idVideo, "Descarga", false, 
            ex.Message, ipAddress, userAgent);
        throw;
    }
}
```

---

## 6️⃣ **Registro de Auditoría**

### **Ejemplo: Registrar Acceso**

```csharp
private async Task LogAccessAsync(
    int idUsuario,
    int idVideo,
    string tipoAcceso,
    bool exitoso,
    string? mensajeError,
    string? ipAddress,
    string? userAgent)
{
    var accessLog = new AccessLog
    {
        IdUsuario = idUsuario,
        IdVideo = idVideo,
        TipoAcceso = tipoAcceso, // "Visualizacion", "Descarga", "SolicitudClave", "Verificacion"
        Exitoso = exitoso,
        MensajeError = mensajeError,
        DireccionIP = ipAddress,
        UserAgent = userAgent,
        FechaHoraAcceso = DateTime.UtcNow
    };
    
    _context.RegistroAccesos.Add(accessLog);
    await _context.SaveChangesAsync();
}
```

---

## 7️⃣ **Revocar Permisos**

### **Ejemplo: Revocar Acceso**

```csharp
public async Task RevokePermissionAsync(
    int idPermiso,
    int revocadoPor)
{
    var permission = await _context.Permisos.FindAsync(idPermiso);
    
    if (permission == null)
        throw new Exception("Permiso no encontrado");
    
    // Verificar que el revocador es el administrador del video
    var video = await _context.Videos.FindAsync(permission.IdVideo);
    if (video.IdAdministrador != revocadoPor)
        throw new UnauthorizedAccessException("Solo el administrador puede revocar permisos");
    
    permission.TipoPermiso = "Revocado";
    permission.FechaRevocacion = DateTime.UtcNow;
    permission.RevocadoPor = revocadoPor;
    
    await _context.SaveChangesAsync();
}
```

---

## 8️⃣ **Consultas Útiles**

### **Listar Videos de un Administrador**

```csharp
var videos = await _context.Videos
    .Include(v => v.DatosCriptograficos)
    .Include(v => v.Administrador)
    .Where(v => v.IdAdministrador == idAdmin && v.EstadoProcesamiento == "Disponible")
    .OrderByDescending(v => v.FechaSubida)
    .ToListAsync();
```

### **Listar Videos Accesibles por Usuario**

```csharp
var videosAccesibles = await _context.Permisos
    .Include(p => p.Video)
    .ThenInclude(v => v.Administrador)
    .Where(p => 
        p.IdUsuario == idUsuario &&
        p.TipoPermiso != "Revocado" &&
        (p.FechaExpiracion == null || p.FechaExpiracion > DateTime.UtcNow))
    .Select(p => p.Video)
    .ToListAsync();
```

### **Auditoría de Accesos a un Video**

```csharp
var logs = await _context.RegistroAccesos
    .Include(log => log.Usuario)
    .Where(log => log.IdVideo == idVideo)
    .OrderByDescending(log => log.FechaHoraAcceso)
    .Take(100)
    .ToListAsync();
```

---

## 🛡️ **Mejores Prácticas**

### **1. Manejo de Claves**
- ✅ **NUNCA** almacenar claves privadas RSA en la BD en texto plano
- ✅ Cifrar claves privadas con una clave maestra (KMS o Azure Key Vault)
- ✅ Generar nuevas claves HMAC periódicamente (rotación)

### **2. Validaciones**
```csharp
// Validar TipoUsuario
if (tipoUsuario != "Administrador" && tipoUsuario != "Usuario")
    throw new ArgumentException("Tipo de usuario inválido");

// Validar EstadoProcesamiento
var estadosValidos = new[] { "Procesando", "Disponible", "Error", "Eliminado" };
if (!estadosValidos.Contains(estado))
    throw new ArgumentException("Estado inválido");
```

### **3. Seguridad**
```csharp
// Verificar expiración de permisos
if (permission.FechaExpiracion.HasValue && 
    permission.FechaExpiracion < DateTime.UtcNow)
{
    throw new UnauthorizedAccessException("El permiso ha expirado");
}

// Limitar intentos de acceso
var failedAttempts = await _context.RegistroAccesos
    .CountAsync(log => 
        log.IdUsuario == idUsuario && 
        !log.Exitoso && 
        log.FechaHoraAcceso > DateTime.UtcNow.AddMinutes(-15));

if (failedAttempts >= 5)
    throw new Exception("Demasiados intentos fallidos");
```

### **4. Performance**
```csharp
// Usar AsNoTracking para consultas de solo lectura
var videos = await _context.Videos
    .AsNoTracking()
    .Where(v => v.EstadoProcesamiento == "Disponible")
    .ToListAsync();

// Proyectar solo lo necesario
var videoSummaries = await _context.Videos
    .Select(v => new { v.IdVideo, v.TituloVideo, v.FechaSubida })
    .ToListAsync();
```

---

## 📚 **Referencias Rápidas**

### **Enums Simulados (Strings)**

```csharp
// TipoUsuario
public static class TipoUsuario
{
    public const string Administrador = "Administrador";
    public const string Usuario = "Usuario";
}

// EstadoProcesamiento
public static class EstadoProcesamiento
{
    public const string Procesando = "Procesando";
    public const string Disponible = "Disponible";
    public const string Error = "Error";
    public const string Eliminado = "Eliminado";
}

// TipoPermiso
public static class TipoPermiso
{
    public const string Lectura = "Lectura";
    public const string Temporal = "Temporal";
    public const string Revocado = "Revocado";
}

// TipoAcceso
public static class TipoAcceso
{
    public const string Visualizacion = "Visualizacion";
    public const string Descarga = "Descarga";
    public const string SolicitudClave = "SolicitudClave";
    public const string Verificacion = "Verificacion";
}
```

---

**¡Listo para empezar a desarrollar! 🚀**
