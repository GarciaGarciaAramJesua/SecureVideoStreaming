# ✅ Checklist de Tareas Pendientes

## 🎯 Estado Actual del Proyecto

Después de la migración a `Data_base_cripto`, aquí está el estado y las tareas pendientes:

---

## ✅ **Completado**

### **Infraestructura y Base de Datos**
- [x] Base de datos `Data_base_cripto` creada en SQL Server
- [x] Entidades adaptadas al nuevo esquema
- [x] `ApplicationDbContext` configurado completamente
- [x] Conexión a BD verificada y funcional
- [x] Proyecto compila sin errores
- [x] Servicios criptográficos implementados (ChaCha20, RSA, SHA-256, HMAC)

---

## 🔨 **Pendiente - Servicios de Negocio**

### **1. AuthService** ⏳
**Ubicación:** `SecureVideoStreaming.Services/Business/Implementations/AuthService.cs`

**Métodos a implementar:**
- [ ] `RegisterAsync(RegisterUserRequest request)` → Crear usuario con claves RSA
- [ ] `LoginAsync(LoginRequest request)` → Verificar credenciales y generar JWT
- [ ] `ValidateTokenAsync(string token)` → Validar JWT
- [ ] `RefreshTokenAsync(string refreshToken)` → Renovar token
- [ ] `ChangePasswordAsync(int userId, string oldPassword, string newPassword)`
- [ ] `GeneratePasswordResetTokenAsync(string email)`

**Consideraciones:**
- Usar `IHashService.DeriveKey()` para PBKDF2
- Generar claves RSA con `IRsaService.GenerateKeyPair()`
- Implementar JWT según configuración en `appsettings.json`
- Crear registro en `ClavesUsuarios` para administradores

---

### **2. UserService** ⏳
**Ubicación:** `SecureVideoStreaming.Services/Business/Implementations/UserService.cs`

**Métodos a implementar:**
- [ ] `GetUserByIdAsync(int userId)`
- [ ] `GetUserByEmailAsync(string email)`
- [ ] `GetUserByUsernameAsync(string username)`
- [ ] `UpdateUserAsync(int userId, UpdateUserRequest request)`
- [ ] `DeleteUserAsync(int userId)` → Soft delete (Activo = false)
- [ ] `GetUserPublicKeyAsync(int userId)`
- [ ] `UpdatePublicKeyAsync(int userId, string newPublicKey)`
- [ ] `CreateUserKeysAsync(int userId)` → Crear HMAC key para admins
- [ ] `GetAllUsersAsync(int pageNumber, int pageSize)`
- [ ] `SearchUsersAsync(string searchTerm)`

**Consideraciones:**
- Manejar tipos de usuario: "Administrador" y "Usuario"
- Verificar campo `Activo` antes de permitir operaciones
- Actualizar `UltimoAcceso` en cada login

---

### **3. VideoService** ⏳
**Ubicación:** `SecureVideoStreaming.Services/Business/Implementations/VideoService.cs`

**Métodos a implementar:**
- [ ] `UploadVideoAsync(int adminId, UploadVideoRequest request, Stream videoStream)`
  - Generar KEK aleatoria
  - Cifrar video con ChaCha20-Poly1305
  - Calcular SHA-256 del original
  - Calcular HMAC con clave del admin
  - Cifrar KEK con RSA del servidor
  - Guardar archivo cifrado
  - Crear registros en `Videos` y `DatosCriptograficosVideos`

- [ ] `DownloadVideoAsync(int userId, int videoId, string ipAddress, string userAgent)`
  - Verificar permisos
  - Descifrar KEK con RSA
  - Descifrar video con ChaCha20
  - Verificar integridad (SHA-256 y HMAC)
  - Registrar acceso en `RegistroAccesos`
  - Actualizar `NumeroAccesos` en `Permisos`

- [ ] `GetVideoByIdAsync(int videoId)`
- [ ] `GetVideosByAdminAsync(int adminId)`
- [ ] `GetAccessibleVideosAsync(int userId)` → Videos con permiso activo
- [ ] `UpdateVideoMetadataAsync(int videoId, UpdateVideoRequest request)`
- [ ] `DeleteVideoAsync(int videoId)` → Cambiar estado a "Eliminado"
- [ ] `GetVideoDetailsAsync(int videoId, int userId)` → Incluir permisos

**Consideraciones:**
- Validar que `IdAdministrador` tenga `TipoUsuario = "Administrador"`
- Manejar estados: "Procesando", "Disponible", "Error", "Eliminado"
- Streaming de videos grandes (no cargar todo en memoria)
- Limpiar archivos físicos al eliminar

---

