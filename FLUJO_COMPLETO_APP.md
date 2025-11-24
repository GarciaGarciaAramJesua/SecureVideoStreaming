# 🚀 Guía Paso a Paso - Flujo Completo de la Aplicación

## ✅ Prerequisitos

1. **Base de datos creada**: `Data_base_cripto`
2. **Aplicación corriendo**: http://localhost:5140
3. **Navegador abierto**

---

## 📋 Paso 1: Registrar Nuevo Usuario

### **URL**: http://localhost:5140/Register

### **Datos de ejemplo:**
```
Nombre Usuario: usuario_prueba
Email:          usuario@test.com
Contraseña:     Password123!
Confirmar:      Password123!
Tipo Usuario:   Usuario
```

### **Qué sucede:**
1. ✅ Se valida que el usuario no exista
2. ✅ Se hashea la contraseña con PBKDF2 (100,000 iteraciones)
3. ✅ Se genera un par de claves RSA-2048:
   - Clave Privada → Guardada cifrada en `ClavesUsuarios`
   - Clave Pública → Guardada en `ClavesUsuarios`
4. ✅ Se inserta en tabla `Usuarios`
5. ✅ **Resultado**: Usuario creado con ID único

### **Verificar en base de datos:**
```sql
-- Ver usuario creado
SELECT IdUsuario, NombreUsuario, Email, TipoUsuario, FechaRegistro
FROM Usuarios
WHERE NombreUsuario = 'usuario_prueba';

-- Ver sus claves RSA
SELECT IdUsuario, TieneClavePrivada, LongitudClavePublica = LEN(ClavePublica)
FROM ClavesUsuarios
WHERE IdUsuario = (SELECT IdUsuario FROM Usuarios WHERE NombreUsuario = 'usuario_prueba');
```

---

## 🔐 Paso 2: Iniciar Sesión

### **URL**: http://localhost:5140/Login

### **Datos:**
```
Usuario:    usuario_prueba
Contraseña: Password123!
```

### **Qué sucede:**
1. ✅ Se busca el usuario en la base de datos
2. ✅ Se verifica el hash de la contraseña
3. ✅ Se genera un **JWT Token** con:
   - `UserId`: ID del usuario
   - `Username`: Nombre del usuario
   - `Role`: Tipo de usuario (Usuario/Administrador)
   - `Expiration`: 60 minutos
4. ✅ Se crea sesión en el servidor
5. ✅ **Redirige a**: `/Home`

