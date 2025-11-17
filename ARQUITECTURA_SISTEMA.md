# Arquitectura del Sistema SecureVideoStreaming

## Diagrama de Arquitectura General

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            CAPA DE PRESENTACIÓN                             │
│                         (SecureVideoStreaming.API)                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐           │
│  │ AuthController   │  │ UsersController  │  │ VideosController │           │
│  ├──────────────────┤  ├──────────────────┤  ├──────────────────┤           │
│  │ POST /register   │  │ GET /users       │  │ POST /upload     │           │
│  │ POST /login      │  │ GET /users/{id}  │  │ GET /my-videos   │           │
│  │ GET /me          │  │ PUT /users/{id}  │  │ GET /{id}        │           │
│  │                  │  │ DELETE /{id}     │  │ POST /{id}/verify│           │
│  │                  │  │                  │  │ PUT /{id}/meta   │           │
│  │                  │  │                  │  │ DELETE /{id}     │           │
│  └────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘           │
│           │                     │                     │                     │
│           └─────────────────────┴─────────────────────┘                     │
│                                 │                                           │
│                    ┌────────────▼────────────┐                              │
│                    │  Middleware Pipeline    │                              │
│                    ├─────────────────────────┤                              │
│                    │ • JWT Authentication    │                              │
│                    │ • Role Authorization    │                              │
│                    │ • Error Handling        │                              │
│                    │ • Request Logging       │                              │
│                    └────────────┬────────────┘                              │
└─────────────────────────────────┼───────────────────────────────────────────┘
                                  │
┌─────────────────────────────────▼────────────────────────────────────────────┐
│                            CAPA DE NEGOCIO                                   │
│                        (SecureVideoStreaming.Services)                       │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐     │
│  │                    Business Services Layer                          │     │
│  ├─────────────────────────────────────────────────────────────────────┤     │
│  │                                                                     │     │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐   │     │
│  │  │ AuthService  │  │ UserService  │  │   VideoService           │   │     │
│  │  ├──────────────┤  ├──────────────┤  ├──────────────────────────┤   │     │
│  │  │ • Register   │  │ • GetById    │  │ • UploadVideoAsync       │   │     │
│  │  │ • Login      │  │ • GetAll     │  │ • GetVideosByAdminAsync  │   │     │
│  │  │ • Validate   │  │ • Update     │  │ • VerifyIntegrityAsync   │   │     │
│  │  │ • GenJWT     │  │ • Delete     │  │ • UpdateMetadataAsync    │   │     │
│  │  │              │  │              │  │ • DeleteVideoAsync       │   │     │
│  │  └──────┬───────┘  └──────┬───────┘  └──────────┬───────────────┘   │     │
│  │         │                 │                     │                   │     │
│  │         └─────────────────┴─────────────────────┘                   │     │
│  │                           │                                         │     │
│  └───────────────────────────┼─────────────────────────────────────────┘     │
│                              │                                               │
│  ┌───────────────────────────▼───────────────────────────────────────────┐   │
│  │                 Cryptography Services Layer                           │   │
│  ├───────────────────────────────────────────────────────────────────────┤   │
│  │                                                                       │   │
│  │  ┌────────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │   │
│  │  │ Key Management     │  │ Video Encryption │  │ Core Crypto      │   │   │
│  │  ├────────────────────┤  ├──────────────────┤  ├──────────────────┤   │   │
│  │  │ KeyManagement      │  │ VideoEncryption  │  │ ChaCha20Poly1305 │   │   │
│  │  │ Service            │  │ Service          │  │ Service          │   │   │
│  │  │ • GetServerKeys    │  │ • EncryptVideo   │  │ • Encrypt        │   │   │
│  │  │ • GenerateKeys     │  │ • DecryptVideo   │  │ • Decrypt        │   │   │
│  │  │ • DeriveHMAC       │  │ • VerifyIntegr.  │  │ • GenKey         │   │   │
│  │  │ • SaveKeys         │  │ • CalcFileHash   │  │                  │   │   │
│  │  └────────────────────┘  └──────────────────┘  └──────────────────┘   │   │
│  │                                                                       │   │
│  │  ┌────────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │   │
│  │  │ KEK Service        │  │ Hash Service     │  │ HMAC Service     │   │   │
│  │  ├────────────────────┤  ├──────────────────┤  ├──────────────────┤   │   │
│  │  │ • GenerateKEK      │  │ • ComputeSHA256  │  │ • ComputeHMAC    │   │   │
│  │  │ • EncryptKEK (RSA) │  │ • VerifyHash     │  │ • VerifyHMAC     │   │   │
│  │  │ • DecryptKEK       │  │                  │  │ • GenKey         │   │   │
│  │  │ • GenAndEncrypt    │  │                  │  │                  │   │   │
│  │  └────────────────────┘  └──────────────────┘  └──────────────────┘   │   │
│  │                                                                       │   │
│  │  ┌────────────────────┐  ┌──────────────────┐                         │   │
│  │  │ RSA Service        │  │ KMAC Service     │                         │   │
│  │  ├────────────────────┤  ├──────────────────┤                         │   │
│  │  │ • GenKeyPair       │  │ • ComputeKMAC256 │                         │   │
│  │  │ • Encrypt (OAEP)   │  │ • VerifyKMAC     │                         │   │
│  │  │ • Decrypt          │  │ • GenKey         │                         │   │
│  │  │ • ExportKeys       │  │                  │                         │   │
│  │  └────────────────────┘  └──────────────────┘                         │   │
│  │                                                                       │   │
│  └───────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└───────────────────────────────┬──────────────────────────────────────────────┘
                                │
