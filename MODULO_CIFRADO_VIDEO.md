# Módulo de Cifrado y Gestión de Videos

## Descripción General

Este módulo implementa un sistema completo de cifrado, almacenamiento seguro y gestión de videos para administradores, utilizando las siguientes tecnologías criptográficas:

- **ChaCha20-Poly1305**: Cifrado autenticado (AEAD) de videos
- **RSA-OAEP**: Cifrado asimétrico de KEKs (Key Encryption Keys)
- **SHA-256**: Hash criptográfico de videos originales
- **HMAC-SHA256**: Autenticación de mensajes de videos cifrados
- **PBKDF2**: Derivación de claves HMAC específicas por usuario

## Arquitectura del Sistema

### Flujo de Cifrado de Videos

1. **Generación de KEK**: Se genera una clave aleatoria de 256 bits por video
2. **Cifrado del video**: ChaCha20-Poly1305 cifra el video con la KEK
3. **Hash del original**: Se calcula SHA-256 del video original
4. **Cifrado de KEK**: La KEK se cifra con la clave pública RSA del servidor
5. **HMAC del cifrado**: Se genera HMAC-SHA256 del video cifrado usando la clave HMAC del administrador
6. **Almacenamiento**: Video cifrado + metadatos criptográficos se guardan en BD

### Componentes Principales

#### 1. VideoEncryptionService

**Responsabilidades**:
- Cifrado/descifrado de videos con ChaCha20-Poly1305
- Generación y cifrado de KEKs
- Cálculo de hashes SHA-256
- Generación de HMACs
- Verificación de integridad

**Métodos**:
```csharp
Task<VideoEncryptionResult> EncryptVideoAsync(
    string inputPath,           // Ruta del video original
    string outputPath,          // Ruta donde guardar cifrado
    byte[] hmacKey,            // Clave HMAC del administrador
    string serverPublicKey     // Clave pública RSA del servidor
)

Task DecryptVideoAsync(
    string encryptedPath,      // Video cifrado
    string outputPath,         // Destino descifrado
    byte[] kek,               // KEK descifrada
    byte[] nonce,             // Nonce usado en cifrado
    byte[] authTag            // Tag de autenticación
)

bool VerifyVideoIntegrity(
    string videoPath,         // Video a verificar
    byte[] expectedHmac,      // HMAC almacenado
    byte[] hmacKey           // Clave HMAC del admin
)

Task<byte[]> CalculateFileHash(string filePath)
```

**Resultado de Cifrado** (`VideoEncryptionResult`):
```csharp
public class VideoEncryptionResult
{
    public byte[] EncryptedKek { get; set; }           // KEK cifrada con RSA
    public byte[] Nonce { get; set; }                  // Nonce de ChaCha20 (12 bytes)
    public byte[] AuthTag { get; set; }                // Tag Poly1305 (16 bytes)
    public byte[] OriginalHash { get; set; }           // SHA-256 del original
    public byte[] HmacOfEncryptedVideo { get; set; }   // HMAC del cifrado
    public long OriginalSizeBytes { get; set; }
    public long EncryptedSizeBytes { get; set; }
}
```

#### 2. VideoService

**Responsabilidades**:
- Orquestación del flujo completo de subida de videos
- Validación de permisos (solo administradores)
- Gestión de archivos temporales
- Persistencia de metadatos en base de datos
- Verificación de integridad
- Actualización de metadata

**Métodos**:
```csharp
Task<ApiResponse<VideoResponse>> UploadVideoAsync(
    UploadVideoRequest request,
    Stream videoStream
)

Task<ApiResponse<VideoIntegrityResponse>> VerifyVideoIntegrityAsync(
    int videoId,
    int adminId
)

Task<ApiResponse<VideoResponse>> UpdateVideoMetadataAsync(
    int videoId,
    UpdateVideoMetadataRequest request,
    int adminId
)

Task<ApiResponse<bool>> DeleteVideoAsync(int videoId, int adminId)

Task<ApiResponse<List<VideoListResponse>>> GetVideosByAdminAsync(int adminId)
```

#### 3. VideosController

**Endpoints REST**:

