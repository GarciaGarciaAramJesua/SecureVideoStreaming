# 🧪 Guía de Pruebas - Entregable 2

## Permissions, Grid y Key Distribution Modules

---

## 🚀 **Setup Inicial**

1. Asegúrate que el servidor esté corriendo:
```powershell
cd SecureVideoStreaming.API
dotnet run
```

2. Abre Swagger: `http://localhost:5140/swagger`

---

## 📝 **Escenario de Prueba Completo**

### **PASO 1: Registrar Usuarios**

#### 1.1 Registrar Administrador
```json
POST /api/auth/register
{
  "nombreUsuario": "admin_test",
  "email": "admin@test.com",
  "password": "Admin123!",
  "tipoUsuario": "Administrador"
}
```
**Guardar:** `admin_token` y `admin_id`

#### 1.2 Registrar Usuario Normal
```json
POST /api/auth/register
{
  "nombreUsuario": "user_test",
  "email": "user@test.com",
  "password": "User123!",
  "tipoUsuario": "Usuario"
}
```
**Guardar:** `user_token` y `user_id`

---

### **PASO 2: Subir Video (Como Admin)**

```http
POST /api/videos/upload
Authorization: Bearer {admin_token}
Content-Type: multipart/form-data

titulo: Mi Video de Prueba
descripcion: Video para probar el sistema de permisos
videoFile: [seleccionar archivo de video]
```

**Guardar:** `video_id` de la respuesta

---

### **PASO 3: Ver Grid sin Permisos (Como Usuario)**

```http
GET /api/videogrid
Authorization: Bearer {user_token}
```

**Resultado esperado:**
- Lista de videos disponibles
- `TienePermiso: false`
- `EstadoPermiso: "Sin Permiso"`
- `PermiteVisualizacion: false`

---

### **PASO 4: Intentar Solicitar Claves sin Permiso**

```http
GET /api/keydistribution/request/{video_id}
Authorization: Bearer {user_token}
```

**Resultado esperado:**
```json
{
  "success": false,
  "message": "No tiene permiso para acceder a este video"
}
```

---

### **PASO 5: Otorgar Permiso (Como Admin)**

#### 5.1 Permiso Permanente
```json
POST /api/permissions/grant
Authorization: Bearer {admin_token}

{
  "idVideo": {video_id},
  "idUsuario": {user_id},
  "otorgadoPor": {admin_id},
  "tipoPermiso": "Lectura"
}
```

#### 5.2 O Permiso Temporal
```json
POST /api/permissions/grant
Authorization: Bearer {admin_token}

{
  "idVideo": {video_id},
  "idUsuario": {user_id},
  "otorgadoPor": {admin_id},
  "tipoPermiso": "Temporal",
  "fechaExpiracion": "2025-12-31T23:59:59Z"
}
```

**Guardar:** `permission_id` de la respuesta

---

### **PASO 6: Verificar Permiso (Como Usuario)**

```http
GET /api/permissions/check?videoId={video_id}
Authorization: Bearer {user_token}
```

**Resultado esperado:**
```json
{
  "success": true,
  "data": true,
  "message": "Permiso activo"
}
```

---

### **PASO 7: Ver Grid con Permisos (Como Usuario)**

```http
GET /api/videogrid
Authorization: Bearer {user_token}
```

**Resultado esperado:**
- `TienePermiso: true`
- `EstadoPermiso: "Activo"`
- `PermiteVisualizacion: true`
- `IdPermiso: {permission_id}`
- `TipoPermiso: "Lectura"` o `"Temporal"`

---

### **PASO 8: Solicitar Claves (Como Usuario)**

```http
GET /api/keydistribution/request/{video_id}
Authorization: Bearer {user_token}
```