┌───────────────────────────────▼───────────────────────────────────────────────┐
│                         CAPA DE DATOS                                         │
│                    (SecureVideoStreaming.Data)                                │
├───────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐      │
│  │                    ApplicationDbContext                             │      │
│  ├─────────────────────────────────────────────────────────────────────┤      │
│  │                                                                     │      │
│  │  DbSet<Usuario>                    DbSet<ClavesUsuarios>            │      │
│  │  DbSet<Video>                      DbSet<DatosCriptograficosVideos> │      │
│  │  DbSet<Permiso>                    DbSet<RegistroAccesos>           │      │
│  │                                                                     │      │
│  └───────────────────────────┬─────────────────────────────────────────┘      │
│                              │                                                │
└──────────────────────────────┼────────────────────────────────────────────────┘
                               │
┌──────────────────────────────▼────────────────────────────────────────────────┐
│                        CAPA DE PERSISTENCIA                                   │
├───────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌─────────────────────────┐          ┌──────────────────────────┐            │
│  │   SQL Server Express    │          │   File System Storage    │            │
│  ├─────────────────────────┤          ├──────────────────────────┤            │
│  │ • Usuarios              │          │ Storage/Keys/            │            │
│  │ • ClavesUsuarios        │          │ • server_private_key.pem │            │
│  │ • Videos                │          │ • server_public_key.pem  │            │
│  │ • DatosCriptograficos   │          │ • .master_secret         │            │
│  │ • Permisos              │          │                          │            │
│  │ • RegistroAccesos       │          │ Storage/Videos/          │            │
│  │ • __EFMigrationsHistory │          │ • {guid}.encrypted       │            │
│  └─────────────────────────┘          └──────────────────────────┘            │
│                                                                               │
└───────────────────────────────────────────────────────────────────────────────┘
```

## Diagrama de Flujo: Subida y Cifrado de Video

```
┌─────────────┐
│   Cliente   │
│  (Swagger/  │
│   cURL)     │
└──────┬──────┘
       │
       │ POST /api/videos/upload
       │ Headers: Authorization: Bearer {JWT}
       │ Form-Data: Titulo, Descripcion, VideoFile
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│                    VideosController                              │
├──────────────────────────────────────────────────────────────────┤
│ 1. Validar JWT                                                   │
│ 2. Verificar rol "Administrador"                                 │
│ 3. Extraer userId del token                                      │
│ 4. Validar VideoFile y Titulo                                    │
└──────┬───────────────────────────────────────────────────────────┘
       │
       │ UploadVideoRequest + Stream
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│                      VideoService                                │
├──────────────────────────────────────────────────────────────────┤
│ 1. Verificar usuario es Administrador (DB query)                 │
└──────┬───────────────────────────────────────────────────────────┘
       │
       │ Query: ClavesUsuarios WHERE IdUsuario = {userId}
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│                   ApplicationDbContext                           │
├──────────────────────────────────────────────────────────────────┤
│ SELECT ClaveHMAC FROM ClavesUsuarios WHERE IdUsuario = ?         │
└──────┬───────────────────────────────────────────────────────────┘
       │
       │ ClaveHMAC (64 bytes)
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│                      VideoService                                │
├──────────────────────────────────────────────────────────────────┤
│ 2. Guardar video original temporalmente                          │
│    → Storage/Videos/temp_{guid}.tmp                              │
│                                                                  │
│ 3. Obtener clave pública del servidor                            │
└──────┬───────────────────────────────────────────────────────────┘
       │
       │ GetServerPublicKey()
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│                  KeyManagementService                            │
├──────────────────────────────────────────────────────────────────┤
│ • Lee Storage/Keys/server_public_key.pem                         │
│ • Retorna clave pública RSA-4096                                 │
└──────┬───────────────────────────────────────────────────────────┘
       │
       │ ServerPublicKey (PEM format)
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│                      VideoService                                │
├──────────────────────────────────────────────────────────────────┤
│ 4. Invocar cifrado del video                                     │
│    Params: inputPath, outputPath, hmacKey, serverPublicKey       │
└──────┬───────────────────────────────────────────────────────────┘
       │
       │ EncryptVideoAsync(...)
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│               VideoEncryptionService                             │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│ PASO 1: Leer video original en chunks                            │
│ ┌────────────────────────────────────────────────────┐           │
│ │ using FileStream(inputPath, FileMode.Open)         │           │
│ │ Buffer: 81920 bytes (80 KB)                        │           │
│ │ ┌─────────────────────┐                            │           │
│ │ │  Compute SHA-256    │                            │           │
│ │ │  ┌──────────────┐   │                            │           │
│ │ │  │  HashService │   │                            │           │
│ │ │  └──────────────┘   │                            │           │
│ │ └─────────────────────┘                            │           │
│ └────────────────────────────────────────────────────┘           │
│                   │                                              │
│                   ▼                                              │
│         OriginalHash (32 bytes)                                  │
│                                                                  │
│ PASO 2: Generar KEK aleatoria                                    │
│ ┌────────────────────────────────────────────────────┐           │
│ │ KEK = RandomNumberGenerator.GetBytes(32)           │           │
│ └────────────────────────────────────────────────────┘           │
│                   │                                              │
│                   ▼                                              │
│              KEK (256 bits)                                      │
│                                                                  │
│ PASO 3: Cifrar KEK con RSA-OAEP                                  │
│ ┌────────────────────────────────────────────────────┐           │
│ │  KekService.GenerateAndEncryptKek()                │           │
│ │  ┌──────────────────────────────────┐              │           │
│ │  │  RSA-OAEP-SHA256                 │              │           │
│ │  │  PublicKey: server_public_key    │              │           │
│ │  │  Plaintext: KEK (32 bytes)       │              │           │
│ │  │  Output: EncryptedKEK (512 bytes)│              │           │
│ │  └──────────────────────────────────┘              │           │
│ └────────────────────────────────────────────────────┘           │
│                   │                                              │
│                   ▼                                              │
│           EncryptedKEK (512 bytes)                               │
│                                                                  │
│ PASO 4: Cifrar video con ChaCha20-Poly1305                       │
│ ┌────────────────────────────────────────────────────┐           │
│ │  ChaCha20Poly1305Service.Encrypt()                 │           │
│ │  ┌──────────────────────────────────┐              │           │
│ │  │  Key: KEK (32 bytes)             │              │           │
│ │  │  Nonce: Random (12 bytes)        │              │           │
│ │  │  Plaintext: VideoBytes           │              │           │
│ │  │  Output:                         │              │           │
│ │  │    • Ciphertext                  │              │           │
│ │  │    • AuthTag (16 bytes)          │              │           │
│ │  └──────────────────────────────────┘              │           │
│ │  Escritura: FileStream(outputPath, FileMode.Create)│           │
│ └────────────────────────────────────────────────────┘           │
│                   │                                              │
│                   ▼                                              │
│  Nonce (12 bytes) + AuthTag (16 bytes) + EncryptedVideoFile      │
│                                                                  │
│ PASO 5: Calcular HMAC del video cifrado                          │
│ ┌────────────────────────────────────────────────────┐           │
│ │  HmacService.ComputeHmac()                         │           │
│ │  ┌──────────────────────────────────┐              │           │
│ │  │  Algorithm: HMAC-SHA256          │              │           │
│ │  │  Key: Admin's HMAC Key (64 bytes)│              │           │
│ │  │  Data: EncryptedVideoBytes       │              │           │
│ │  │  Output: HMAC (64 bytes)         │              │           │
│ │  └──────────────────────────────────┘              │           │
│ └────────────────────────────────────────────────────┘           │
│                   │                                              │
│                   ▼                                              │
│            HmacOfEncryptedVideo (64 bytes)                       │
│                                                                  │
│ PASO 6: Retornar VideoEncryptionResult                           │
│ ┌────────────────────────────────────────────────────┐           │
│ │  VideoEncryptionResult {                           │           │
│ │    EncryptedKek: 512 bytes                         │           │
│ │    Nonce: 12 bytes                                 │           │
│ │    AuthTag: 16 bytes                               │           │
│ │    OriginalHash: 32 bytes (SHA-256)                │           │
│ │    HmacOfEncryptedVideo: 64 bytes                  │           │
│ │    OriginalSizeBytes: long                         │           │
│ │    EncryptedSizeBytes: long                        │           │
│ │  }                                                 │           │
│ └────────────────────────────────────────────────────┘           │
└──────┬───────────────────────────────────────────────────────────┘
       │
       │ VideoEncryptionResult
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│                      VideoService                                │
├──────────────────────────────────────────────────────────────────┤
│ 5. Eliminar archivo temporal                                     │
│    File.Delete(tempOriginalPath)                                 │
│                                                                  │
│ 6. Crear registro en tabla Videos                                │
│ ┌────────────────────────────────────────────────────┐           │
│ │  Video {                                           │           │
│ │    IdAdministrador: userId                         │           │
│ │    TituloVideo: request.Titulo                     │           │
│ │    NombreArchivoOriginal: videoFile.FileName       │           │
│ │    NombreArchivoCifrado: {guid}.encrypted          │           │
│ │    TamañoArchivo: EncryptedSizeBytes               │           │
│ │    RutaAlmacenamiento: Storage/Videos/{guid}...    │           │
│ │    EstadoProcesamiento: "Disponible"               │           │
│ │    FechaSubida: DateTime.UtcNow                    │           │
│ │  }                                                 │           │
│ └────────────────────────────────────────────────────┘           │
│                                                                  │
│ 7. Crear registro en tabla DatosCriptograficosVideos             │
│ ┌────────────────────────────────────────────────────┐           │
│ │  CryptoData {                                      │           │
│ │    IdVideo: video.IdVideo (FK)                     │           │
│ │    KEKCifrada: EncryptedKek                        │           │
│ │    AlgoritmoKEK: "RSA-OAEP"                        │           │
│ │    Nonce: result.Nonce                             │           │
│ │    AuthTag: result.AuthTag                         │           │
│ │    HashSHA256Original: result.OriginalHash         │           │
│ │    HMACDelVideo: result.HmacOfEncryptedVideo       │           │
│ │    FechaGeneracionClaves: DateTime.UtcNow          │           │
│ │    VersionAlgoritmo: "1.0"                         │           │
│ │  }                                                 │           │
│ └────────────────────────────────────────────────────┘           │
└──────┬───────────────────────────────────────────────────────────┘
       │
       │ SaveChangesAsync()
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│                   SQL Server Database                            │
├──────────────────────────────────────────────────────────────────┤
│ BEGIN TRANSACTION                                                │
│   INSERT INTO Videos (...)                                       │
│   INSERT INTO DatosCriptograficosVideos (...)                    │
│ COMMIT                                                           │
└──────┬───────────────────────────────────────────────────────────┘
       │
       │ VideoResponse
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│                    VideosController                              │
├──────────────────────────────────────────────────────────────────┤
│ Return 200 OK                                                    │
│ {                                                                │
│   "success": true,                                               │
│   "data": {                                                      │
│     "idVideo": 1,                                                │
│     "tituloVideo": "...",                                        │
│     "tamañoArchivo": 15728640,                                   │
│     "message": "Video subido y cifrado exitosamente"             │
│   }                                                              │
│ }                                                                │
└──────┬───────────────────────────────────────────────────────────┘
       │
       ▼
