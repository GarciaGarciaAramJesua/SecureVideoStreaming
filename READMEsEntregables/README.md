# Entregable Semana 1 - Módulo de Criptografía Base

## Proyecto: Secure Video Streaming

### Fecha: [Tu fecha]
### Alumno: [Tu nombre]

---

## 1. Configuración del Entorno

### Tecnologías Utilizadas
- **Framework**: .NET 8.0
- **Lenguaje**: C# 12
- **Base de Datos**: SQLite (desarrollo) / SQL Server (producción)
- **ORM**: Entity Framework Core 8.0
- **Criptografía**: System.Security.Cryptography + BouncyCastle

### Estructura del Proyecto
```
SecureVideoStreaming/
├── SecureVideoStreaming.API/          # Web API
├── SecureVideoStreaming.Models/       # Entidades y DTOs
├── SecureVideoStreaming.Data/         # Contexto y repositorios
├── SecureVideoStreaming.Services/     # Lógica de negocio y criptografía
└── SecureVideoStreaming.Tests/        # Pruebas unitarias
```

---

## 2. Algoritmos Criptográficos Implementados

### 2.1 ChaCha20-Poly1305
- **Propósito**: Cifrado autenticado de videos (AEAD)
- **Características**:
  - Clave: 256 bits (32 bytes)
  - Nonce: 96 bits (12 bytes)
  - Tag: 128 bits (16 bytes)
- **Implementación**: Nativa de .NET (System.Security.Cryptography)
- **Ventajas**: Mayor rendimiento que AES-GCM en CPUs sin instrucciones AES-NI

### 2.2 RSA-2048/4096 con OAEP
- **Propósito**: Cifrado de claves simétricas y firma digital
- **Características**:
  - Padding: OAEP con SHA-256
  - Firma: SHA256withRSA
  - Formato: PEM
- **Implementación**: BouncyCastle

### 2.3 SHA-256
- **Propósito**: Hash criptográfico para integridad
- **Uso**:
  - Hash de videos originales
  - PBKDF2 para derivación de contraseñas (100,000 iteraciones)

### 2.4 HMAC-SHA256
- **Propósito**: Autenticación de mensajes
- **Uso**: Verificar autoría del dueño del video

### 2.5 KMAC256
- **Propósito**: MAC moderno basado en SHA-3
- **Uso**: Autenticación de metadata

---

## 3. Base de Datos

### Modelo de Datos

#### Tabla: Users
- Id (GUID)
- Username, Email
- UserType (Owner/Consumer)
- PublicKeyRsa
- PasswordHash (PBKDF2)
- Salt, HmacKey

#### Tabla: Videos
- Id (GUID)
- Title, Description
- EncryptedFilePath
- EncryptedKek (KEK cifrada con RSA servidor)
- **Nonce** (ChaCha20)
- AuthTag (Poly1305)
- Hmac, OriginalHash (SHA-256)

#### Tabla: Permissions
- VideoId, ConsumerId
- GrantedAt, ExpiresAt
- IsRevoked

---

## 4. Endpoints de Prueba

### Health Check
`GET /api/health`

### Tests Criptográficos
- `GET /api/cryptotest/test-chacha20`
- `GET /api/cryptotest/test-rsa`
- `GET /api/cryptotest/test-hash`
- `GET /api/cryptotest/test-hmac`
- `GET /api/cryptotest/test-kmac`
- `GET /api/cryptotest/test-all`

---

## 5. Cómo Ejecutar el Proyecto
```bash
# Clonar/Abrir el proyecto
cd SecureVideoStreaming

# Restaurar dependencias
dotnet restore

# Aplicar migraciones
dotnet ef database update --project SecureVideoStreaming.Data --startup-project SecureVideoStreaming.API

# Ejecutar tests
dotnet test

# Ejecutar API
cd SecureVideoStreaming.API
dotnet run

# Abrir Swagger
http://localhost:5140/swagger
```

---

## 6. Pruebas Realizadas

✅ Todos los servicios criptográficos implementados
✅ Tests unitarios pasando
✅ API funcionando correctamente
✅ Base de datos creada y migrada
✅ Swagger operacional

---

## 7. Próximos Pasos (Semana 2)

- Implementar módulo de registro de usuarios
- Sistema de autenticación con JWT
- Gestión de claves RSA por usuario
- Endpoints de usuarios (Owner/Consumer)