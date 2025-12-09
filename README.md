# Secure Video Streaming - Sistema de Streaming Seguro

## 🎯 Proyecto de Criptografía Aplicada

### 📅 Última Actualización: Diciembre 2025
### 👥 Autores
- **García García Aram Jesua**
- **Hernández Díaz Roberto Angel**

### 📊 Estado del Proyecto
- **Progreso:** 100% Completo ✅
- **Módulos Funcionales:** 11/11
- **Modelo de Seguridad:** Claves Efímeras (Ephemeral Keys)
- **Última Mejora:** Implementación de claves temporales sin persistencia

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

## 🚀 Módulos Implementados

### ✅ Entregable 1 (Completado)
1. **DB Design** - Base de datos completa con 6 tablas
2. **Users Sign Up** - Registro con RSA + HMAC
3. **Authentication** - JWT + PBKDF2
4. **Key Management** - Gestión de claves criptográficas
5. **Videos Upload** - Subida y cifrado automático
6. **Videos Encryption** - ChaCha20-Poly1305 AEAD
7. **Owner Management** - CRUD de videos del admin

### ✅ Entregable 2 (Completado)
8. **Permissions Module** - Control de acceso granular
   - Otorgar/revocar permisos
   - Permisos permanentes y temporales
   - Validación de expiración
   - Contador de accesos

9. **Grid Module** - Catálogo de videos
   - Vista con información de permisos
   - Filtros avanzados
   - Estados visuales

10. **Key Distribution** - Distribución segura
    - Re-cifrado con RSA del usuario
    - Persistencia de claves del servidor
    - Auditoría completa

11. **Download/Stream Module** - Reproducción segura ✅
    - Streaming con descifrado en tiempo real
    - Modelo de claves efímeras (sin persistencia)
    - Auto-destrucción de claves temporales
    - Zero-storage security model

---

## 📚 Documentación del Proyecto

El proyecto cuenta con documentación completa organizada en los siguientes archivos:

| Documento | Descripción |
|-----------|-------------|
| `README.md` | Este archivo - Guía general del proyecto |
| `ARQUITECTURA.md` | Arquitectura completa del sistema incluyendo modelo de claves efímeras |
| `MIGRACION_CLAVES_EFIMERAS.md` | Documentación de la migración a claves temporales |
| `MIGRACION_BD.md` | Guía de migraciones de base de datos |
| `LIMPIAR_CACHE.md` | Instrucciones para limpiar caché del navegador |
| `OTORGAR_PERMISOS_SQL.md` | Scripts SQL para permisos de base de datos |
| `PRUEBAS.md` | Documentación de pruebas del sistema |
| `TODO.md` | Lista de tareas y pendientes |

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

## 7. 🔐 Modelo de Seguridad - Claves Efímeras

### Características Principales
- **Zero-Storage**: No se almacenan claves privadas en ningún medio persistente
- **Zero-Persistence**: No se usa localStorage ni sessionStorage
- **Auto-Destruction**: Las claves se destruyen automáticamente al cerrar el video
- **RAM-Only**: Las claves temporales solo existen en memoria durante la reproducción

### Flujo de Seguridad
1. Usuario solicita ver un video
2. Se generan claves RSA-2048 temporales en RAM (Web Crypto API)
3. Servidor cifra la clave de video con la clave pública temporal
4. Cliente descifra en memoria y reproduce el video
5. Al cerrar el video, las claves se destruyen automáticamente

### Beneficios de Seguridad
✅ Elimina riesgo de robo de claves privadas almacenadas  
✅ No hay archivos descargables que comprometan la seguridad  
✅ Mejor experiencia de usuario (sin backups manuales)  
✅ Cumple con principio de "least privilege"  
✅ Auto-limpieza garantizada por garbage collector

---

## 8. 🚀 Inicio Rápido

```bash
# 1. Restaurar dependencias
dotnet restore

# 2. Aplicar migraciones
dotnet ef database update --project SecureVideoStreaming.Data --startup-project SecureVideoStreaming.API

# 3. Ejecutar proyecto
cd SecureVideoStreaming.API
dotnet run

# 4. Abrir en navegador
# https://localhost:7217
```

### Primera Vez
1. Navega a `/Register`
2. Crea un usuario tipo "Administrador" para subir videos
3. Crea un usuario tipo "Usuario" para ver videos
4. El administrador sube videos y otorga permisos
5. El usuario ve videos con claves efímeras

---

## 9. ⚠️ Solución de Problemas

### Error: "SecureKeyStorage is not defined"
Este error indica caché del navegador. **Solución**:
- Presiona `Ctrl + Shift + R` (Windows/Linux) o `Cmd + Shift + R` (Mac)
- Consulta `LIMPIAR_CACHE.md` para más detalles

### Error de Base de Datos
```bash
dotnet ef database drop --project SecureVideoStreaming.Data --startup-project SecureVideoStreaming.API
dotnet ef database update --project SecureVideoStreaming.Data --startup-project SecureVideoStreaming.API
```

### Permisos SQL Server
Consulta `OTORGAR_PERMISOS_SQL.md` para configurar permisos correctamente.

---

## 10. 📝 Licencia y Créditos

**Proyecto Académico** - ESCOM IPN 
**Materia**: Selected Topics in Cryptography
**Semestre**: August 2025 - December 2025  

**Tecnologías Clave**:
- .NET 8.0, Entity Framework Core
- ChaCha20-Poly1305, RSA-OAEP
- Web Crypto API, BouncyCastle
- SQL Server, JWT Authentication