┌─────────────┐
│   Cliente   │
└─────────────┘
```

## Diagrama de Flujo: Verificación de Integridad

```
┌─────────────┐
│   Cliente   │
└──────┬──────┘
       │
       │ POST /api/videos/{id}/verify-integrity
       │ Headers: Authorization: Bearer {JWT}
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│                    VideosController                              │
├──────────────────────────────────────────────────────────────────┤
│ 1. Validar JWT y extraer userId                                  │
│ 2. Verificar rol "Administrador"                                 │
└──────┬───────────────────────────────────────────────────────────┘
       │
       │ VerifyVideoIntegrityAsync(videoId, userId)
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│                      VideoService                                │
├──────────────────────────────────────────────────────────────────┤
│ 1. Query: Videos WHERE IdVideo = {videoId}                       │
│ 2. Verificar: video.IdAdministrador == userId                    │
│ 3. Query: DatosCriptograficosVideos WHERE IdVideo = {videoId}    │
│ 4. Query: ClavesUsuarios WHERE IdUsuario = {userId}              │
└──────┬───────────────────────────────────────────────────────────┘
       │
       │ video, cryptoData, userKeys
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│               VideoEncryptionService                             │
├──────────────────────────────────────────────────────────────────┤
│ VerifyVideoIntegrity(videoPath, expectedHmac, hmacKey)           │
│                                                                  │
│ PASO 1: Leer video cifrado                                       │
│ ┌────────────────────────────────────────────────────┐           │
│ │ FileStream(videoPath, FileMode.Open)               │           │
│ │ Buffer: 81920 bytes                                │           │
│ └────────────────────────────────────────────────────┘           │
│                   │                                              │
│                   ▼                                              │
│ PASO 2: Calcular HMAC del archivo actual                         │
│ ┌────────────────────────────────────────────────────┐           │
│ │  HmacService.ComputeHmac(stream, hmacKey)          │           │
│ │  ┌──────────────────────────────────┐              │           │
│ │  │  Algorithm: HMAC-SHA256          │              │           │
│ │  │  Key: Admin's HMAC Key (64 bytes)│              │           │
│ │  │  Data: EncryptedVideoBytes       │              │           │
│ │  │  Output: ComputedHMAC (64 bytes) │              │           │
│ │  └──────────────────────────────────┘              │           │
│ └────────────────────────────────────────────────────┘           │
│                   │                                              │
│                   ▼                                              │
│           ComputedHmac (64 bytes)                                │
│                                                                  │
│ PASO 3: Comparación timing-safe                                  │
│ ┌────────────────────────────────────────────────────┐           │
│ │  CryptographicOperations.FixedTimeEquals(          │           │
│ │    computedHmac,                                   │           │
│ │    expectedHmac                                    │           │
│ │  )                                                 │           │
│ │  // Previene timing attacks                        │           │
│ └────────────────────────────────────────────────────┘           │
│                   │                                              │
│                   ▼                                              │
│              bool isValid                                        │
└──────┬───────────────────────────────────────────────────────────┘
       │
       │ isValid: true/false
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│                      VideoService                                │
├──────────────────────────────────────────────────────────────────┤
│ Construir VideoIntegrityResponse                                 │
│ {                                                                │
│   idVideo: videoId,                                              │
│   tituloVideo: video.TituloVideo,                                │
│   isValid: isValid,                                              │
│   hashSHA256Original: Base64(cryptoData.HashSHA256Original),     │
│   fechaVerificacion: DateTime.UtcNow,                            │
│   message: isValid ? "Válida" : "ALERTA: Comprometida"           │
│ }                                                                │
└──────┬───────────────────────────────────────────────────────────┘
       │
       ▼