| Método | Endpoint | Autorización | Descripción |
|--------|----------|--------------|-------------|
| POST | `/api/videos/upload` | Administrador | Sube y cifra un video |
| GET | `/api/videos/my-videos` | Administrador | Lista videos del admin autenticado |
| GET | `/api/videos/{id}` | Autenticado | Obtiene detalles de un video |
| POST | `/api/videos/{id}/verify-integrity` | Administrador (dueño) | Verifica integridad HMAC |
| PUT | `/api/videos/{id}/metadata` | Administrador (dueño) | Actualiza título/descripción |
| DELETE | `/api/videos/{id}` | Administrador (dueño) | Eliminación lógica (soft delete) |

## Modelos de Datos

### Base de Datos

#### Tabla `Videos`
```sql
IdVideo (int, PK)
IdAdministrador (int, FK → Usuarios)
TituloVideo (nvarchar(200))
Descripcion (nvarchar(1000))
NombreArchivoOriginal (nvarchar(255))
NombreArchivoCifrado (nvarchar(255))
TamañoArchivo (bigint)
RutaAlmacenamiento (nvarchar(500))
EstadoProcesamiento (nvarchar(50))  -- Disponible, Procesando, Eliminado
FechaSubida (datetime2)
FechaModificacion (datetime2)
```

#### Tabla `DatosCriptograficosVideos`
```sql
IdDatoCripto (int, PK)
IdVideo (int, FK → Videos)
KEKCifrada (varbinary(max))         -- KEK cifrada con RSA
AlgoritmoKEK (nvarchar(50))         -- "RSA-OAEP"
Nonce (varbinary(12))               -- Nonce de ChaCha20
AuthTag (varbinary(16))             -- Tag de Poly1305
AAD (varbinary(max))                -- Additional Authenticated Data (opcional)
HashSHA256Original (varbinary(32))  -- SHA-256 del video original
HMACDelVideo (varbinary(64))        -- HMAC-SHA256 del video cifrado
FechaGeneracionClaves (datetime2)
VersionAlgoritmo (nvarchar(20))     -- "1.0"
```

### DTOs

#### Request DTOs

**UploadVideoRequest**:
```csharp
public class UploadVideoRequest
{
    public int IdAdministrador { get; set; }
    public string NombreArchivo { get; set; }
    public string? Descripcion { get; set; }
}
```

**UpdateVideoMetadataRequest**:
```csharp
public class UpdateVideoMetadataRequest
{
    [StringLength(200)]
    public string? TituloVideo { get; set; }
    
    [StringLength(1000)]
    public string? Descripcion { get; set; }
}
```

#### Response DTOs

**VideoResponse**:
```csharp
public class VideoResponse
{
    public int IdVideo { get; set; }
    public string TituloVideo { get; set; }
    public string Descripcion { get; set; }
    public string NombreArchivoOriginal { get; set; }
    public long TamañoArchivo { get; set; }
    public string EstadoProcesamiento { get; set; }
    public DateTime FechaSubida { get; set; }
    public int IdAdministrador { get; set; }
    public string NombreAdministrador { get; set; }
    public string Message { get; set; }
}
```

**VideoIntegrityResponse**:
```csharp
public class VideoIntegrityResponse
{
    public int IdVideo { get; set; }
    public string TituloVideo { get; set; }
    public bool IsValid { get; set; }
    public string HashSHA256Original { get; set; }  // Base64
    public DateTime FechaVerificacion { get; set; }
    public string Message { get; set; }
}
```

## Guía de Uso

### 1. Preparación del Ambiente

#### Migración de Base de Datos
```bash
# Desde el directorio SecureVideoStreaming.Data
cd SecureVideoStreaming.Data

# Eliminar base de datos anterior (si existe)
sqlcmd -S localhost\SQLEXPRESS -Q "DROP DATABASE IF EXISTS Data_base_cripto"

# Aplicar migración
dotnet ef database update --startup-project ../SecureVideoStreaming.API

# Verificar tablas creadas
sqlcmd -S localhost\SQLEXPRESS -d Data_base_cripto -Q "SELECT name FROM sys.tables"
```