**Resultado esperado:**
```json
{
  "success": true,
  "data": {
    "idVideo": 1,
    "tituloVideo": "Mi Video de Prueba",
    "kekCifradaParaUsuario": "BASE64_STRING...",
    "nonce": "BASE64_STRING...",
    "authTag": "BASE64_STRING...",
    "algoritmoCifrado": "ChaCha20-Poly1305",
    "hashOriginal": "BASE64_STRING...",
    "hmac": "BASE64_STRING...",
    "videoDownloadUrl": "/api/videos/1/download",
    "tamañoArchivo": 1234567,
    "fechaGeneracion": "2025-11-23T...",
    "idPermiso": 1
  }
}
```

---

### **PASO 9: Ver Permisos del Video (Como Admin)**

```http
GET /api/permissions/video/{video_id}
Authorization: Bearer {admin_token}
```

**Resultado esperado:**
- Lista de todos los permisos otorgados para el video
- Información de cada usuario
- Número de accesos
- Fechas de otorgamiento y expiración

---

### **PASO 10: Ver Mis Permisos (Como Usuario)**

```http
GET /api/permissions/my-permissions
Authorization: Bearer {user_token}
```

**Resultado esperado:**
- Lista de videos a los que tengo acceso
- Estado de cada permiso
- Fechas de expiración

---

### **PASO 11: Filtrar Grid (Como Usuario)**

#### 11.1 Buscar por título
```http
GET /api/videogrid/search?searchTerm=prueba
Authorization: Bearer {user_token}
```

#### 11.2 Filtrar por administrador
```http
GET /api/videogrid/search?administrador=admin_test
Authorization: Bearer {user_token}
```

#### 11.3 Solo videos con permiso
```http
GET /api/videogrid/search?soloConPermiso=true
Authorization: Bearer {user_token}
```

---

### **PASO 12: Extender Permiso (Como Admin)**

```json
PUT /api/permissions/{permission_id}/extend
Authorization: Bearer {admin_token}
Content-Type: application/json

"2026-06-30T23:59:59Z"
```

---

### **PASO 13: Revocar Permiso (Como Admin)**

```http
DELETE /api/permissions/{permission_id}
Authorization: Bearer {admin_token}
```

---

### **PASO 14: Verificar Revocación (Como Usuario)**

```http
GET /api/keydistribution/request/{video_id}
Authorization: Bearer {user_token}
```

**Resultado esperado:**
```json
{
  "success": false,
  "message": "No tiene permiso para acceder a este video"
}
```

---

## 🔍 **Casos de Prueba Adicionales**

### **Caso 1: Permiso Expirado**

1. Otorgar permiso temporal con fecha pasada:
```json
{
  "tipoPermiso": "Temporal",
  "fechaExpiracion": "2024-01-01T00:00:00Z"
}
```

2. Intentar solicitar claves:
```http
GET /api/keydistribution/request/{video_id}
```

3. **Resultado:** Error "Permiso expirado"

---

### **Caso 2: Usuario Inactivo**

1. Como admin, desactivar usuario:
```sql
UPDATE Usuarios SET Activo = 0 WHERE IdUsuario = {user_id}
```

2. Intentar solicitar claves:
```http
GET /api/keydistribution/request/{video_id}
```

3. **Resultado:** Error "Usuario no disponible"

---

### **Caso 3: Validar Ownership**

1. Como usuario normal (no admin), intentar otorgar permiso:
```json
POST /api/permissions/grant
Authorization: Bearer {user_token}
```

2. **Resultado:** Error 403 Forbidden

---

### **Caso 4: Doble Permiso**

1. Otorgar permiso a usuario
2. Intentar otorgar otro permiso al mismo usuario para el mismo video
3. **Resultado:** Error "El usuario ya tiene un permiso activo"

---

### **Caso 5: Contador de Accesos**

1. Solicitar claves múltiples veces:
```http
GET /api/keydistribution/request/{video_id}  // 1ra vez
GET /api/keydistribution/request/{video_id}  // 2da vez
GET /api/keydistribution/request/{video_id}  // 3ra vez
```

2. Ver permisos del video:
```http
GET /api/permissions/video/{video_id}
```

3. **Verificar:** `numeroAccesos` debe ser 3

---

## 📊 **Verificación en Base de Datos**