### **Token JWT generado (ejemplo):**
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiI1IiwidXNlcm5hbWUiOiJ1c3VhcmlvX3BydWViYSIsInJvbGUiOiJVc3VhcmlvIiwiZXhwIjoxNzAwODQ0MDAwfQ.signature
```

**Decodificado:**
```json
{
  "userId": "5",
  "username": "usuario_prueba",
  "role": "Usuario",
  "exp": 1700844000
}
```

---

## 🏠 Paso 3: Página de Inicio

### **URL**: http://localhost:5140/Home

### **Qué verás:**
```
┌─────────────────────────────────────────────┐
│ 🔐 SecureVideoStreaming                     │
│                    👤 usuario_prueba (Usuario)│
├─────────────────────────────────────────────┤
│  🏠 Inicio  │  📼 Galería  │  ☁️ Subir      │
├─────────────────────────────────────────────┤
│                                             │
│  Bienvenido, usuario_prueba                 │
│                                             │
│  📊 Panel de Control                        │
│  ┌─────────────────────────────────────┐   │
│  │ 📼 Ver Galería de Videos           │   │
│  │    Explora videos disponibles      │   │
│  └─────────────────────────────────────┘   │
│                                             │
│  ┌─────────────────────────────────────┐   │
│  │ 🔑 Mis Permisos                    │   │
│  │    Ver videos a los que tienes     │   │
│  │    acceso                           │   │
│  └─────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
```

---

## 📼 Paso 4: Ver Galería de Videos (Grid)

### **URL**: http://localhost:5140/VideoGrid

### **Escenario Inicial**: No tienes permisos aún

```
┌─────────────────────────────────────────────┐
│  📊 Galería de Videos                       │
│  Explora los videos disponibles             │
├─────────────────────────────────────────────┤
│  [🔍 Buscar: _____] [Estado: Todos ▼]      │
│  [👤 Admin:  _____] [🔎 Buscar]            │
├─────────────────────────────────────────────┤
│  📊 10 Total │ ✅ 0 Activos │ 🔒 10 Sin Acc│
├─────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐        │
│  │ 🎬 Video 1   │  │ 🎬 Video 2   │        │
│  │ 🔒 Sin Permiso│  │ 🔒 Sin Permiso│        │
│  │              │  │              │        │
│  │ 👤 admin1    │  │ 👤 admin2    │        │
│  │ 💾 15.2 MB   │  │ 💾 8.5 MB    │        │
│  │ ⏱️ 05:30     │  │ ⏱️ 03:15     │        │
│  │              │  │              │        │
│  │ [🔒 Bloqueado] │  │ [🔒 Bloqueado] │        │
│  │ [ℹ️ Detalles] │  │ [ℹ️ Detalles] │        │
│  └──────────────┘  └──────────────┘        │
└─────────────────────────────────────────────┘
```

**Observaciones:**
- ❌ Todos los videos muestran 🔒 Sin Permiso
- ❌ Botones "Solicitar Claves" y "Ver Video" deshabilitados
- ✅ Puedes ver información básica

---

## 👨‍💼 Paso 5: Administrador Otorga Permiso

**Para que puedas acceder a un video, un ADMINISTRADOR debe otorgarte permiso.**

### **Opción A: Vía Swagger (API)**

1. **Login como Admin:**
```
POST /api/auth/login
{
  "nombreUsuario": "admin1",
  "contraseña": "AdminPass123!"
}
```

2. **Copiar token JWT**

3. **Authorize en Swagger** (botón 🔓)
```
Bearer <token_del_admin>
```

4. **Otorgar Permiso:**
```
POST /api/permissions/grant
{
  "idVideo": 1,
  "idUsuario": 5,         // Tu ID de usuario
  "otorgadoPor": 1,       // ID del admin
  "tipoPermiso": "Lectura",
  "fechaExpiracion": null  // Permanente
}
```

### **Opción B: Directamente en Base de Datos**

```sql
-- Obtener tu ID de usuario
DECLARE @IdUsuario INT = (SELECT IdUsuario FROM Usuarios WHERE NombreUsuario = 'usuario_prueba');
DECLARE @IdAdmin INT = (SELECT IdUsuario FROM Usuarios WHERE TipoUsuario = 'Administrador' AND IdUsuario = 1);
DECLARE @IdVideo INT = 1;

-- Otorgar permiso
INSERT INTO Permisos (IdVideo, IdUsuario, OtorgadoPor, TipoPermiso, FechaOtorgamiento, FechaExpiracion, NumeroAccesos)
VALUES (@IdVideo, @IdUsuario, @IdAdmin, 'Lectura', GETDATE(), NULL, 0);

-- Verificar
SELECT p.IdPermiso, p.TipoPermiso, p.FechaOtorgamiento, p.FechaExpiracion,
       u.NombreUsuario AS Usuario, v.TituloVideo, a.NombreUsuario AS OtorgadoPor
