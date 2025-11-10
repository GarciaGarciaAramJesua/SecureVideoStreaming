# 🚀 Guía Rápida de Prueba - Primer Entregable

## ✅ El servidor está corriendo en: `http://localhost:5140`

---

## 📖 **Acceso a Swagger**

Abre tu navegador en:
```
http://localhost:5140/swagger
```

Verás la documentación interactiva de la API con todos los endpoints disponibles.

---

## 🧪 **Pruebas Paso a Paso**

### **PASO 1: Registrar un Administrador**

1. En Swagger, busca `POST /api/auth/register`
2. Click en "Try it out"
3. Copia y pega este JSON:

```json
{
  "nombreUsuario": "admin1",
  "email": "admin@test.com",
  "password": "Admin123!",
  "tipoUsuario": "Administrador"
}
```

4. Click en "Execute"
5. **Resultado esperado:** Código 200 con un token JWT

---

### **PASO 2: Registrar un Usuario Normal**

1. En el mismo endpoint `POST /api/auth/register`
2. Usar este JSON:

```json
{
  "nombreUsuario": "usuario1",
  "email": "usuario@test.com",
  "password": "User123!",
  "tipoUsuario": "Usuario"
}
```

3. **Resultado esperado:** Código 200 con un token JWT

---

### **PASO 3: Login como Administrador**

1. Busca `POST /api/auth/login`
2. Click en "Try it out"
3. Usa este JSON:

```json
{
  "email": "admin@test.com",
  "password": "Admin123!"
}
```

4. **Copia el TOKEN** de la respuesta (el valor largo que empieza con `eyJ...`)

---

### **PASO 4: Configurar Autenticación**

1. En la parte superior de Swagger, click en el botón **"Authorize"** (🔒)
2. En el campo que aparece, escribe:
   ```
   Bearer TU_TOKEN_AQUI
   ```
   (Reemplaza `TU_TOKEN_AQUI` con el token que copiaste)
3. Click en "Authorize"
4. Click en "Close"

---

### **PASO 5: Ver tu Perfil**

1. Busca `GET /api/auth/me`
2. Click en "Try it out"
3. Click en "Execute"
4. **Resultado esperado:** Información del administrador logueado

---

### **PASO 6: Listar Usuarios (Solo Admin)**

1. Busca `GET /api/users`
2. Click en "Try it out"
3. Click en "Execute"
4. **Resultado esperado:** Array con los 2 usuarios registrados

---

### **PASO 7: Subir un Video (Solo Admin)**

⚠️ **Nota:** Para este paso, necesitas un archivo de video pequeño (puedes usar cualquier archivo .mp4, .avi, etc.)

1. Busca `POST /api/videos/upload`
2. Click en "Try it out"
3. Completa los campos:
   - **titulo:** "Mi Primer Video"
   - **descripcion:** "Video de prueba cifrado"
   - **videoFile:** Click en "Choose File" y selecciona un video
4. Click en "Execute"
5. **Resultado esperado:** 
   - Código 200
   - Mensaje: "Video subido y cifrado exitosamente"
   - El video se cifra automáticamente con ChaCha20-Poly1305

---

### **PASO 8: Ver Grid de Videos**

1. Busca `GET /api/videos`
2. Click en "Try it out"
3. Click en "Execute"
4. **Resultado esperado:** Array con el video que acabas de subir

Verás información como:
```json
[
  {
    "idVideo": 1,
    "tituloVideo": "Mi Primer Video",
    "descripcion": "Video de prueba cifrado",
    "tamañoArchivo": 12345,
    "estadoProcesamiento": "Disponible",
    "fechaSubida": "2025-11-10T...",
    "nombreAdministrador": "admin1"
  }
]
```

---

### **PASO 9: Probar como Usuario Normal**

1. Click nuevamente en "Authorize" (🔒)
2. Click en "Logout"
3. Login con el usuario normal (`POST /api/auth/login`):
   ```json
   {
     "email": "usuario@test.com",
     "password": "User123!"
   }
   ```