┌─────────────┐
│   Cliente   │
│ (Resultado) │
└─────────────┘
```

## Modelo de Datos Relacional

```
┌─────────────────────────────────────────────────────────────────┐
│                           Usuarios                              │
├─────────────────────────────────────────────────────────────────┤
│ PK  IdUsuario          int                                      │
│     NombreUsuario      nvarchar(100)  [UNIQUE]                  │
│     Email              nvarchar(255)  [UNIQUE]                  │
│     TipoUsuario        nvarchar(50)   [INDEX]                   │
│     PasswordHash       varbinary(256)                           │
│     Salt               varbinary(256)                           │
│     ClavePublicaRSA    nvarchar(max)                            │
│     FechaRegistro      datetime2                                │
│     UltimoAcceso       datetime2                                │
│     Activo             bit                                      │
└──────────────┬──────────────────────────────────────────────────┘
               │
               │ 1:1
               │
               ▼
┌─────────────────────────────────────────────────────────────────┐
│                      ClavesUsuarios                             │
├─────────────────────────────────────────────────────────────────┤
│ PK  IdClaveUsuario            int                               │
│ FK  IdUsuario                 int → Usuarios.IdUsuario          │
│     ClaveHMAC                 varbinary(256)                    │
│     FingerprintClavePublica   varbinary(64)                     │
│     FechaCreacion             datetime2                         │
│     FechaExpiracion           datetime2                         │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                            Videos                               │
├─────────────────────────────────────────────────────────────────┤
│ PK  IdVideo                int                                  │
│ FK  IdAdministrador        int → Usuarios.IdUsuario [CASCADE]   │
│     TituloVideo            nvarchar(200)                        │
│     Descripcion            nvarchar(1000)                       │
│     NombreArchivoOriginal  nvarchar(255)                        │
│     NombreArchivoCifrado   nvarchar(255)                        │
│     TamañoArchivo          bigint                               │
│     Duracion               int (nullable)                       │
│     FormatoVideo           nvarchar(20) (nullable)              │
│     RutaAlmacenamiento     nvarchar(500)                        │
│     EstadoProcesamiento    nvarchar(50)                         │
│     FechaSubida            datetime2                            │
│     FechaModificacion      datetime2 (nullable)                 │
└──────────────┬──────────────────────────────────────────────────┘
               │
               │ 1:1
               │
               ▼
