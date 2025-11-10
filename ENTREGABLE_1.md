# 🎉 PRIMER ENTREGABLE - CRUD DE USUARIOS COMPLETO

## ✅ Estado: IMPLEMENTADO Y FUNCIONAL

---

## 📋 **Funcionalidades Implementadas**

### **1. Sistema de Autenticación** 🔐
- ✅ **Registro de usuarios** (`POST /api/auth/register`)
  - Validaciones de campos
  - Hash de contraseñas con PBKDF2 (100,000 iteraciones)
  - Generación automática de claves RSA
  - Generación de clave HMAC para administradores
  - Tipos: "Administrador" y "Usuario"

- ✅ **Login** (`POST /api/auth/login`)
  - Verificación de credenciales
  - Generación de JWT token
  - Actualización de último acceso
  - Control de usuarios activos

- ✅ **Perfil actual** (`GET /api/auth/me`)
  - Obtener información del usuario autenticado
  - Protegido con JWT

### **2. CRUD de Usuarios** 👥
- ✅ **Listar usuarios** (`GET /api/users`) - Solo Administradores
- ✅ **Ver usuario** (`GET /api/users/{id}`) - Propio perfil o Admin
- ✅ **Actualizar usuario** (`PUT /api/users/{id}`) - Propio perfil o Admin
- ✅ **Eliminar usuario** (`DELETE /api/users/{id}`) - Solo Administradores

### **3. Sistema de Videos** 🎬
- ✅ **Subir video** (`POST /api/videos/upload`) - Solo Administradores
  - Cifrado automático con ChaCha20-Poly1305
  - Cálculo de HMAC y hash SHA-256
  - Almacenamiento seguro

- ✅ **Listar todos los videos** (`GET /api/videos`)
- ✅ **Listar videos por admin** (`GET /api/videos/admin/{adminId}`)
- ✅ **Ver detalle de video** (`GET /api/videos/{id}`)
- ✅ **Eliminar video** (`DELETE /api/videos/{id}`) - Solo el admin dueño

---

## 🏗️ **Arquitectura Implementada**

### **DTOs (Data Transfer Objects)**
```
Request:
├── RegisterUserRequest.cs ✅ (con validaciones)
├── LoginRequest.cs ✅ (con validaciones)
└── UpdateUserRequest.cs ✅

Response:
├── AuthResponse.cs ✅
├── UserResponse.cs ✅
├── VideoResponse.cs ✅
└── VideoListResponse.cs ✅
```

### **Servicios de Negocio**
```
Services:
├── AuthService.cs ✅ (Registro, Login, JWT)
├── UserService.cs ✅ (CRUD completo)
└── VideoService.cs ✅ (Upload con cifrado, Listar, Eliminar)
```

### **Controladores API**
```
Controllers:
├── AuthController.cs ✅ (Register, Login, Me)
├── UsersController.cs ✅ (CRUD con autorización)
└── VideosController.cs ✅ (Upload, Grid, Delete)
```

---

## 🔒 **Seguridad Implementada**

### **JWT Authentication**
- ✅ Configurado en `Program.cs`
- ✅ Token con claims: UserId, Email, Username, Role
- ✅ Expiración configurable (60 minutos por defecto)
- ✅ Firma con HMACSHA256

### **Autorización por Roles**
- ✅ `[Authorize]` - Requiere estar autenticado
- ✅ `[Authorize(Roles = "Administrador")]` - Solo administradores
- ✅ Validación de propiedad (usuarios solo editan su perfil)

### **Criptografía**
- ✅ Contraseñas: PBKDF2-SHA256 (100,000 iteraciones)
- ✅ Videos: ChaCha20-Poly1305 (cifrado autenticado)
- ✅ Integridad: SHA-256 + HMAC-SHA256
- ✅ Claves RSA: 2048 bits con OAEP

---

## 📡 **Endpoints Disponibles**

### **Autenticación (Sin autenticación requerida)**
```http
POST /api/auth/register
Content-Type: application/json

{
  "nombreUsuario": "admin1",
  "email": "admin@example.com",
  "password": "password123",
  "tipoUsuario": "Administrador"
}

Response: { token, email, username, userType, message }
```

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "password123"
}

Response: { token, email, username, userType, message }
```

```http
GET /api/auth/me
Authorization: Bearer {token}

