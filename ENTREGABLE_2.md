# 🎉 ENTREGABLE 2 - Permissions, Grid y Key Distribution

## ✅ Estado: IMPLEMENTADO Y FUNCIONAL

**Fecha de entrega:** 23 de Noviembre de 2025

---

## 📋 **Módulos Implementados**

### **1. Permissions Module (Módulo de Permisos)** 🔐

Sistema completo de gestión de permisos para controlar el acceso a videos.

#### **Servicios Implementados:**
- ✅ `IPermissionService` - Interfaz del servicio
- ✅ `PermissionService` - Implementación completa

#### **Funcionalidades:**

##### **Otorgar Permisos** (`GrantPermissionAsync`)
- Verificación de video disponible
- Validación de ownership (solo el admin dueño puede otorgar)
- Validación de usuario receptor (no puede ser admin)
- Soporte para 2 tipos de permisos:
  - **"Lectura"**: Permanente sin expiración
  - **"Temporal"**: Con fecha de expiración
- Prevención de permisos duplicados
- Revocación automática de permisos expirados

##### **Revocar Permisos** (`RevokePermissionAsync`)
- Solo el admin dueño puede revocar
- Marca fecha de revocación
- Cambia tipo a "Revocado"
- Previene revocación duplicada

##### **Verificar Permisos** (`CheckPermissionAsync`)
- Verifica existencia de permiso
- Valida que no esté revocado
- Verifica que no esté expirado
- Retorna booleano simple

##### **Listar Permisos**
- `GetPermissionsByVideoAsync`: Todos los permisos de un video (solo admin dueño)
- `GetPermissionsByUserAsync`: Permisos activos del usuario

##### **Extender Permisos** (`ExtendPermissionAsync`)
- Cambiar fecha de expiración
- Solo el admin dueño
- Validaciones de fecha futura

##### **Contador de Accesos** (`IncrementAccessCountAsync`)
- Incrementa `NumeroAccesos`
- Actualiza `UltimoAcceso`

#### **DTOs Creados:**
```
Request:
- GrantPermissionRequest (IdVideo, IdUsuario, OtorgadoPor, TipoPermiso, FechaExpiracion)

Response:
- PermissionResponse (Completo con toda la info del permiso)
```

#### **API Endpoints:**
```
POST   /api/permissions/grant              - Otorgar permiso [Admin]
DELETE /api/permissions/{id}               - Revocar permiso [Admin]
GET    /api/permissions/check?videoId=X    - Verificar permiso [User]
GET    /api/permissions/video/{videoId}    - Listar permisos de video [Admin]
GET    /api/permissions/my-permissions     - Mis permisos [User]
PUT    /api/permissions/{id}/extend        - Extender permiso [Admin]
GET    /api/permissions/{id}               - Detalles de permiso [User]
```

---

### **2. Grid Module (Módulo de Grid de Videos)** 📊

Vista de catálogo de videos para usuarios con información de permisos integrada.

#### **Servicios Implementados:**
- ✅ `IVideoGridService` - Interfaz del servicio
- ✅ `VideoGridService` - Implementación completa

#### **Funcionalidades:**

##### **Grid Principal** (`GetVideoGridForUserAsync`)
- Lista todos los videos disponibles
- Incluye información de permisos del usuario
- Estados visuales: "Activo", "Expirado", "Sin Permiso"
- Indica si permite visualización
- Ordena por fecha de subida (descendente)

##### **Grid con Filtros** (`GetVideoGridWithFiltersAsync`)
Filtros disponibles:
- **searchTerm**: Búsqueda en título y descripción
- **administrador**: Filtrar por nombre del admin
- **soloConPermiso**: Solo videos con permiso activo

##### **Item Individual** (`GetVideoGridItemAsync`)
- Detalles de un video específico
- Con información de permisos del usuario

#### **Información en el Grid:**
```csharp
- IdVideo, TituloVideo, Descripcion
- TamañoArchivo (bytes + formateado: "1.5 MB")
- Duracion (segundos + formateada: "05:30")
- FormatoVideo, FechaSubida
- NombreAdministrador
- TienePermiso (bool)
- TipoPermiso, FechaExpiracion
- NumeroAccesos, UltimoAcceso
- PermiteVisualizacion (bool)
- EstadoPermiso (string)
```

