# 🔧 Script SQL - Otorgar Permiso Rápido

## ⚡ Otorgar Permiso Manualmente (Sin Swagger)

### **Paso 1: Verificar tu ID de usuario**

```sql
-- Busca tu usuario
SELECT IdUsuario, NombreUsuario, Email, TipoUsuario
FROM Usuarios
ORDER BY FechaRegistro DESC;
```

**Anota tu `IdUsuario`** (ejemplo: 5)

---

### **Paso 2: Verificar videos disponibles**

```sql
-- Ver todos los videos
SELECT 
    v.IdVideo,
    v.TituloVideo,
    v.Descripcion,
    v.TamañoArchivo / 1024.0 / 1024.0 AS TamañoMB,
    v.Duracion / 60 AS Minutos,
    u.NombreUsuario AS Administrador
FROM Videos v
INNER JOIN Usuarios u ON v.IdAdministrador = u.IdUsuario
WHERE v.EstadoProcesamiento = 'Disponible'
ORDER BY v.FechaSubida DESC;
```

**Anota el `IdVideo`** del que quieres acceso (ejemplo: 1)

---

### **Paso 3: Verificar que hay un administrador**

```sql
-- Ver administradores
SELECT IdUsuario, NombreUsuario, TipoUsuario
FROM Usuarios
WHERE TipoUsuario = 'Administrador';
```

**Anota el `IdUsuario` del admin** (ejemplo: 1)

---

### **Paso 4: Otorgar permiso PERMANENTE**

```sql
-- Variables (MODIFICA ESTOS VALORES)
DECLARE @TuIdUsuario INT = 5;          -- ← TU ID aquí
DECLARE @IdVideo INT = 1;              -- ← ID del video
DECLARE @IdAdmin INT = 1;              -- ← ID del administrador

-- Otorgar permiso permanente
INSERT INTO Permisos (
    IdVideo,
    IdUsuario,
    OtorgadoPor,
    TipoPermiso,
    FechaOtorgamiento,
    FechaExpiracion,
    NumeroAccesos,
    UltimoAcceso,
    FechaRevocacion
)
VALUES (
    @IdVideo,
    @TuIdUsuario,
    @IdAdmin,
    'Lectura',
    GETDATE(),
    NULL,              -- NULL = Permanente
    0,
    NULL,
    NULL
);

-- Verificar que se creó
SELECT 
    p.IdPermiso,
    v.TituloVideo,
    u.NombreUsuario AS Usuario,
    p.TipoPermiso,
    p.FechaOtorgamiento,
    p.FechaExpiracion,
    CASE 
        WHEN p.FechaExpiracion IS NULL THEN 'Permanente'
        ELSE 'Temporal'
    END AS TipoDuracion
FROM Permisos p
INNER JOIN Videos v ON p.IdVideo = v.IdVideo
INNER JOIN Usuarios u ON p.IdUsuario = u.IdUsuario
WHERE p.IdUsuario = @TuIdUsuario;
```

---

### **Paso 5: Otorgar permiso TEMPORAL (7 días)**

```sql
-- Variables
DECLARE @TuIdUsuario INT = 5;
DECLARE @IdVideo INT = 2;
DECLARE @IdAdmin INT = 1;

-- Otorgar permiso por 7 días
INSERT INTO Permisos (
    IdVideo,
    IdUsuario,
    OtorgadoPor,
    TipoPermiso,
    FechaOtorgamiento,
    FechaExpiracion,
    NumeroAccesos,
    UltimoAcceso,
    FechaRevocacion
)
VALUES (
    @IdVideo,
    @TuIdUsuario,
    @IdAdmin,
    'Temporal',
    GETDATE(),
    DATEADD(DAY, 7, GETDATE()),  -- Expira en 7 días
    0,
    NULL,
    NULL
);

-- Ver la fecha de expiración
SELECT 
    p.IdPermiso,
    v.TituloVideo,
    p.FechaOtorgamiento,
    p.FechaExpiracion,
    DATEDIFF(HOUR, GETDATE(), p.FechaExpiracion) AS HorasRestantes
FROM Permisos p
INNER JOIN Videos v ON p.IdVideo = v.IdVideo
WHERE p.IdPermiso = SCOPE_IDENTITY();
```

---

### **📊 Script Completo - Otorgar Acceso a TODOS los Videos**

```sql
-- OTORGAR PERMISO A TODOS LOS VIDEOS DISPONIBLES

DECLARE @TuIdUsuario INT = 5;      -- ← CAMBIA ESTO
DECLARE @IdAdmin INT = 1;           -- ← ID del admin

-- Insertar permisos para todos los videos
INSERT INTO Permisos (IdVideo, IdUsuario, OtorgadoPor, TipoPermiso, FechaOtorgamiento, FechaExpiracion, NumeroAccesos)
SELECT 
    v.IdVideo,
    @TuIdUsuario,
    @IdAdmin,
    'Lectura',
    GETDATE(),
    NULL,
    0
FROM Videos v
WHERE v.EstadoProcesamiento = 'Disponible'
  AND NOT EXISTS (
      SELECT 1 FROM Permisos p 
      WHERE p.IdVideo = v.IdVideo 
        AND p.IdUsuario = @TuIdUsuario
        AND p.FechaRevocacion IS NULL
  );

-- Ver resultado
SELECT 
    v.TituloVideo,
    p.TipoPermiso,
    p.FechaOtorgamiento,
    '✅ Activo' AS Estado
FROM Permisos p
INNER JOIN Videos v ON p.IdVideo = v.IdVideo
WHERE p.IdUsuario = @TuIdUsuario
  AND p.FechaRevocacion IS NULL
ORDER BY p.FechaOtorgamiento DESC;
```