#### Verificar Generación de Claves del Servidor
```bash
# Al iniciar la API, el KeyManagementService auto-genera claves si no existen
# Verificar archivos en Storage/Keys/:
ls -la Storage/Keys/
# Debe contener:
# - server_private_key.pem (RSA 4096 bits)
# - server_public_key.pem
# - .master_secret (para derivación HMAC)
```

### 2. Registro de Administrador

**Endpoint**: `POST /api/auth/register`

```json
{
  "nombreUsuario": "admin1",
  "email": "admin@example.com",
  "password": "SecurePass123!",
  "tipoUsuario": "Administrador"
}
```

**Respuesta**:
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "idUsuario": 1,
      "nombreUsuario": "admin1",
      "email": "admin@example.com",
      "tipoUsuario": "Administrador"
    }
  },
  "message": "Usuario registrado exitosamente"
}
```

**Proceso Interno**:
1. Password → PBKDF2-SHA256 (210k iteraciones, salt aleatorio)
2. Generación de par de claves RSA-2048 para el usuario
3. Derivación de clave HMAC de 64 bytes:
   ```
   HMAC_KEY = PBKDF2(
       password: master_secret + user_data,
       salt: SHA256(userId + "_" + email),
       iterations: 210000,
       keyLength: 64 bytes
   )
   ```
4. Almacenamiento en `ClavesUsuarios`

### 3. Subir Video Cifrado

**Endpoint**: `POST /api/videos/upload`

**Headers**:
```
Authorization: Bearer <JWT_TOKEN>
Content-Type: multipart/form-data
```

**Body (form-data)**:
```
titulo: "Mi Video Seguro"
descripcion: "Video de demostración"
videoFile: [archivo.mp4]
```

**Ejemplo con cURL**:
```bash
curl -X POST http://localhost:5000/api/videos/upload \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -F "titulo=Video Demo" \
  -F "descripcion=Prueba de cifrado" \
  -F "videoFile=@/path/to/video.mp4"
```

**Respuesta**:
```json
{
  "success": true,
  "data": {
    "idVideo": 1,
    "tituloVideo": "Video Demo",
    "descripcion": "Prueba de cifrado",
    "nombreArchivoOriginal": "video.mp4",
    "tamañoArchivo": 15728640,
    "estadoProcesamiento": "Disponible",
    "fechaSubida": "2024-11-16T10:30:00Z",
    "idAdministrador": 1,
    "nombreAdministrador": "admin1",
    "message": "Video subido y cifrado exitosamente"
  },
  "message": "Video subido exitosamente"
}
```

**Proceso Interno**:
1. Validación de autenticación JWT y rol Administrador
2. Obtención de clave HMAC del administrador desde `ClavesUsuarios`
3. Guardado temporal del video original
4. `VideoEncryptionService.EncryptVideoAsync()`:
   - Lectura del archivo en chunks (buffer 81920 bytes)
   - Cálculo SHA-256 del original
   - Generación KEK aleatoria (32 bytes)
   - Cifrado con ChaCha20-Poly1305 (nonce 12 bytes, tag 16 bytes)
   - Cifrado de KEK con RSA-OAEP del servidor
   - Cálculo HMAC-SHA256 del video cifrado
5. Almacenamiento en `Storage/Videos/` con nombre `{GUID}.encrypted`
6. Persistencia en BD:
   - Registro en `Videos`
   - Registro en `DatosCriptograficosVideos`
7. Eliminación del archivo temporal

### 4. Listar Mis Videos

**Endpoint**: `GET /api/videos/my-videos`

**Headers**:
```
Authorization: Bearer <JWT_TOKEN>
```

**Respuesta**:
```json
{
  "success": true,
  "data": [
    {
      "idVideo": 1,
      "tituloVideo": "Video Demo",
      "descripcion": "Prueba de cifrado",
      "tamañoArchivo": 15728640,
      "duracion": null,
      "formatoVideo": null,
      "estadoProcesamiento": "Disponible",
      "fechaSubida": "2024-11-16T10:30:00Z",
      "nombreAdministrador": "admin1"
    }
  ],
  "message": null
}
```

### 5. Verificar Integridad

**Endpoint**: `POST /api/videos/{id}/verify-integrity`

**Headers**:
```
Authorization: Bearer <JWT_TOKEN>
```

**Ejemplo**:
```bash
curl -X POST http://localhost:5000/api/videos/1/verify-integrity \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Respuesta (integridad válida)**:
```json
{
  "success": true,
  "data": {
    "idVideo": 1,
    "tituloVideo": "Video Demo",
    "isValid": true,
    "hashSHA256Original": "5Z3q7x8y9z0a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q=",
    "fechaVerificacion": "2024-11-16T11:00:00Z",
    "message": "La integridad del video es válida"
  },
  "message": "Verificación exitosa"
}
```