┌─────────────────────────────────────────────────────────────────┐
│               DatosCriptograficosVideos                         │
├─────────────────────────────────────────────────────────────────┤
│ PK  IdDatoCripto            int                                 │
│ FK  IdVideo                 int → Videos.IdVideo [CASCADE]      │
│     KEKCifrada              varbinary(max)    [512 bytes RSA]   │
│     AlgoritmoKEK            nvarchar(50)      ["RSA-OAEP"]      │
│     Nonce                   varbinary(12)     [ChaCha20]        │
│     AuthTag                 varbinary(16)     [Poly1305]        │
│     AAD                     varbinary(max)    (nullable)        │
│     HashSHA256Original      varbinary(32)     [SHA-256]         │
│     HMACDelVideo            varbinary(64)     [HMAC-SHA256]     │
│     FechaGeneracionClaves   datetime2                           │
│     VersionAlgoritmo        nvarchar(20)      ["1.0"]           │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                          Permisos                               │
├─────────────────────────────────────────────────────────────────┤
│ PK  IdPermiso          int                                      │
│ FK  IdVideo            int → Videos.IdVideo [CASCADE]           │
│ FK  IdUsuario          int → Usuarios.IdUsuario [RESTRICT]      │
│     TipoPermiso        nvarchar(50)                             │
│     FechaOtorgamiento  datetime2                                │
│     FechaExpiracion    datetime2 (nullable)                     │
│     FechaRevocacion    datetime2 (nullable)                     │
│     NumeroAccesos      int (nullable)                           │
│     OtorgadoPor        int (nullable)                           │
│     RevocadoPor        int (nullable)                           │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                       RegistroAccesos                           │
├─────────────────────────────────────────────────────────────────┤
│ PK  IdRegistro         bigint                                   │
│ FK  IdUsuario          int → Usuarios.IdUsuario [RESTRICT]      │
│ FK  IdVideo            int → Videos.IdVideo [RESTRICT]          │
│     TipoAcceso         nvarchar(100)                            │
│     Exitoso            bit                                      │
│     MensajeError       nvarchar(500) (nullable)                 │
│     DireccionIP        nvarchar(50) (nullable)                  │
│     UserAgent          nvarchar(500) (nullable)                 │
│     FechaHoraAcceso    datetime2                                │
│     DuracionAcceso     int (nullable)                           │
└─────────────────────────────────────────────────────────────────┘
```

## Pila Tecnológica

### Backend Stack
```
┌────────────────────────────────────────────────────────────┐
│ .NET 8.0 / C# 12                                           │
├────────────────────────────────────────────────────────────┤
│ • ASP.NET Core Web API                                     │
│ • Minimal API Pattern                                      │
│ • Dependency Injection (Built-in)                          │
│ • Configuration System                                     │
└────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────┐
│ Authentication & Authorization                             │
├────────────────────────────────────────────────────────────┤
│ • JWT Bearer Tokens (Microsoft.AspNetCore.Authentication)  │
│ • Role-based Authorization [Authorize(Roles="...")]        │
│ • Claims-based Identity                                    │
└────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────┐
│ Data Access Layer                                          │
├────────────────────────────────────────────────────────────┤
│ • Entity Framework Core 8.0.21                             │
│ • SQL Server Provider                                      │
│ • Code-First Migrations                                    │
│ • Fluent API Configuration                                 │
└────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────┐
│ Cryptography Libraries                                     │
├────────────────────────────────────────────────────────────┤
│ • System.Security.Cryptography (Built-in .NET)             │
│   - ChaCha20Poly1305                                       │
│   - RSA with OAEP padding                                  │
│   - HMACSHA256                                             │
│   - SHA256                                                 │
│   - RandomNumberGenerator                                  │
│   - Rfc2898DeriveBytes (PBKDF2)                            │
│                                                            │
│ • BouncyCastle 2.6.2 (Portable.BouncyCastle)               │
│   - PEM format import/export                               │
│   - Advanced RSA operations                                │
└────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────┐
│ API Documentation                                          │
├────────────────────────────────────────────────────────────┤
│ • Swashbuckle.AspNetCore (Swagger/OpenAPI)                 │
│ • Swagger UI                                               │
└────────────────────────────────────────────────────────────┘
```

### Database
```
┌────────────────────────────────────────────────────────────┐
│ SQL Server Express                                         │
├────────────────────────────────────────────────────────────┤
│ • Instance: localhost\SQLEXPRESS                           │
│ • Database: Data_base_cripto                               │
│ • 6 Tables + __EFMigrationsHistory                         │
└────────────────────────────────────────────────────────────┘
```

### File Storage
```
┌────────────────────────────────────────────────────────────┐
│ Local File System                                          │
├────────────────────────────────────────────────────────────┤
│ Storage/Keys/                                              │
│ ├── server_private_key.pem (RSA-4096)                      │
│ ├── server_public_key.pem                                  │
│ └── .master_secret (HMAC derivation)                       │
│                                                            │
│ Storage/Videos/                                            │
│ └── {guid}.encrypted (ChaCha20-Poly1305 ciphertext)        │
└────────────────────────────────────────────────────────────┘
```

## Patrones de Diseño Implementados

### 1. Repository Pattern (implícito via EF Core)
```csharp
// ApplicationDbContext actúa como Unit of Work
public class VideoService
{
    private readonly ApplicationDbContext _context;
    