### **Tabla Permisos**
```sql
SELECT * FROM Permisos WHERE IdVideo = {video_id}
```

**Verificar:**
- TipoPermiso
- FechaOtorgamiento
- FechaExpiracion
- FechaRevocacion
- NumeroAccesos
- UltimoAcceso
- OtorgadoPor
- RevocadoPor

### **Tabla RegistroAccesos**
```sql
SELECT * FROM RegistroAccesos 
WHERE IdVideo = {video_id} AND TipoAcceso = 'SolicitudClave'
ORDER BY FechaHoraAcceso DESC
```

**Verificar:**
- Todas las solicitudes de claves están registradas
- Estado Exitoso = true/false
- MensajeError cuando falla

### **Claves del Servidor**
```powershell
ls Storage/Keys/
```

**Verificar:**
- Existe `server_private_key.pem`
- Existe `server_public_key.pem`
- No se regeneran en cada ejecución

---

## 🎯 **Resultados Esperados**

### ✅ **Funcionalidades Validadas:**

1. ✅ Otorgar permisos (Lectura y Temporal)
2. ✅ Revocar permisos
3. ✅ Verificar permisos activos
4. ✅ Grid muestra estado correcto de permisos
5. ✅ Filtros del grid funcionan
6. ✅ Distribución de claves exitosa con permiso
7. ✅ Distribución de claves bloqueada sin permiso
8. ✅ Re-cifrado con clave pública del usuario
9. ✅ Contador de accesos se incrementa
10. ✅ Auditoría en RegistroAccesos
11. ✅ Validación de ownership
12. ✅ Manejo de permisos expirados
13. ✅ Extender fecha de expiración
14. ✅ Claves del servidor persistentes

---

## 🐛 **Troubleshooting**

### **Error: "No tiene clave HMAC configurada"**
- **Causa:** Usuario admin sin entrada en ClavesUsuarios
- **Solución:** Registrar nuevo admin o crear manualmente la entrada

### **Error: "Usuario sin clave pública configurada"**
- **Causa:** Usuario sin ClavePublicaRSA
- **Solución:** Registrar nuevo usuario (genera claves automáticamente)

### **Error: "Error al procesar claves del servidor"**
- **Causa:** Clave privada del servidor no coincide
- **Solución:** Eliminar `Storage/Keys/*.pem` y reiniciar servidor

### **Error 401 Unauthorized**
- **Causa:** Token expirado o inválido
- **Solución:** Hacer login nuevamente

### **Error 403 Forbidden**
- **Causa:** Usuario no tiene el rol requerido
- **Solución:** Usar cuenta de admin para endpoints [Authorize(Roles = "Administrador")]

---

## 📝 **Checklist de Pruebas**

```
Pre-requisitos:
□ Servidor corriendo
□ Base de datos conectada
□ Swagger accesible

Permissions Module:
□ Otorgar permiso Lectura
□ Otorgar permiso Temporal
□ Verificar permiso activo
□ Revocar permiso
□ Extender permiso
□ Listar permisos por video
□ Listar permisos por usuario
□ Intentar otorgar sin ownership
□ Prevenir permisos duplicados
□ Contador de accesos funciona

Grid Module:
□ Ver grid sin permisos
□ Ver grid con permisos
□ Filtrar por búsqueda
□ Filtrar por administrador
□ Filtrar solo con permiso
□ Estados visuales correctos
□ Formato de tamaño correcto
□ Formato de duración correcto

Key Distribution Module:
□ Solicitar claves con permiso
□ Bloquear claves sin permiso
□ Bloquear claves con permiso expirado
□ Validar distribución
□ Incrementar contador
□ Registrar en auditoría
□ Claves servidor persistentes
□ Re-cifrado con RSA usuario

Integración:
□ Flujo completo funciona
□ Auditoría completa
□ Base de datos consistente
```

---

## 🎉 **¡Pruebas Completadas!**

Si todas las pruebas pasan, el sistema está listo para producción de esta fase.