Response: { idUsuario, nombreUsuario, email, tipoUsuario, ... }
```

### **Usuarios (Requiere autenticación)**
```http
GET /api/users
Authorization: Bearer {token}
Roles: Administrador

Response: [{ idUsuario, nombreUsuario, email, ... }]
```

```http
GET /api/users/{id}
Authorization: Bearer {token}

Response: { idUsuario, nombreUsuario, email, ... }
```

```http
PUT /api/users/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "nombreUsuario": "nuevo_nombre",
  "email": "nuevo@example.com"
}
```

```http
DELETE /api/users/{id}
Authorization: Bearer {token}
Roles: Administrador
```

### **Videos (Requiere autenticación)**
```http
GET /api/videos
Authorization: Bearer {token}

Response: [{ idVideo, tituloVideo, descripcion, nombreAdministrador, ... }]
```

```http
POST /api/videos/upload
Authorization: Bearer {token}
Roles: Administrador
Content-Type: multipart/form-data

Form Data:
- titulo: string
- descripcion: string (opcional)
- videoFile: file

Response: { idVideo, tituloVideo, message, ... }
```

```http
GET /api/videos/{id}
Authorization: Bearer {token}

Response: { idVideo, tituloVideo, descripcion, ... }
```

```http
DELETE /api/videos/{id}
Authorization: Bearer {token}
Roles: Administrador (solo el dueño)
```

---

## 🚀 **Cómo Ejecutar**

### **1. Verificar Base de Datos**
Asegúrate de que la base de datos `Data_base_cripto` esté creada y accesible.

### **2. Ejecutar el Proyecto**
```bash
cd SecureVideoStreaming.API
dotnet run
```

### **3. Abrir Swagger**
```
https://localhost:5140/swagger
```

---

## 📝 **Flujo de Uso Típico**

### **Escenario 1: Administrador**
1. **Registrarse** como Administrador
   ```
   POST /api/auth/register
   { "nombreUsuario": "admin1", "email": "admin@test.com", "password": "pass123", "tipoUsuario": "Administrador" }
   ```

2. **Iniciar sesión**
   ```
   POST /api/auth/login
   { "email": "admin@test.com", "password": "pass123" }
   ```
   → Guardar el `token` recibido

3. **Subir un video**
   ```
   POST /api/videos/upload
   Authorization: Bearer {token}
   Form: titulo="Mi Video", videoFile=...
   ```

4. **Ver sus videos**
   ```
   GET /api/videos
   Authorization: Bearer {token}
   ```

5. **Ver todos los usuarios**
   ```
   GET /api/users
   Authorization: Bearer {token}
   ```

### **Escenario 2: Usuario Normal**
1. **Registrarse** como Usuario
   ```
   POST /api/auth/register
   { "nombreUsuario": "user1", "email": "user@test.com", "password": "pass123", "tipoUsuario": "Usuario" }
   ```

2. **Iniciar sesión**
   ```
   POST /api/auth/login
   { "email": "user@test.com", "password": "pass123" }
   ```

3. **Ver grid de videos** (solo lectura)
   ```
   GET /api/videos
   Authorization: Bearer {token}
   ```

4. **Ver su perfil**
   ```
   GET /api/auth/me
   Authorization: Bearer {token}
   ```

---

## 🎨 **Frontend Recomendado**

Para el **Home** después del login:

### **Admin View** (Administrador)
```html
Dashboard:
├── Header: "Bienvenido, {nombreUsuario} (Administrador)"
├── Botón: "Subir Video"
├── Grid de Videos:
│   ├── Título, Descripción, Fecha
│   ├── Botón "Eliminar" (solo sus videos)
│   └── Tamaño, Estado
└── Menú: Usuarios, Videos, Perfil, Logout
```

### **User View** (Usuario)
```html
Dashboard:
├── Header: "Bienvenido, {nombreUsuario}"
├── Grid de Videos (solo lectura):
│   ├── Título, Descripción, Fecha
│   ├── Administrador que lo subió
│   └── Tamaño
└── Menú: Videos, Perfil, Logout
```

---

## ✅ **Testing Manual en Swagger**

### **Test 1: Registro**
1. Abrir `https://localhost:5140/swagger`
2. Ejecutar `POST /api/auth/register`
3. Body:
   ```json
   {
     "nombreUsuario": "admin_test",
     "email": "admin@test.com",
     "password": "Test123!",
     "tipoUsuario": "Administrador"
   }
   ```