### **4. PermissionService** 🆕 (Crear nuevo)
**Ubicación:** `SecureVideoStreaming.Services/Business/Implementations/PermissionService.cs`

**Métodos a implementar:**
- [ ] `GrantPermissionAsync(int videoId, int userId, int grantedBy, GrantPermissionRequest)`
- [ ] `RevokePermissionAsync(int permissionId, int revokedBy)`
- [ ] `GetPermissionsByVideoAsync(int videoId)`
- [ ] `GetPermissionsByUserAsync(int userId)`
- [ ] `CheckPermissionAsync(int videoId, int userId)` → Validar acceso
- [ ] `ExtendPermissionAsync(int permissionId, DateTime newExpiration)`
- [ ] `GetActivePermissionsAsync()` → No revocados y no expirados
- [ ] `GetExpiredPermissionsAsync()` → Para notificaciones

**Consideraciones:**
- Solo el `IdAdministrador` del video puede otorgar/revocar
- Verificar `FechaExpiracion` en cada acceso
- Tipos: "Lectura", "Temporal", "Revocado"
- Índice único en `(IdVideo, IdUsuario)`

---

### **5. AccessLogService** 🆕 (Crear nuevo)
**Ubicación:** `SecureVideoStreaming.Services/Business/Implementations/AccessLogService.cs`

**Métodos a implementar:**
- [ ] `LogAccessAsync(LogAccessRequest request)`
- [ ] `GetAccessLogsByUserAsync(int userId, int pageNumber, int pageSize)`
- [ ] `GetAccessLogsByVideoAsync(int videoId, int pageNumber, int pageSize)`
- [ ] `GetFailedAccessAttemptsAsync(int userId, DateTime since)`
- [ ] `GetAccessStatisticsAsync(int videoId)` → Resumen de accesos
- [ ] `ExportAccessLogsAsync(int videoId, DateTime from, DateTime to)` → Para auditoría

**Consideraciones:**
- Tipos de acceso: "Visualizacion", "Descarga", "SolicitudClave", "Verificacion"
- Registrar siempre IP y User-Agent
- No eliminar logs (cumplimiento normativo)

---

## 🎮 **Pendiente - Controladores API**

### **1. AuthController** ⏳
**Ubicación:** `SecureVideoStreaming.API/Controllers/AuthController.cs`

**Endpoints a implementar:**
```csharp
[POST] /api/auth/register
[POST] /api/auth/login
[POST] /api/auth/refresh
[POST] /api/auth/logout
[POST] /api/auth/change-password
[POST] /api/auth/forgot-password
[GET]  /api/auth/me → Info del usuario autenticado
```

---

### **2. UsersController** ⏳
**Ubicación:** `SecureVideoStreaming.API/Controllers/UsersController.cs`

**Endpoints a implementar:**
```csharp
[GET]    /api/users → Lista paginada
[GET]    /api/users/{id}
[GET]    /api/users/{id}/public-key
[PUT]    /api/users/{id}
[DELETE] /api/users/{id}
[GET]    /api/users/search?q={term}
[POST]   /api/users/{id}/keys → Generar claves HMAC
```

---

### **3. VideosController** ⏳
**Ubicación:** `SecureVideoStreaming.API/Controllers/VideosController.cs`

**Endpoints a implementar:**
```csharp
[POST]   /api/videos/upload
[GET]    /api/videos → Videos del admin o accesibles por el usuario
[GET]    /api/videos/{id}
[GET]    /api/videos/{id}/download → Stream del video descifrado
[PUT]    /api/videos/{id}
[DELETE] /api/videos/{id}
[GET]    /api/videos/admin/{adminId} → Videos de un admin específico
```

---

### **4. PermissionsController** 🆕 (Crear nuevo)
**Ubicación:** `SecureVideoStreaming.API/Controllers/PermissionsController.cs`

**Endpoints a implementar:**
```csharp
[POST]   /api/permissions/grant
[PUT]    /api/permissions/{id}/revoke
[GET]    /api/permissions/video/{videoId}
[GET]    /api/permissions/user/{userId}
[PUT]    /api/permissions/{id}/extend
[GET]    /api/permissions/expired
```

---

### **5. AccessLogsController** 🆕 (Crear nuevo)
**Ubicación:** `SecureVideoStreaming.API/Controllers/AccessLogsController.cs`

**Endpoints a implementar:**
```csharp
[GET] /api/logs/user/{userId}
[GET] /api/logs/video/{videoId}
[GET] /api/logs/video/{videoId}/statistics
[GET] /api/logs/export?videoId={id}&from={date}&to={date}
```

---

## 🧪 **Pendiente - Tests**