**Respuesta (integridad comprometida)**:
```json
{
  "success": true,
  "data": {
    "idVideo": 1,
    "tituloVideo": "Video Demo",
    "isValid": false,
    "hashSHA256Original": "5Z3q7x8y9z0a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q=",
    "fechaVerificacion": "2024-11-16T11:00:00Z",
    "message": "ALERTA: La integridad del video ha sido comprometida"
  },
  "message": "Verificación fallida"
}
```

**Proceso Interno**:
1. Verificar que el usuario autenticado es el dueño del video
2. Obtener HMAC almacenado de `DatosCriptograficosVideos`
3. Obtener clave HMAC del administrador
4. `VideoEncryptionService.VerifyVideoIntegrity()`:
   - Leer archivo cifrado en chunks
   - Calcular HMAC-SHA256
   - Comparación timing-safe con `CryptographicOperations.FixedTimeEquals()`
5. Retornar resultado booleano

### 6. Actualizar Metadata

**Endpoint**: `PUT /api/videos/{id}/metadata`

**Headers**:
```
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

**Body**:
```json
{
  "tituloVideo": "Nuevo Título",
  "descripcion": "Descripción actualizada"
}
```

**Respuesta**:
```json
{
  "success": true,
  "data": {
    "idVideo": 1,
    "tituloVideo": "Nuevo Título",
    "descripcion": "Descripción actualizada",
    "nombreArchivoOriginal": "video.mp4",
    "tamañoArchivo": 15728640,
    "estadoProcesamiento": "Disponible",
    "fechaSubida": "2024-11-16T10:30:00Z",
    "idAdministrador": 1,
    "nombreAdministrador": "admin1",
    "message": "Metadata actualizada exitosamente"
  },
  "message": "Metadata actualizada exitosamente"
}
```

### 7. Eliminar Video

**Endpoint**: `DELETE /api/videos/{id}`

**Headers**:
```
Authorization: Bearer <JWT_TOKEN>
```

**Respuesta**:
```json
{
  "success": true,
  "data": true,
  "message": "Video eliminado exitosamente"
}
```

**Nota**: Se realiza soft delete (cambia `EstadoProcesamiento` a "Eliminado"). El archivo cifrado permanece en disco.

## Consideraciones de Seguridad

### 1. Confidencialidad
- **ChaCha20**: Cifrado de flujo robusto, recomendado por IETF (RFC 7539)
- **Nonces únicos**: Generados con `RandomNumberGenerator.GetBytes(12)`
- **KEK por video**: Cada video tiene su propia clave de cifrado
- **RSA-OAEP**: Cifrado asimétrico de KEKs con padding seguro

### 2. Integridad
- **Poly1305**: MAC integrado en ChaCha20-Poly1305 (AEAD)
- **HMAC-SHA256**: Autenticación adicional del video cifrado
- **SHA-256**: Hash del video original para verificación futura
- **Comparación timing-safe**: Protección contra ataques de timing

### 3. Autenticación
- **JWT con roles**: Solo administradores pueden subir videos
- **Verificación de propiedad**: Solo el dueño puede verificar/modificar/eliminar
- **PBKDF2 (210k iteraciones)**: Derivación segura de passwords

### 4. Gestión de Claves
- **Clave maestra del servidor**: Almacenada en `Storage/Keys/.master_secret`
- **Derivación HMAC determinística**: Permite regenerar claves HMAC por usuario
- **RSA 4096 bits del servidor**: Protección a largo plazo de KEKs
- **Rotación de claves**: Futuro - implementar versionado en `VersionAlgoritmo`

### 5. Protecciones Implementadas
- **Soft delete**: Videos eliminados conservan datos para auditoría
- **Validación de tamaños**: KEK=32 bytes, Nonce=12 bytes, AuthTag=16 bytes
- **Manejo de errores**: Sin exposición de stack traces en respuestas
- **Límite de tamaño**: 500 MB por archivo (`RequestSizeLimit`)

## Casos de Uso Avanzados

### Descifrado de Video (futuro)

```csharp
// 1. Obtener KEK cifrada desde DatosCriptograficosVideos
var cryptoData = await _context.DatosCriptograficosVideos
    .FirstOrDefaultAsync(c => c.IdVideo == videoId);