---

### **🔍 Queries Útiles**

#### **Ver todos tus permisos actuales:**
```sql
DECLARE @TuIdUsuario INT = 5;

SELECT 
    p.IdPermiso,
    v.TituloVideo,
    v.TamañoArchivo / 1024.0 / 1024.0 AS TamañoMB,
    p.TipoPermiso,
    p.FechaOtorgamiento,
    p.FechaExpiracion,
    p.NumeroAccesos,
    CASE 
        WHEN p.FechaRevocacion IS NOT NULL THEN '🔴 Revocado'
        WHEN p.FechaExpiracion IS NOT NULL AND p.FechaExpiracion < GETDATE() THEN '⚠️ Expirado'
        ELSE '✅ Activo'
    END AS Estado
FROM Permisos p
INNER JOIN Videos v ON p.IdVideo = v.IdVideo
WHERE p.IdUsuario = @TuIdUsuario
ORDER BY p.FechaOtorgamiento DESC;
```

#### **Ver videos SIN permiso:**
```sql
DECLARE @TuIdUsuario INT = 5;

SELECT 
    v.IdVideo,
    v.TituloVideo,
    u.NombreUsuario AS Administrador,
    '🔒 Sin Acceso' AS Estado
FROM Videos v
INNER JOIN Usuarios u ON v.IdAdministrador = u.IdUsuario
WHERE v.EstadoProcesamiento = 'Disponible'
  AND NOT EXISTS (
      SELECT 1 FROM Permisos p 
      WHERE p.IdVideo = v.IdVideo 
        AND p.IdUsuario = @TuIdUsuario
        AND p.FechaRevocacion IS NULL
  )
ORDER BY v.FechaSubida DESC;
```

#### **Revocar un permiso:**
```sql
DECLARE @IdPermiso INT = 1;  -- ID del permiso a revocar

UPDATE Permisos
SET FechaRevocacion = GETDATE()
WHERE IdPermiso = @IdPermiso;

-- Verificar
SELECT * FROM Permisos WHERE IdPermiso = @IdPermiso;
```

#### **Extender fecha de expiración:**
```sql
DECLARE @IdPermiso INT = 1;

UPDATE Permisos
SET FechaExpiracion = DATEADD(DAY, 30, GETDATE())  -- Extender 30 días más
WHERE IdPermiso = @IdPermiso;

-- Verificar
SELECT 
    IdPermiso,
    FechaExpiracion,
    DATEDIFF(DAY, GETDATE(), FechaExpiracion) AS DiasRestantes
FROM Permisos 
WHERE IdPermiso = @IdPermiso;
```

---

### **✅ Después de ejecutar el script SQL:**

1. **Refresca el navegador** en http://localhost:5140/VideoGrid
2. **Deberías ver** videos con badge ✅ Activo
3. **Botones habilitados**: "🔑 Solicitar Claves" y "▶️ Ver Video"

---

### **🎯 Script de Ejemplo Completo**

```sql
-- =====================================================
-- SCRIPT COMPLETO: Setup de Permisos para Pruebas
-- =====================================================

-- 1. Crear usuario de prueba (si no existe)
IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE NombreUsuario = 'usuario_test')
BEGIN
    INSERT INTO Usuarios (NombreUsuario, Email, ContraseñaHash, Salt, TipoUsuario, FechaRegistro)
    VALUES (
        'usuario_test',
        'test@example.com',
        0x1234567890ABCDEF,  -- Hash de ejemplo
        0xFEDCBA0987654321,  -- Salt de ejemplo
        'Usuario',
        GETDATE()
    );
END

-- 2. Obtener IDs
DECLARE @IdUsuarioTest INT = (SELECT IdUsuario FROM Usuarios WHERE NombreUsuario = 'usuario_test');
DECLARE @IdAdmin INT = (SELECT TOP 1 IdUsuario FROM Usuarios WHERE TipoUsuario = 'Administrador');

-- 3. Ver información
PRINT 'ID Usuario Test: ' + CAST(@IdUsuarioTest AS VARCHAR);
PRINT 'ID Administrador: ' + CAST(@IdAdmin AS VARCHAR);

-- 4. Otorgar permisos a 3 videos de ejemplo
INSERT INTO Permisos (IdVideo, IdUsuario, OtorgadoPor, TipoPermiso, FechaOtorgamiento, FechaExpiracion, NumeroAccesos)
SELECT TOP 3
    v.IdVideo,
    @IdUsuarioTest,
    @IdAdmin,
    'Lectura',
    GETDATE(),
    NULL,
    0
FROM Videos v
WHERE v.EstadoProcesamiento = 'Disponible'
  AND NOT EXISTS (
      SELECT 1 FROM Permisos p 
      WHERE p.IdVideo = v.IdVideo 
        AND p.IdUsuario = @IdUsuarioTest
  );

-- 5. Verificar resultado
SELECT 
    'Permisos otorgados exitosamente' AS Mensaje,
    COUNT(*) AS TotalPermisos
FROM Permisos
WHERE IdUsuario = @IdUsuarioTest;

-- 6. Ver detalle
SELECT 
    p.IdPermiso,
    v.TituloVideo,
    p.TipoPermiso,
    p.FechaOtorgamiento,
    '✅ Activo' AS Estado
FROM Permisos p
INNER JOIN Videos v ON p.IdVideo = v.IdVideo
WHERE p.IdUsuario = @IdUsuarioTest;
```

---

**¡Ejecuta este script y luego refresca el Grid!** 🎉