    public async Task<Video> GetVideoByIdAsync(int id)
    {
        return await _context.Videos.FindAsync(id);
    }
}
```

### 2. Dependency Injection
```csharp
// ServiceExtensions.cs
services.AddScoped<IVideoService, VideoService>();
services.AddSingleton<IVideoEncryptionService, VideoEncryptionService>();

// VideosController
public VideosController(
    IVideoService videoService,
    ILogger<VideosController> logger)
{
    _videoService = videoService;
    _logger = logger;
}
```

### 3. Service Layer Pattern
```
Controllers (API endpoints)
    ↓
Business Services (VideoService, AuthService, UserService)
    ↓
Cryptography Services (VideoEncryptionService, KeyManagementService)
    ↓
Data Access Layer (ApplicationDbContext)
```

### 4. Strategy Pattern (en servicios criptográficos)
```csharp
public interface IVideoEncryptionService
{
    Task<VideoEncryptionResult> EncryptVideoAsync(...);
}

// Diferentes implementaciones posibles:
// - VideoEncryptionService (ChaCha20-Poly1305)
// - AesVideoEncryptionService (AES-GCM) [futuro]
```

### 5. DTO Pattern
```
Entities (Video, User, CryptoData)
    → separados de →
DTOs (VideoResponse, UploadVideoRequest, VideoIntegrityResponse)
```

### 6. Middleware Pipeline Pattern
```csharp
app.UseAuthentication();  // Valida JWT
app.UseAuthorization();   // Verifica roles/claims
app.MapControllers();     // Enruta a controllers
```

## Flujo de Autenticación JWT

```
┌────────────────────────────────────────────────────────────┐
│ 1. REGISTRO (POST /api/auth/register)                      │
├────────────────────────────────────────────────────────────┤
│ Input: { email, password, nombreUsuario, tipoUsuario }     │
│                                                            │
│ AuthService.RegisterAsync():                               │
│ ┌────────────────────────────────────────────────┐         │
│ │ 1. Validar email único                         │         │
│ │ 2. Generar Salt (32 bytes random)              │         │
│ │ 3. PBKDF2(password, salt, 210k iter) → Hash    │         │
│ │ 4. RSA.GenerateKeyPair(2048) → User keys       │         │
│ │ 5. Guardar Usuario en DB                       │         │
│ │ 6. Si Administrador:                           │         │
│ │    - DeriveHmacKeyForUser() → 64 bytes         │         │
│ │    - Guardar en ClavesUsuarios                 │         │
│ │ 7. GenerateJwtToken(user) → JWT                │         │
│ └────────────────────────────────────────────────┘         │
│                                                            │
│ Output: {                                                  │
│   token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",        │
│   user: { idUsuario, email, tipoUsuario, ... }             │
│ }                                                          │
└────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────┐
│ 2. LOGIN (POST /api/auth/login)                            │
├────────────────────────────────────────────────────────────┤
│ Input: { email, password }                                 │
│                                                            │
│ AuthService.LoginAsync():                                  │
│ ┌────────────────────────────────────────────────┐         │
│ │ 1. Buscar usuario por email                    │         │
│ │ 2. PBKDF2(inputPassword, user.Salt, 210k)      │         │
│ │ 3. FixedTimeEquals(computedHash, user.Hash)    │         │
│ │ 4. Actualizar UltimoAcceso                     │         │
│ │ 5. GenerateJwtToken(user)                      │         │
│ └────────────────────────────────────────────────┘         │
│                                                            │
│ JWT Claims:                                                │
│ {                                                          │
│   "sub": "1",                          // IdUsuario        │
│   "email": "admin@example.com",                            │
│   "name": "admin1",                                        │
│   "role": "Administrador",                                 │
│   "jti": "unique-token-id",                                │
│   "exp": 1700000000,                   // Expiration       │
│   "iss": "SecureVideoStreaming",       // Issuer           │
│   "aud": "SecureVideoStreaming"        // Audience         │
│ }                                                          │
│                                                            │
│ Firmado con: HMAC-SHA256(claims, JWT_SECRET_KEY)           │
└────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────┐
│ 3. REQUEST AUTENTICADO (con JWT en header)                 │
├────────────────────────────────────────────────────────────┤
│ Header: Authorization: Bearer eyJhbGciOiJIUzI1NiIs...      │
│                                                            │
│ JWT Middleware Pipeline:                                   │
│ ┌────────────────────────────────────────────────┐         │
│ │ 1. Extraer token del header Authorization      │         │
│ │ 2. Validar firma HMAC-SHA256                   │         │
│ │ 3. Verificar expiration (exp claim)            │         │
│ │ 4. Verificar issuer y audience                 │         │
│ │ 5. Construir ClaimsPrincipal                   │         │
│ │ 6. Establecer HttpContext.User                 │         │
│ └────────────────────────────────────────────────┘         │
│                                                            │
│ En Controller:                                             │
│ var userId = User.FindFirst(ClaimTypes.NameIdentifier);    │
│ var role = User.FindFirst(ClaimTypes.Role);                │
│                                                            │
│ [Authorize(Roles = "Administrador")] verifica role claim   │
└────────────────────────────────────────────────────────────┘
```

## Seguridad en Profundidad (Defense in Depth)

### Capa 1: Autenticación
```
• PBKDF2-SHA256 (210,000 iteraciones) para passwords
• Salts aleatorios de 32 bytes por usuario
• JWT con expiración configurable
• Validación de firma HMAC-SHA256
```

### Capa 2: Autorización
```
• Role-based access control (Administrador/Usuario)
• Verificación de propiedad (solo dueño modifica sus videos)
• Claims-based identity
```

### Capa 3: Cifrado de Datos
```
• Videos: ChaCha20-Poly1305 (AEAD)
• KEKs: RSA-4096 OAEP
• Nonces únicos por video (12 bytes random)
• KEK única por video (no reutilización)
```

### Capa 4: Integridad
```
• Poly1305 MAC (integrado en ChaCha20-Poly1305)
• HMAC-SHA256 adicional del video cifrado
• SHA-256 del video original
• Comparaciones timing-safe (FixedTimeEquals)
```

### Capa 5: Gestión de Claves
```
• Servidor: RSA-4096 (Storage/Keys/)
• Master secret para derivación HMAC determinística
• Claves HMAC de 64 bytes por administrador
• Fingerprints de claves públicas
```

### Capa 6: Auditoría
```
• Tabla RegistroAccesos (preparada para logging)
• Timestamps en todas las operaciones
• Soft delete para preservar evidencia
```

## Endpoints REST Disponibles

### Authentication Endpoints
```
POST   /api/auth/register
       Body: { nombreUsuario, email, password, tipoUsuario }
       Response: { token, user }