// 2. Descifrar KEK con clave privada del servidor
var serverPrivateKey = _keyManagementService.GetServerPrivateKey();
var kek = _kekService.DecryptKek(cryptoData.KEKCifrada, serverPrivateKey);

// 3. Descifrar video
await _videoEncryptionService.DecryptVideoAsync(
    encryptedPath: video.RutaAlmacenamiento,
    outputPath: tempOutputPath,
    kek: kek,
    nonce: cryptoData.Nonce,
    authTag: cryptoData.AuthTag
);

// 4. Verificar hash SHA-256 del descifrado
var decryptedHash = await _videoEncryptionService.CalculateFileHash(tempOutputPath);
bool hashMatches = CryptographicOperations.FixedTimeEquals(
    decryptedHash, 
    cryptoData.HashSHA256Original
);
```

### Auditoría de Accesos

```csharp
// Al verificar integridad, registrar en AccessLog
var accessLog = new AccessLog
{
    IdUsuario = adminId,
    IdVideo = videoId,
    TipoAcceso = "VerificacionIntegridad",
    Exitoso = isValid,
    DireccionIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
    FechaHoraAcceso = DateTime.UtcNow
};
_context.RegistroAccesos.Add(accessLog);
await _context.SaveChangesAsync();
```

### Re-cifrado con Nueva Clave

```csharp
// Escenario: Rotación de clave HMAC del administrador
// 1. Descifrar video con KEK existente
// 2. Generar nueva clave HMAC
var newHmacKey = _keyManagementService.DeriveHmacKeyForUser(adminId, email);
// 3. Recalcular HMAC del video cifrado
var newHmac = _hmacService.ComputeHmac(encryptedVideoBytes, newHmacKey);
// 4. Actualizar DatosCriptograficosVideos
cryptoData.HMACDelVideo = newHmac;
cryptoData.FechaGeneracionClaves = DateTime.UtcNow;
await _context.SaveChangesAsync();
```

## Pruebas Unitarias

### Ejemplo: Test de Cifrado Completo

```csharp
[Fact]
public async Task EncryptVideoAsync_ValidInput_ReturnsValidResult()
{
    // Arrange
    var inputPath = "test_video.mp4";
    var outputPath = "test_encrypted.bin";
    var hmacKey = new byte[64];
    RandomNumberGenerator.Fill(hmacKey);
    var serverPublicKey = _keyManagementService.GetServerPublicKey();

    // Act
    var result = await _videoEncryptionService.EncryptVideoAsync(
        inputPath, outputPath, hmacKey, serverPublicKey
    );

    // Assert
    Assert.NotNull(result);
    Assert.Equal(32, result.EncryptedKek.Length);  // KEK cifrada con RSA
    Assert.Equal(12, result.Nonce.Length);
    Assert.Equal(16, result.AuthTag.Length);
    Assert.Equal(32, result.OriginalHash.Length);   // SHA-256
    Assert.Equal(64, result.HmacOfEncryptedVideo.Length);  // HMAC-SHA256
    Assert.True(File.Exists(outputPath));
}
```

### Ejemplo: Test de Verificación de Integridad

```csharp
[Fact]
public void VerifyVideoIntegrity_ValidHmac_ReturnsTrue()
{
    // Arrange
    var videoPath = "encrypted_video.bin";
    var videoBytes = File.ReadAllBytes(videoPath);
    var hmacKey = new byte[64];
    RandomNumberGenerator.Fill(hmacKey);
    var expectedHmac = _hmacService.ComputeHmac(videoBytes, hmacKey);

    // Act
    var isValid = _videoEncryptionService.VerifyVideoIntegrity(
        videoPath, expectedHmac, hmacKey
    );

    // Assert
    Assert.True(isValid);
}

