# ✅ Checklist de Pruebas - VideoGrid

## 🚀 Inicio Rápido

### 1. Iniciar aplicación
```powershell
cd 'c:\Users\herna\OneDrive\Documents\Cripto_project_final\SecureVideoStreaming\SecureVideoStreaming.API'
dotnet run
```

Espera a ver: `Now listening on: http://localhost:5140`

---

## 📋 Pruebas a Realizar

### ✅ Prueba 1: VideoGrid sin autenticación
- [ ] Abre navegador en modo incógnito
- [ ] Ve a: `http://localhost:5140/VideoGrid`
- [ ] **Debe redirigir a** `/Login`
- [ ] **Debe mostrar mensaje**: "Debes iniciar sesión para ver el grid de videos"

**Resultado esperado**: ✅ Redirección automática

---

### ✅ Prueba 2: Registro + Login + VideoGrid
- [ ] Ve a: `http://localhost:5140/Register`
- [ ] Registra usuario nuevo:
  ```
  Email: prueba1@test.com
  Password: Pass123!
  Confirmar: Pass123!
  Nombre: Prueba Usuario
  ```
- [ ] Clic en "Registrar"
- [ ] **Debe mostrar**: Mensaje de éxito
- [ ] Ve a: `http://localhost:5140/Login`
- [ ] Inicia sesión con: `prueba1@test.com` / `Pass123!`
- [ ] **Debe redirigir a**: `/Home`
- [ ] Clic en "Video Grid" en el menú superior
- [ ] **Debe cargar**: Página VideoGrid SIN error 401

**Resultado esperado**: ✅ VideoGrid se carga correctamente

---

### ✅ Prueba 3: Verificar Grid vacío
Si no hay videos en la BD:
- [ ] Debes ver mensaje: "No se encontraron videos"
- [ ] Estadísticas en 0:
  - Total Videos: 0
  - Con Permiso Activo: 0
  - Permisos Expirados: 0
  - Sin Permiso: 0

**Resultado esperado**: ✅ Interfaz limpia sin errores

---

### ✅ Prueba 4: Subir video (opcional)
- [ ] Clic en "Upload Video" en el menú
- [ ] Selecciona un archivo de video (MP4, AVI, MKV)
- [ ] Completa:
  ```
  Título: Video de Prueba 1
  Descripción: Video para probar el grid
  ```
- [ ] Clic en "Subir Video"
- [ ] Espera a que termine el cifrado
- [ ] Regresa a VideoGrid
- [ ] **Debe mostrar**: 1 video con estado "Sin Permiso"

**Resultado esperado**: ✅ Video aparece en el grid

---

### ✅ Prueba 5: Otorgar permisos (SQL)

#### Opción A: Desde SQL Server Management Studio
1. Conecta a tu BD `SecureVideoStreamingDB`
2. Ejecuta este script:

```sql
-- 1. Ver usuarios registrados
SELECT UserId, Email, NombreCompleto 
FROM Users 
ORDER BY UserId DESC;

-- 2. Ver videos disponibles
SELECT VideoId, Titulo, PropietarioId 
FROM Videos 
ORDER BY VideoId DESC;

-- 3. Otorgar permiso permanente
-- Reemplaza <UserId> y <VideoId> con los valores reales
INSERT INTO Permisos (VideoId, UsuarioId, FechaOtorgamiento, FechaExpiracion, OtorgadoPorId)
VALUES (
    1,  -- VideoId del video
    2,  -- UserId del usuario que recibirá el permiso
    GETDATE(),  -- Fecha actual
    DATEADD(DAY, 365, GETDATE()),  -- Expira en 1 año
    1   -- ID del admin que otorga el permiso
);

-- 4. Verificar permiso creado
SELECT p.PermisoId, p.VideoId, v.Titulo, p.UsuarioId, u.Email, 
       p.FechaOtorgamiento, p.FechaExpiracion
FROM Permisos p
INNER JOIN Videos v ON p.VideoId = v.VideoId
INNER JOIN Users u ON p.UsuarioId = u.UserId
ORDER BY p.PermisoId DESC;
```

