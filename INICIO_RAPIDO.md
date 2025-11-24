# 🚀 Inicio Rápido - Secure Video Streaming

## Setup en 5 Minutos

---

## 📋 Pre-requisitos

```
✅ .NET 8.0 SDK
✅ SQL Server (LocalDB o SQL Express)
✅ Visual Studio 2022 / VS Code
✅ Git
```

---

## ⚡ Pasos de Instalación

### 1. Clonar Repositorio
```powershell
git clone https://github.com/GarciaGarciaAramJesua/SecureVideoStreaming.git
cd SecureVideoStreaming
```

### 2. Restaurar Dependencias
```powershell
dotnet restore
```

### 3. Configurar Base de Datos

Editar `SecureVideoStreaming.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Data_base_cripto;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 4. Crear Base de Datos
```powershell
cd SecureVideoStreaming.Data
dotnet ef database update
```

O ejecutar el script SQL directamente en SQL Server.

### 5. Ejecutar Proyecto
```powershell
cd SecureVideoStreaming.API
dotnet run
```

### 6. Abrir Swagger
```
http://localhost:5140/swagger
```

---

## 🧪 Prueba Rápida

### 1. Registrar Admin
```http
POST /api/auth/register
{
  "nombreUsuario": "admin",
  "email": "admin@test.com",
  "password": "Admin123!",
  "tipoUsuario": "Administrador"
}
```
**Guardar:** Token JWT

### 2. Subir Video
```http
POST /api/videos/upload
Authorization: Bearer {tu_token}
Content-Type: multipart/form-data

titulo: Video de Prueba
descripcion: Mi primer video
videoFile: [archivo]
```

### 3. Registrar Usuario
```http
POST /api/auth/register
{
  "nombreUsuario": "usuario",
  "email": "user@test.com",
  "password": "User123!",
  "tipoUsuario": "Usuario"
}
```

### 4. Otorgar Permiso
```http
POST /api/permissions/grant
Authorization: Bearer {admin_token}
{
  "idVideo": 1,
  "idUsuario": 2,
  "otorgadoPor": 1,
  "tipoPermiso": "Lectura"
}
```

### 5. Ver Grid (Como Usuario)
```http
GET /api/videogrid
Authorization: Bearer {user_token}
```

### 6. Solicitar Claves
```http
GET /api/keydistribution/request/1
Authorization: Bearer {user_token}
```

---

## 📚 Documentación

- **ENTREGABLE_2.md** - Documentación técnica completa
- **PRUEBAS_ENTREGABLE_2.md** - Guía de pruebas detallada
- **RESUMEN_EJECUTIVO_E2.md** - Resumen ejecutivo
- **README.md** - Información general

---

## 🐛 Troubleshooting

### Error de Conexión a BD
```powershell
# Verificar SQL Server está corriendo
Get-Service MSSQL*

# Iniciar si está detenido
Start-Service MSSQL$SQLEXPRESS
```

### Error de Migración
```powershell
# Eliminar migraciones anteriores
cd SecureVideoStreaming.Data
Remove-Item -Recurse Migrations

# Crear nueva migración
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Puerto en Uso
Editar `SecureVideoStreaming.API/Properties/launchSettings.json`:
```json
"applicationUrl": "http://localhost:NUEVO_PUERTO"
```

---

## 📞 Soporte

Repository: https://github.com/GarciaGarciaAramJesua/SecureVideoStreaming
Issues: Crear issue en GitHub

---

## ✅ Verificación de Instalación

```powershell
# 1. Proyecto compila
dotnet build

# 2. Tests pasan
dotnet test

# 3. Servidor inicia
dotnet run --project SecureVideoStreaming.API

# 4. Swagger accesible
# Abrir: http://localhost:5140/swagger

# 5. Base de datos existe
# En SQL Server Management Studio:
# - Database: Data_base_cripto
# - Tablas: 6 (Usuarios, Videos, Permisos, etc.)
```

---

## 🎯 Próximos Pasos

1. Revisar **ENTREGABLE_2.md** para entender la arquitectura
2. Ejecutar pruebas de **PRUEBAS_ENTREGABLE_2.md**
3. Explorar endpoints en Swagger
4. Revisar código en Visual Studio

---

**¡Listo! El sistema está funcionando. 🎉**