### **Tests de Integración**
- [ ] `AuthIntegrationTests.cs` → Registro, login, JWT
- [ ] `VideoUploadIntegrationTests.cs` → Upload y cifrado completo
- [ ] `VideoDownloadIntegrationTests.cs` → Descarga y descifrado
- [ ] `PermissionIntegrationTests.cs` → Otorgar/revocar permisos
- [ ] `AccessLogIntegrationTests.cs` → Auditoría

### **Tests de Performance**
- [ ] Cifrado de videos grandes (>100 MB)
- [ ] Descifrado concurrente (múltiples usuarios)
- [ ] Consultas de auditoría con millones de registros

---

## 🔧 **Configuraciones Adicionales**

### **JWT Configuration**
- [ ] Generar `SecretKey` segura (mínimo 256 bits)
- [ ] Configurar `ExpirationMinutes` apropiado
- [ ] Implementar Refresh Tokens
- [ ] Configurar Claims personalizados (TipoUsuario, etc.)

### **File Storage**
- [ ] Configurar directorio `Storage/Videos` en `appsettings.json`
- [ ] Implementar limpieza de archivos huérfanos
- [ ] Configurar límites de tamaño de archivo
- [ ] Implementar chunked upload para archivos grandes

### **Middleware**
- [ ] `ErrorHandlingMiddleware` → Ya existe, verificar funcionalidad
- [ ] `AuthenticationMiddleware` → JWT validation
- [ ] `RateLimitingMiddleware` → Protección contra abuso
- [ ] `AuditMiddleware` → Logging automático de todas las requests

---

## 📝 **DTOs a Crear**

### **Request DTOs**
- [ ] `UpdateUserRequest.cs`
- [ ] `UpdateVideoRequest.cs`
- [ ] `GrantPermissionRequest.cs`
- [ ] `LogAccessRequest.cs`

### **Response DTOs**
- [ ] `UserResponse.cs` (sin PasswordHash/Salt)
- [ ] `VideoDetailResponse.cs` (con permisos)
- [ ] `PermissionResponse.cs`
- [ ] `AccessLogResponse.cs`
- [ ] `VideoStatisticsResponse.cs`

---

## 🔐 **Seguridad**

### **Implementaciones Críticas**
- [ ] Gestión de clave privada RSA del servidor
  - Almacenar en Azure Key Vault o archivo cifrado
  - Rotación periódica
  
- [ ] Rate Limiting
  - Login: 5 intentos / 15 minutos
  - Upload: 10 videos / hora por admin
  - Download: 100 descargas / hora por usuario

- [ ] Validaciones de Entrada
  - Tamaño máximo de video
  - Formatos de video permitidos
  - Email válido
  - Contraseña fuerte (regex)

- [ ] CORS
  - Configurar orígenes permitidos (no `AllowAnyOrigin` en producción)

---

## 📊 **Documentación**

- [ ] Swagger/OpenAPI
  - Agregar descripciones a endpoints
  - Ejemplos de request/response
  - Códigos de estado HTTP documentados

- [ ] README actualizado
  - Instrucciones de instalación
  - Variables de entorno
  - Ejemplos de uso con `curl` o Postman

- [ ] Diagramas
  - Flujo de upload/download
  - Arquitectura de seguridad
  - Diagrama de base de datos

---

## 🚀 **Deployment**

- [ ] Configuración de producción
  - Connection string segura (variables de entorno)
  - JWT SecretKey desde Key Vault
  - HTTPS obligatorio
  - Logging a servicio externo (Application Insights)

- [ ] CI/CD
  - GitHub Actions o Azure DevOps
  - Tests automáticos
  - Deploy a Azure App Service

---

## 📅 **Priorización Sugerida**

### **Sprint 1 (1-2 semanas)** - Funcionalidad Core
1. AuthService + AuthController
2. UserService + UsersController
3. Tests de autenticación

### **Sprint 2 (1-2 semanas)** - Videos
1. VideoService (upload/download)
2. VideosController
3. Tests de integración de videos

### **Sprint 3 (1 semana)** - Permisos y Auditoría
1. PermissionService + Controller
2. AccessLogService + Controller
3. Tests completos

### **Sprint 4 (1 semana)** - Seguridad y Optimización
1. Rate limiting
2. Performance testing
3. Security audit
4. Documentación

---

## 🎯 **Métricas de Éxito**

- [ ] Todos los tests pasan (>90% cobertura)
- [ ] API responde en <200ms (operaciones simples)
- [ ] Videos >100MB se cifran/descifran correctamente
- [ ] 0 vulnerabilidades críticas (OWASP Top 10)
- [ ] Documentación completa en Swagger

---

**Estado Actual:** 40% Completado  
**Próximo Paso:** Implementar AuthService ✨