4. Verificar respuesta con token

### **Test 2: Login**
1. Ejecutar `POST /api/auth/login`
2. Body:
   ```json
   {
     "email": "admin@test.com",
     "password": "Test123!"
   }
   ```
3. Copiar el `token` de la respuesta

### **Test 3: Autenticación**
1. En Swagger, click en "Authorize" (candado arriba)
2. Escribir: `Bearer {token}` (reemplazar {token})
3. Click "Authorize"

### **Test 4: Ver Perfil**
1. Ejecutar `GET /api/auth/me`
2. Verificar que devuelve información del usuario

### **Test 5: Listar Videos**
1. Ejecutar `GET /api/videos`
2. Debe devolver array vacío inicialmente

### **Test 6: Subir Video**
1. Ejecutar `POST /api/videos/upload`
2. Form:
   - titulo: "Video de Prueba"
   - descripcion: "Primer video"
   - videoFile: Seleccionar archivo
3. Verificar mensaje de éxito

### **Test 7: Grid de Videos**
1. Ejecutar nuevamente `GET /api/videos`
2. Verificar que aparece el video subido

---

## 🔧 **Configuración**

### **appsettings.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=Data_base_cripto;..."
  },
  "Jwt": {
    "SecretKey": "TU_CLAVE_SECRETA_MUY_LARGA_Y_SEGURA_MINIMO_32_CARACTERES_AQUI",
    "Issuer": "SecureVideoStreaming",
    "Audience": "SecureVideoStreamingUsers",
    "ExpirationMinutes": 60
  },
  "Storage": {
    "VideosPath": "./Storage/Videos"
  }
}
```

---

## 📊 **Estado del Proyecto**

### ✅ **Completado (100%)**
- [x] Registro de usuarios
- [x] Login con JWT
- [x] Roles (Administrador/Usuario)
- [x] CRUD de usuarios
- [x] Upload de videos (Admins)
- [x] Grid de videos (todos)
- [x] Cifrado automático de videos
- [x] Autorización por rol
- [x] Validaciones de entrada
- [x] Manejo de errores

### 🎯 **Funcionalidades Extra Implementadas**
- ✅ Cifrado de videos con ChaCha20-Poly1305
- ✅ HMAC y SHA-256 para integridad
- ✅ Generación automática de claves RSA
- ✅ Soft delete de usuarios y videos
- ✅ Actualización de último acceso

---

## 📈 **Próximas Mejoras (Opcionales)**

### **Funcionalidades Futuras**
- [ ] Descarga de videos descifrados
- [ ] Sistema de permisos para usuarios normales
- [ ] Paginación en el grid
- [ ] Filtros y búsqueda
- [ ] Streaming progresivo
- [ ] Thumbnails de videos

### **Frontend**
- [ ] React/Angular/Vue para el UI
- [ ] Vista de grid responsive
- [ ] Drag & drop para upload
- [ ] Progress bar de cifrado

---

## 🎓 **Resumen Técnico**

### **Stack Tecnológico**
- **.NET 8.0** - Framework principal
- **Entity Framework Core 8** - ORM
- **SQL Server** - Base de datos
- **JWT Bearer** - Autenticación
- **Swagger/OpenAPI** - Documentación API
- **ChaCha20-Poly1305** - Cifrado de videos
- **PBKDF2** - Hash de contraseñas

### **Principios Aplicados**
- ✅ Clean Architecture (capas separadas)
- ✅ SOLID principles
- ✅ Repository Pattern
- ✅ Dependency Injection
- ✅ DTO Pattern
- ✅ Secure by design

---

## 🏆 **Entregable COMPLETO**

✅ **CRUD de Usuarios**: Registro, Login, Ver, Editar, Eliminar  
✅ **Roles Diferenciados**: Administrador (sube videos) vs Usuario (solo ve)  
✅ **Home después de Login**: Grid de videos según rol  
✅ **Seguridad**: JWT, PBKDF2, Cifrado ChaCha20  
✅ **API RESTful completa** con documentación Swagger  

**Estado:** ✨ **LISTO PARA DEMO** ✨

---

**Fecha:** 9 de Noviembre de 2025  
**Versión:** 1.0 - Primer Entregable  
**Compilación:** ✅ Exitosa (2 warnings menores)  
**Base de Datos:** ✅ `Data_base_cripto` configurada  