POST   /api/auth/login
       Body: { email, password }
       Response: { token, user }

GET    /api/auth/me
       Headers: Authorization: Bearer {token}
       Response: { user }
```

### User Management Endpoints
```
GET    /api/users
       Auth: Administrador
       Response: [ { users } ]

GET    /api/users/{id}
       Auth: Self or Administrador
       Response: { user }

PUT    /api/users/{id}
       Auth: Self or Administrador
       Body: { nombreUsuario?, email? }
       Response: { user }

DELETE /api/users/{id}
       Auth: Administrador
       Response: { success }
```

### Video Management Endpoints
```
POST   /api/videos/upload
       Auth: Administrador
       Content-Type: multipart/form-data
       Body: { Titulo, Descripcion?, VideoFile }
       Response: { video }

GET    /api/videos/my-videos
       Auth: Administrador
       Response: [ { videos } ]

GET    /api/videos/{id}
       Auth: Any authenticated user
       Response: { video }

POST   /api/videos/{id}/verify-integrity
       Auth: Administrador (owner only)
       Response: { isValid, hashSHA256Original, message }

PUT    /api/videos/{id}/metadata
       Auth: Administrador (owner only)
       Body: { tituloVideo?, descripcion? }
       Response: { video }

DELETE /api/videos/{id}
       Auth: Administrador (owner only)
       Response: { success }