4. Copia el nuevo token y autorízate nuevamente
5. Prueba `GET /api/videos` → **Funciona** (puede ver el grid)
6. Prueba `POST /api/videos/upload` → **Error 403 Forbidden** (no es admin)

---

## 🎯 **Diferencias entre Roles**

### **Administrador puede:**
✅ Ver grid de videos  
✅ Subir videos  
✅ Eliminar sus propios videos  
✅ Ver lista de usuarios  
✅ Eliminar usuarios  

### **Usuario puede:**
✅ Ver grid de videos  
❌ Subir videos (Forbidden)  
❌ Eliminar videos (Forbidden)  
❌ Ver lista de usuarios (Forbidden)  
❌ Eliminar usuarios (Forbidden)  

---

## 🔍 **Verificación en Base de Datos**

Puedes verificar que los datos se guardaron correctamente:

### **SQL para ver usuarios:**
```sql
USE Data_base_cripto;
SELECT IdUsuario, NombreUsuario, Email, TipoUsuario, Activo, FechaRegistro 
FROM Usuarios;
```

### **SQL para ver videos:**
```sql
SELECT IdVideo, TituloVideo, Descripcion, EstadoProcesamiento, FechaSubida 
FROM Videos;
```

### **SQL para ver datos criptográficos:**
```sql
SELECT IdDatoCripto, IdVideo, AlgoritmoKEK, 
       LEN(Nonce) as NonceSize, 
       LEN(AuthTag) as AuthTagSize,
       LEN(HashSHA256Original) as HashSize
FROM DatosCriptograficosVideos;
```

Deberías ver:
- Nonce: 12 bytes
- AuthTag: 16 bytes
- Hash: 32 bytes

---

## 📁 **Verificar Archivos Cifrados**

Los videos cifrados se guardan en:
```
SecureVideoStreaming.API/Storage/Videos/
```

Encontrarás archivos con nombres como:
```
a1b2c3d4-e5f6-7890-abcd-ef1234567890.encrypted
```

Si intentas abrir estos archivos, verás que están **completamente cifrados** y no se pueden reproducir sin descifrarlos primero.

---

## 🐛 **Troubleshooting**

### **Error: "401 Unauthorized"**
- Verifica que copiaste bien el token
- Asegúrate de incluir "Bearer " antes del token
- El token expira en 60 minutos, haz login nuevamente

### **Error: "403 Forbidden"**
- Estás intentando una acción que requiere rol de Administrador
- Verifica que estás usando el token del admin, no del usuario

### **Error: "Connection Failed"**
- Verifica que SQL Server está corriendo
- Verifica la cadena de conexión en `appsettings.json`
- Asegúrate de que la BD `Data_base_cripto` existe

### **No puedo subir videos**
- Verifica que el endpoint es `/api/videos/upload` (con /upload)
- Asegúrate de estar autenticado como Administrador
- Verifica que el archivo no sea demasiado grande (límite: 500MB)

---

## 📊 **Resumen de Endpoints Probados**

| Endpoint | Método | Rol | Estado |
|----------|--------|-----|--------|
| `/api/auth/register` | POST | - | ✅ |
| `/api/auth/login` | POST | - | ✅ |
| `/api/auth/me` | GET | Auth | ✅ |
| `/api/users` | GET | Admin | ✅ |
| `/api/users/{id}` | GET | Auth | ✅ |
| `/api/videos` | GET | Auth | ✅ |
| `/api/videos/upload` | POST | Admin | ✅ |
| `/api/videos/{id}` | GET | Auth | ✅ |

---

## 🎉 **¡Listo!**

Has probado exitosamente:
- ✅ Registro de usuarios (Admin y Usuario)
- ✅ Login y JWT
- ✅ Autorización por roles
- ✅ Grid de videos
- ✅ Upload de videos con cifrado automático
- ✅ CRUD de usuarios

**El primer entregable está completo y funcional! 🚀**

---

## 🔄 **Para Detener el Servidor**

En la terminal donde está corriendo, presiona:
```
Ctrl + C
```

---

**¿Necesitas ayuda?** Revisa el archivo `ENTREGABLE_1.md` para más detalles técnicos.