[Fact]
public void VerifyVideoIntegrity_TamperedVideo_ReturnsFalse()
{
    // Arrange
    var videoPath = "encrypted_video.bin";
    var hmacKey = new byte[64];
    var originalHmac = _hmacService.ComputeHmac(
        File.ReadAllBytes(videoPath), hmacKey
    );
    
    // Tamper with video
    var videoBytes = File.ReadAllBytes(videoPath);
    videoBytes[100] ^= 0xFF;  // Flip byte
    File.WriteAllBytes(videoPath, videoBytes);

    // Act
    var isValid = _videoEncryptionService.VerifyVideoIntegrity(
        videoPath, originalHmac, hmacKey
    );

    // Assert
    Assert.False(isValid);
}
```

## Troubleshooting

### Error: "El administrador no tiene clave HMAC configurada"
**Causa**: Usuario creado antes de implementar generación automática de HMAC.
**Solución**:
```sql
-- Verificar existencia de clave HMAC
SELECT uk.IdClaveUsuario, uk.IdUsuario, u.Email
FROM ClavesUsuarios uk
INNER JOIN Usuarios u ON uk.IdUsuario = u.IdUsuario
WHERE u.IdUsuario = 1;

-- Si no existe, regenerar mediante re-registro o script manual
```

### Error: "No se encontraron datos criptográficos para este video"
**Causa**: Fallo en transacción de subida, video sin metadata criptográfica.
**Solución**:
```sql
-- Verificar consistencia
SELECT v.IdVideo, v.TituloVideo, dc.IdDatoCripto
FROM Videos v
LEFT JOIN DatosCriptograficosVideos dc ON v.IdVideo = dc.IdVideo
WHERE dc.IdDatoCripto IS NULL;

-- Eliminar videos huérfanos
UPDATE Videos SET EstadoProcesamiento = 'Eliminado'
WHERE IdVideo IN (SELECT v.IdVideo FROM Videos v
    LEFT JOIN DatosCriptograficosVideos dc ON v.IdVideo = dc.IdVideo
    WHERE dc.IdDatoCripto IS NULL);
```

### Error: Verificación de integridad falla inesperadamente
**Diagnóstico**:
```csharp
// 1. Verificar longitud de HMAC esperado
Console.WriteLine($"HMAC Length: {cryptoData.HMACDelVideo.Length}");  // Debe ser 64

// 2. Recalcular HMAC manualmente
var recomputedHmac = _hmacService.ComputeHmac(
    File.ReadAllBytes(video.RutaAlmacenamiento),
    userKeys.ClaveHMAC
);
var matches = CryptographicOperations.FixedTimeEquals(
    recomputedHmac, cryptoData.HMACDelVideo
);
Console.WriteLine($"Manual verification: {matches}");

// 3. Verificar que archivo no fue modificado
var currentSize = new FileInfo(video.RutaAlmacenamiento).Length;
Console.WriteLine($"Current: {currentSize}, DB: {video.TamañoArchivo}");
```

## Roadmap

### Funcionalidades Futuras
- [ ] **Streaming seguro**: Descifrado en tiempo real con HLS/DASH
- [ ] **Permisos granulares**: Compartir videos cifrados con usuarios finales
- [ ] **Rotación automática de KEKs**: Tras detección de compromisos
- [ ] **Cifrado del lado del cliente**: Videos nunca tocan servidor sin cifrar
- [ ] **Auditoría completa**: Logs detallados en `RegistroAccesos`
- [ ] **Compresión antes de cifrado**: Reducir tamaño sin comprometer seguridad
- [ ] **Soporte multi-región**: Replicación de videos cifrados en múltiples centros de datos

## Referencias

- [RFC 7539 - ChaCha20 and Poly1305](https://datatracker.ietf.org/doc/html/rfc7539)
- [NIST SP 800-132 - PBKDF2](https://csrc.nist.gov/publications/detail/sp/800-132/final)
- [OWASP Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
- [RFC 8017 - RSA-OAEP](https://datatracker.ietf.org/doc/html/rfc8017)
- [FIPS 180-4 - SHA-256](https://csrc.nist.gov/publications/detail/fips/180/4/final)