GET    /api/videos/admin/{adminId}
       Auth: Any authenticated user
       Response: [ { videos } ]
```

## Configuración del Sistema

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=Data_base_cripto;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Key": "{strong-secret-key}",
    "Issuer": "SecureVideoStreaming",
    "Audience": "SecureVideoStreaming",
    "ExpirationMinutes": 60
  },
  "Storage": {
    "KeysPath": "./Storage/Keys",
    "VideosPath": "./Storage/Videos"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Tamaños de Datos Criptográficos

| Elemento | Tamaño | Algoritmo |
|----------|--------|-----------|
| KEK | 32 bytes | RandomNumberGenerator |
| KEK Cifrada | 512 bytes | RSA-4096 OAEP |
| Nonce | 12 bytes | ChaCha20 estándar |
| Auth Tag | 16 bytes | Poly1305 |
| Hash Original | 32 bytes | SHA-256 |
| HMAC | 64 bytes | HMAC-SHA256 (512 bits) |
| Password Hash | 32 bytes | PBKDF2-SHA256 |
| Salt | 32 bytes | RandomNumberGenerator |
| HMAC Key (Admin) | 64 bytes | PBKDF2 derivation |

---

**Notas:**
- Arquitectura basada en Clean Architecture / Onion Architecture
- Separación clara de responsabilidades por capas
- Inyección de dependencias en toda la aplicación
- Servicios criptográficos son Singleton (stateless)
- Servicios de negocio son Scoped (DbContext lifecycle)
- Todas las operaciones criptográficas usan APIs del sistema (.NET Cryptography)
- File system usado para videos cifrados (escalable a blob storage)