FROM Permisos p
INNER JOIN Usuarios u ON p.IdUsuario = u.IdUsuario
INNER JOIN Videos v ON p.IdVideo = v.IdVideo
INNER JOIN Usuarios a ON p.OtorgadoPor = a.IdUsuario
WHERE p.IdUsuario = @IdUsuario;
```

---

## ✅ Paso 6: Refrescar Grid - Ahora con Permiso

### **Refrescar**: http://localhost:5140/VideoGrid

```
┌─────────────────────────────────────────────┐
│  📊 Galería de Videos                       │
├─────────────────────────────────────────────┤
│  📊 10 Total │ ✅ 1 Activo │ 🔒 9 Sin Acc  │
├─────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐        │
│  │ 🎬 Video 1   │  │ 🎬 Video 2   │        │
│  │ ✅ Activo    │  │ 🔒 Sin Permiso│        │
│  │              │  │              │        │
│  │ 👤 admin1    │  │ 👤 admin2    │        │
│  │ 💾 15.2 MB   │  │ 💾 8.5 MB    │        │
│  │ ⏱️ 05:30     │  │ ⏱️ 03:15     │        │
│  │ 🔐 ChaCha20  │  │ 🔐 ChaCha20  │        │
│  │              │  │              │        │
│  │ ┌──────────┐ │  │              │        │
│  │ │✅ Otorgado│ │  │              │        │
│  │ │24/11/2024 │ │  │              │        │
│  │ │Permanente │ │  │              │        │
│  │ └──────────┘ │  │              │        │
│  │              │  │              │        │
│  │ [🔑 Claves]  │  │ [🔒 Bloqueado] │        │
│  │ [▶️ Ver]     │  │ [ℹ️ Detalles] │        │
│  │ [ℹ️ Detalles]│  │              │        │
│  └──────────────┘  └──────────────┘        │
└─────────────────────────────────────────────┘
```

**¡Cambios!**
- ✅ Video 1 ahora muestra badge verde "Activo"
- ✅ Botones "Solicitar Claves" y "Ver Video" habilitados
- ✅ Muestra info del permiso (fecha otorgado, tipo)

---

## 🔑 Paso 7: Solicitar Claves Criptográficas

### **Clic en**: "🔑 Solicitar Claves" del Video 1

### **Se hace request a:**
```
GET /api/keydistribution/request/1
Authorization: Bearer <tu_token>
```

### **Respuesta JSON descargada:**
```json
{
  "success": true,
  "message": "Claves distribuidas exitosamente",
  "data": {
    "kekCifradaParaUsuario": "bXQ5ZGxWMjBrN3Z...Base64...",
    "nonce": "eHNhY2hhY2hh...Base64...",
    "authTag": "YXV0aHRhZ2V4...Base64...",
    "hashOriginal": "aGFzaG9yaWdp...Base64...",
    "hmac": "aG1hY3ZhbHVl...Base64...",
    "algoritmoCifrado": "ChaCha20-Poly1305",
    "videoDownloadUrl": "/api/videos/download/1",
    "idVideo": 1,
    "tituloVideo": "Tutorial Criptografía",
    "tamañoArchivo": 15728640,
    "duracion": 330
  }
}
```

### **¿Qué pasó internamente?**

1. **Validación de Permiso:**
```sql
SELECT * FROM Permisos
WHERE IdVideo = 1 
  AND IdUsuario = 5 
  AND FechaRevocacion IS NULL
  AND (FechaExpiracion IS NULL OR FechaExpiracion > GETDATE())
```

2. **Obtener KEK cifrada del servidor:**
```sql
SELECT KEKCifrada FROM DatosCriptograficosVideos WHERE IdVideo = 1
```

3. **Descifrar KEK con clave privada del servidor:**
```csharp
byte[] kekPlaintext = RsaService.Decrypt(
    kekCifradaConServidor, 
    serverPrivateKey
);
```

4. **Obtener tu clave pública:**
```sql
SELECT ClavePublica FROM ClavesUsuarios WHERE IdUsuario = 5
```

5. **Re-cifrar KEK con TU clave pública:**
```csharp
byte[] kekCifradaParaTi = RsaService.Encrypt(
    kekPlaintext,
    tuClavePublica
);
```

6. **Registrar acceso:**
```sql
INSERT INTO RegistroAccesos (IdPermiso, IdVideo, IdUsuario, FechaAcceso, Exitoso)
VALUES (@IdPermiso, 1, 5, GETDATE(), 1);

UPDATE Permisos SET NumeroAccesos = NumeroAccesos + 1 WHERE IdPermiso = @IdPermiso;
```

---

## 📥 Paso 8: Descargar Video (Próximo Entregable)

**Actualmente**: Al hacer clic en "▶️ Ver Video" muestra:
```
⚠️ Funcionalidad de reproducción pendiente de implementar en Entregable 3

Por ahora, puedes solicitar las claves desde el botón "Solicitar Claves"
```

**En Entregable 3 implementarás:**
1. Descargar video cifrado: `GET /api/videos/download/1`
2. Descifrar KEK con tu clave privada RSA
3. Descifrar video con ChaCha20-Poly1305 usando la KEK
4. Reproducir en el navegador

---

## 🔍 Verificaciones en Base de Datos

### **1. Ver todos tus permisos:**
```sql
SELECT 
    p.IdPermiso,
    v.TituloVideo,
    p.TipoPermiso,
    p.FechaOtorgamiento,
    p.FechaExpiracion,
    p.NumeroAccesos,
    p.UltimoAcceso,
    CASE 
        WHEN p.FechaRevocacion IS NOT NULL THEN 'Revocado'
        WHEN p.FechaExpiracion IS NOT NULL AND p.FechaExpiracion < GETDATE() THEN 'Expirado'
        ELSE 'Activo'
    END AS Estado
FROM Permisos p
INNER JOIN Videos v ON p.IdVideo = v.IdVideo
WHERE p.IdUsuario = (SELECT IdUsuario FROM Usuarios WHERE NombreUsuario = 'usuario_prueba')
ORDER BY p.FechaOtorgamiento DESC;
```

### **2. Ver tu historial de accesos:**
```sql
SELECT 
    ra.FechaAcceso,
    v.TituloVideo,
    ra.Exitoso,
    ra.MensajeError