#### Opción B: Script rápido (admin otorga permiso al primer usuario sobre el primer video)
```sql
DECLARE @VideoId INT = (SELECT TOP 1 VideoId FROM Videos ORDER BY VideoId);
DECLARE @UserId INT = (SELECT TOP 1 UserId FROM Users WHERE UserType = 1 ORDER BY UserId);
DECLARE @AdminId INT = (SELECT TOP 1 UserId FROM Users WHERE UserType = 2 ORDER BY UserId);

IF @VideoId IS NOT NULL AND @UserId IS NOT NULL AND @AdminId IS NOT NULL
BEGIN
    INSERT INTO Permisos (VideoId, UsuarioId, FechaOtorgamiento, FechaExpiracion, OtorgadoPorId)
    VALUES (@VideoId, @UserId, GETDATE(), DATEADD(DAY, 30, GETDATE()), @AdminId);
    
    PRINT 'Permiso otorgado exitosamente';
END
ELSE
BEGIN
    PRINT 'Error: No se encontraron datos necesarios';
    PRINT 'Videos: ' + CAST(@VideoId AS VARCHAR);
    PRINT 'Usuario: ' + CAST(@UserId AS VARCHAR);
    PRINT 'Admin: ' + CAST(@AdminId AS VARCHAR);
END
```

- [ ] Ejecuta uno de los scripts anteriores
- [ ] Refresca la página VideoGrid (F5)
- [ ] **Debe mostrar**: Video con estado "Activo" (badge verde)
- [ ] **Estadísticas actualizadas**: "Con Permiso Activo: 1"

**Resultado esperado**: ✅ Video con permiso activo visible

---

### ✅ Prueba 6: Probar filtros
- [ ] **Filtro de búsqueda**: Escribe el nombre de un video → Enter
- [ ] **Filtro por estado**: Selecciona "Activo" → Enter
- [ ] **Limpiar filtros**: Borra texto y selecciona "Todos" → Enter

**Resultado esperado**: ✅ Filtros funcionan correctamente

---

### ✅ Prueba 7: Modal de detalles
- [ ] Clic en botón "Ver Detalles" de un video
- [ ] **Debe aparecer**: Modal con información completa
  - Título
  - Descripción
  - Propietario
  - Duración
  - Tamaño
  - Algoritmo: ChaCha20-Poly1305
  - Estado de permiso
  - Número de accesos
  - Fecha de otorgamiento (si tiene permiso)
- [ ] Clic en "Cerrar" o fuera del modal
- [ ] Modal se cierra correctamente

**Resultado esperado**: ✅ Modal funciona

---

## 🐛 Solución de Problemas

### ❌ Problema: Error 401 aún aparece
**Solución**:
```powershell
# Detener aplicación (Ctrl+C en terminal)
dotnet clean
dotnet build
dotnet run
```

### ❌ Problema: Session no persiste
**Verificar** en `Program.cs`:
```csharp
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Y en el middleware:
app.UseSession();
```

### ❌ Problema: No aparecen videos en el Grid
**Causas posibles**:
1. No hay videos en la BD → Subir videos con `/UploadVideo`
2. Usuario no tiene permisos → Ejecutar scripts SQL de permisos
3. Error en el servicio → Ver logs en la consola

### ❌ Problema: SQL Scripts no funcionan
**Verificar**:
```sql
-- ¿Hay usuarios?
SELECT COUNT(*) FROM Users;

-- ¿Hay videos?
SELECT COUNT(*) FROM Videos;

-- ¿Hay permisos?
SELECT COUNT(*) FROM Permisos;
```

---

## 📊 Resultados Esperados

### Logs exitosos en consola:
```
info: SecureVideoStreaming.API.Pages.LoginModel[0]
      Usuario prueba1@test.com inició sesión exitosamente

info: SecureVideoStreaming.API.Pages.VideoGridModel[0]
      Usuario 2 cargó el grid completo: 1 videos totales
```

### NO debería aparecer:
```
❌ fail: Microsoft.AspNetCore.Authentication.JwtBearer
    Authorization failed...
```

---

## ✅ Checklist Final

- [ ] VideoGrid redirige a Login si no autenticado
- [ ] Login exitoso permite acceso a VideoGrid
- [ ] VideoGrid carga sin error 401
- [ ] Estadísticas se muestran correctamente
- [ ] Videos aparecen con estados (Sin Permiso / Activo / Expirado)
- [ ] Filtros funcionan
- [ ] Modal de detalles funciona
- [ ] Logs muestran información correcta

---

## 📝 Notas

- **Entregable 2**: ✅ COMPLETO (Permisos + Grid + Distribución de Claves)
- **Autenticación VideoGrid**: ✅ FIXED (cambiado de JWT a Sesión)
- **Próximo**: Entregable 3 (Descarga/Stream + Desencriptación + Reproducción)

---

**Fecha**: 2024  
**Estado**: ✅ LISTO PARA PROBAR