#### **API Endpoints:**
```
GET /api/videogrid                                           - Grid completo [User]
GET /api/videogrid/search?searchTerm=X&administrador=Y       - Grid con filtros [User]
GET /api/videogrid/{videoId}                                 - Item individual [User]
```

---

### **3. Key Distribution Module (Distribución de Claves)** 🔑

Sistema de distribución segura de claves criptográficas usando RSA.

#### **Servicios Implementados:**
- ✅ `IKeyDistributionService` - Interfaz del servicio
- ✅ `KeyDistributionService` - Implementación completa

#### **Funcionalidades:**

##### **Distribución de Claves** (`DistributeKeysAsync`)

**Flujo de seguridad:**
```
1. Validar permiso activo del usuario
2. Obtener video y datos criptográficos
3. Obtener clave pública RSA del usuario
4. Descifrar KEK con clave PRIVADA del servidor
5. Re-cifrar KEK con clave PÚBLICA del usuario
6. Incrementar contador de accesos
7. Retornar todo el paquete de claves
8. Registrar en log de auditoría
```

**Datos distribuidos:**
- **KEKCifradaParaUsuario**: KEK cifrada con RSA del usuario (Base64)
- **Nonce**: Nonce de ChaCha20 (Base64)
- **AuthTag**: Tag Poly1305 (Base64)
- **HashOriginal**: SHA-256 del video original (Base64)
- **HMAC**: HMAC del video cifrado (Base64)
- **AlgoritmoCifrado**: "ChaCha20-Poly1305"
- **VideoDownloadUrl**: URL para descargar
- **TamañoArchivo**: Tamaño del video cifrado
- **FechaGeneracion**: Timestamp

##### **Validación** (`ValidateKeyDistributionAsync`)
- Video existe y está disponible
- Permiso activo
- Usuario activo

##### **Auditoría** (`LogKeyDistributionAsync`)
- Registra en `RegistroAccesos`
- TipoAcceso: "SolicitudClave"
- Guarda éxito/fallo
- Mensaje de error si falla

#### **Gestión de Claves del Servidor:**

##### **Persistencia de Claves RSA:**
```
Storage/Keys/
├── server_private_key.pem  (Clave privada RSA-2048)
└── server_public_key.pem   (Clave pública RSA-2048)
```

- Se genera **una sola vez** al inicio
- Se reutiliza para todos los videos
- Permite descifrar KEKs en el futuro
- **Soluciona el problema crítico** de la versión anterior

#### **API Endpoints:**
```
GET /api/keydistribution/request/{videoId}   - Solicitar claves [User con permiso]
GET /api/keydistribution/validate/{videoId}  - Validar acceso [User]
```

---

## 🔄 **Cambios en Módulos Existentes**

### **VideoService - ACTUALIZADO** ⚡

#### **Problema Solucionado:**
**ANTES:** 
```csharp
var (serverPublicKey, _) = _rsaService.GenerateKeyPair(2048); // ⚠️ Nueva cada vez!
var encryptedKek = _rsaService.Encrypt(kek, serverPublicKey);
```
**Resultado:** Videos no recuperables porque no se guardaba la clave privada.

**AHORA:**
```csharp
var serverPublicKey = await GetOrCreateServerPublicKeyAsync(); // ✅ Persistente!
var encryptedKek = _rsaService.Encrypt(kek, serverPublicKey);
```
**Resultado:** Claves persistentes en disco, videos siempre recuperables.

#### **Método Agregado:**
```csharp
private async Task<string> GetOrCreateServerPublicKeyAsync()
{
    // Si existe, la lee del disco
    // Si no existe, genera el par y lo guarda
    return publicKey;
}
```

---

## 🗄️ **Modelo de Datos Utilizado**