FROM RegistroAccesos ra
INNER JOIN Videos v ON ra.IdVideo = v.IdVideo
WHERE ra.IdUsuario = (SELECT IdUsuario FROM Usuarios WHERE NombreUsuario = 'usuario_prueba')
ORDER BY ra.FechaAcceso DESC;
```

### **3. Ver videos disponibles con tu estado:**
```sql
SELECT 
    v.IdVideo,
    v.TituloVideo,
    v.TamañoArchivo / 1024.0 / 1024.0 AS TamañoMB,
    v.Duracion / 60 AS MinutosDuracion,
    u.NombreUsuario AS Administrador,
    CASE 
        WHEN p.IdPermiso IS NOT NULL AND p.FechaRevocacion IS NULL 
             AND (p.FechaExpiracion IS NULL OR p.FechaExpiracion > GETDATE())
        THEN 'Tienes Acceso ✅'
        WHEN p.IdPermiso IS NOT NULL AND p.FechaExpiracion < GETDATE()
        THEN 'Permiso Expirado ⚠️'
        ELSE 'Sin Acceso 🔒'
    END AS EstadoAcceso
FROM Videos v
INNER JOIN Usuarios u ON v.IdAdministrador = u.IdUsuario
LEFT JOIN Permisos p ON v.IdVideo = p.IdVideo 
    AND p.IdUsuario = (SELECT IdUsuario FROM Usuarios WHERE NombreUsuario = 'usuario_prueba')
WHERE v.EstadoProcesamiento = 'Disponible'
ORDER BY v.FechaSubida DESC;
```

---

## 📊 Resumen del Flujo

```
1. REGISTRO
   ├─ Crear usuario
   ├─ Generar claves RSA
   └─ Hash contraseña
   
2. LOGIN
   ├─ Validar credenciales
   ├─ Generar JWT token
   └─ Crear sesión
   
3. HOME
   └─ Ver opciones disponibles
   
4. GRID (sin permisos)
   ├─ Ver todos los videos
   └─ Estado: 🔒 Sin Permiso
   
5. ADMIN OTORGA PERMISO
   ├─ INSERT en tabla Permisos
   └─ Asocia usuario + video
   
6. GRID (con permisos)
   ├─ Ver videos con ✅ Activo
   └─ Botones habilitados
   
7. SOLICITAR CLAVES
   ├─ Validar permiso
   ├─ Descifrar KEK (servidor)
   ├─ Re-cifrar KEK (usuario)
   ├─ Registrar acceso
   └─ Retornar claves + metadata
   
8. DESCARGAR/VER VIDEO (Entregable 3)
   ├─ Descargar video cifrado
   ├─ Descifrar con claves
   └─ Reproducir
```

---

## ✅ Checklist de Prueba

```
☐ 1. Registrar usuario nuevo
☐ 2. Verificar usuario en BD (tabla Usuarios)
☐ 3. Verificar claves RSA (tabla ClavesUsuarios)
☐ 4. Iniciar sesión
☐ 5. Verificar redirección a /Home
☐ 6. Ir a /VideoGrid
☐ 7. Verificar que videos muestran 🔒 Sin Permiso
☐ 8. Admin otorga permiso (Swagger o SQL)
☐ 9. Refrescar /VideoGrid
☐ 10. Verificar video con ✅ Activo
☐ 11. Clic en "Solicitar Claves"
☐ 12. Verificar JSON descargado
☐ 13. Verificar registro en RegistroAccesos
☐ 14. Verificar NumeroAccesos incrementado
```

---

## 🚨 Posibles Problemas

### **❌ No puedo registrarme**
- Verifica que la BD esté creada
- Verifica conexión en `appsettings.json`

### **❌ Login falla**
- Usuario/contraseña incorrectos
- Usuario no existe en BD

### **❌ No veo videos en el grid**
- No hay videos subidos
- Verifica: `SELECT * FROM Videos WHERE EstadoProcesamiento = 'Disponible'`

### **❌ Todos muestran "Sin Permiso"**
- Normal, necesitas que admin te otorgue permiso
- Usa Swagger o SQL para otorgar

### **❌ No puedo solicitar claves**
- No tienes permiso activo
- El permiso expiró
- El permiso fue revocado

---

¿Quieres que te ayude a crear usuarios de prueba o a otorgar permisos? 🚀