### **Permisos (Permissions)**
```sql
IdPermiso (PK)
IdVideo (FK)
IdUsuario (FK)
TipoPermiso ('Lectura', 'Temporal', 'Revocado')
FechaOtorgamiento
FechaExpiracion (nullable)
FechaRevocacion (nullable)
NumeroAccesos (int)
UltimoAcceso (nullable)
OtorgadoPor (FK → Usuarios)
RevocadoPor (FK → Usuarios, nullable)
```

### **RegistroAccesos (AccessLog)** - Usado para auditoría
```sql
IdRegistro (PK)
IdVideo (FK)
IdUsuario (FK)
TipoAcceso ('SolicitudClave', 'Visualizacion', 'Descarga')
Exitoso (bool)
MensajeError (nullable)
DireccionIP, UserAgent
FechaHoraAcceso
DuracionAcceso (nullable)
```

---

## 🧪 **Casos de Uso Implementados**

### **Caso 1: Administrador otorga permiso**
```
1. Admin sube video
2. Admin otorga permiso a usuario
   POST /api/permissions/grant
   {
     "idVideo": 1,
     "idUsuario": 5,
     "otorgadoPor": 2,
     "tipoPermiso": "Temporal",
     "fechaExpiracion": "2025-12-31"
   }
3. Sistema verifica ownership
4. Permiso creado exitosamente
```

### **Caso 2: Usuario consulta grid**
```
1. Usuario hace login
2. GET /api/videogrid
3. Sistema muestra:
   - Videos con permiso: ✅ "Activo" (botón de ver)
   - Videos sin permiso: ⛔ "Sin Permiso" (botón deshabilitado)
   - Videos expirados: ⏰ "Expirado" (solicitar renovación)
```

### **Caso 3: Usuario solicita claves**
```
1. Usuario con permiso activo
2. GET /api/keydistribution/request/1
3. Sistema:
   a. Valida permiso
   b. Descifra KEK con clave privada servidor
   c. Re-cifra KEK con clave pública usuario
   d. Retorna paquete completo de claves
   e. Incrementa contador de accesos
   f. Registra en log
4. Usuario recibe:
   - KEK cifrada para él
   - Nonce, AuthTag
   - Hash original, HMAC
   - URL de descarga
```

### **Caso 4: Admin revoca permiso**
```
1. Admin ve lista de permisos de su video
   GET /api/permissions/video/1
2. Admin revoca permiso
   DELETE /api/permissions/5
3. Usuario ya no puede solicitar claves
4. Solicitudes futuras retornan error
```

---

## 🔐 **Seguridad Implementada**

### **1. Criptografía Híbrida**
```
Video Original
    ↓ (ChaCha20-Poly1305 con KEK)
Video Cifrado + AuthTag
    ↓ (KEK cifrada con RSA servidor)
KEK almacenada en BD
    ↓ (Solicitud de usuario con permiso)
KEK descifrada con RSA servidor
    ↓ (Re-cifrada con RSA usuario)
KEK entregada al usuario
    ↓ (Usuario descifra con su clave privada)
Usuario obtiene KEK
    ↓ (Descifra video con ChaCha20)
Video Original
```

### **2. Control de Acceso**
- ✅ Permisos granulares por video
- ✅ Validación de expiración
- ✅ Revocación instantánea
- ✅ Ownership verificado
- ✅ Auditoría completa

### **3. Integridad**
- ✅ SHA-256 del original
- ✅ HMAC del cifrado
- ✅ AuthTag de Poly1305
- ✅ Triple verificación

---

## 📊 **Estadísticas del Proyecto**

```
Módulos Implementados: 10/10 (100%)
├── DB Design             ✅
├── Users Sign Up         ✅
├── Authentication        ✅
├── Key Management        ✅ (Mejorado)
├── Videos Upload         ✅
├── Videos Encryption     ✅
├── Owner Management      ✅
├── Permissions           ✅ (NUEVO)
├── Grid                  ✅ (NUEVO)
└── Key Distribution      ✅ (NUEVO)

Archivos Creados en esta entrega: 11
- IPermissionService.cs
- PermissionService.cs
- PermissionsController.cs
- GrantPermissionRequest.cs
- PermissionResponse.cs
- IVideoGridService.cs
- VideoGridService.cs
- VideoGridController.cs
- VideoGridItemResponse.cs
- IKeyDistributionService.cs
- KeyDistributionService.cs
- KeyDistributionController.cs
- KeyDistributionResponse.cs

Archivos Modificados: 2
- VideoService.cs (Fix clave RSA servidor)
- Program.cs (Registro de servicios)

Total de Endpoints API: 25+
Total de Servicios: 6
Total de DTOs: 12+
```

---

## 🚀 **Cómo Probar**

### **1. Otorgar Permiso (Swagger)**
```http
POST /api/permissions/grant
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "idVideo": 1,
  "idUsuario": 3,
  "otorgadoPor": 2,
  "tipoPermiso": "Temporal",
  "fechaExpiracion": "2025-12-31T23:59:59Z"
}
```

### **2. Ver Grid (Swagger)**
```http
GET /api/videogrid
Authorization: Bearer {user_token}
```

### **3. Solicitar Claves (Swagger)**
```http
GET /api/keydistribution/request/1
Authorization: Bearer {user_token}
```

### **4. Verificar Permiso (Swagger)**
```http
GET /api/permissions/check?videoId=1
Authorization: Bearer {user_token}
```

---

## ✅ **Checklist de Funcionalidades**

### **Permissions Module**
- ✅ Otorgar permisos (Lectura y Temporal)
- ✅ Revocar permisos
- ✅ Verificar permisos activos
- ✅ Listar permisos por video
- ✅ Listar permisos por usuario
- ✅ Extender fecha de expiración
- ✅ Contador de accesos
- ✅ Validación de ownership
- ✅ Prevención de duplicados
- ✅ Manejo de expiración

### **Grid Module**
- ✅ Grid completo con permisos
- ✅ Filtro por búsqueda
- ✅ Filtro por administrador
- ✅ Filtro solo con permiso
- ✅ Estados visuales claros
- ✅ Formato de tamaño y duración
- ✅ Información completa de permisos
- ✅ Item individual detallado

### **Key Distribution Module**
- ✅ Distribución segura de claves
- ✅ Re-cifrado con RSA del usuario
- ✅ Validación de permisos
- ✅ Auditoría de solicitudes
- ✅ Persistencia de claves del servidor
- ✅ Gestión automática de claves
- ✅ Incremento de contador de accesos
- ✅ Logs de auditoría

---

## 🎯 **Próximos Pasos (Futuro)**

1. ⏭️ **Download/Streaming Module** - Descarga y descifrado de videos
2. ⏭️ **Frontend completo** - UI para grid y permisos
3. ⏭️ **Notificaciones** - Alertas de permisos expirados
4. ⏭️ **Dashboard de Analytics** - Estadísticas de acceso
5. ⏭️ **Export de logs** - Reportes de auditoría

---

## 📝 **Notas Técnicas**

### **Decisiones de Diseño:**

1. **RSA para distribución de claves**: Permite cifrado asimétrico seguro
2. **Re-cifrado en el servidor**: Usuario nunca ve KEK en claro en el servidor
3. **Permisos granulares**: Control fino por video y usuario
4. **Auditoría completa**: Trazabilidad de todas las operaciones
5. **Claves persistentes**: Soluciona el problema crítico de recuperabilidad

### **Consideraciones de Seguridad:**

1. **Clave privada del servidor**: Debe protegerse con filesystem permissions
2. **Claves privadas de usuarios**: Nunca se envían al servidor
3. **HTTPS obligatorio**: En producción para proteger tokens y claves
4. **Rate limiting**: Considerar para solicitudes de claves
5. **Backup de claves**: Implementar estrategia de backup para `server_private_key.pem`

---

## 🎉 **Conclusión**

**Entregable completado al 100%**. El sistema ahora tiene:
- ✅ Gestión completa de permisos
- ✅ Grid interactivo para usuarios
- ✅ Distribución segura de claves
- ✅ Persistencia de claves del servidor (problema crítico solucionado)

El proyecto está listo para la siguiente fase: **descarga y reproducción de videos**.
