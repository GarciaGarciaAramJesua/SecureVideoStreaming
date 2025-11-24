# 🎨 Guía Visual - Cómo Ver el Grid de Videos

## ✅ Aplicación Corriendo

La aplicación ya está ejecutándose en: **http://localhost:5140**

---

## 📋 Opciones para Ver el Grid

### **Opción 1: Interfaz Visual (Página Razor)** 🖼️

#### Paso 1: Acceder al Sistema
```
http://localhost:5140/Login
```

#### Paso 2: Iniciar Sesión
- **Usuario**: El que registraste previamente
- **Contraseña**: Tu contraseña

#### Paso 3: Navegar al Grid
```
http://localhost:5140/VideoGrid
```

O hacer clic en el menú: **"Galería de Videos"**

#### ¿Qué verás?

```
┌─────────────────────────────────────────────────────────────┐
│  🏠 Inicio  │  📼 Galería de Videos  │  ☁️ Subir Video      │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  📊 Galería de Videos                                        │
│  Explora los videos disponibles y solicita acceso           │
│                                                               │
│  ┌──────────────────────────────────────────────────┐       │
│  │ 🔍 Buscar: [___________]  Estado: [Todos ▼]    │       │
│  │ 👤 Admin:  [___________]  [🔎 Buscar]          │       │
│  └──────────────────────────────────────────────────┘       │
│                                                               │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐       │
│  │   10    │  │    5    │  │    2    │  │    3    │       │
│  │ Totales │  │ Activos │  │Expirados│  │Sin Acceso│       │
│  └─────────┘  └─────────┘  └─────────┘  └─────────┘       │
│                                                               │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐           │
│  │ 🎬 Video 1 │  │ 🎬 Video 2 │  │ 🎬 Video 3 │           │
│  │ ✅ Activo  │  │ ⚠️ Expirado│  │ 🔒 Bloqueado│           │
│  │            │  │            │  │            │           │
│  │ 📊 15.2 MB │  │ 📊 8.5 MB  │  │ 📊 22.1 MB │           │
│  │ ⏱️ 05:30   │  │ ⏱️ 03:15   │  │ ⏱️ 10:45   │           │
│  │ 🔐 ChaCha20│  │ 🔐 ChaCha20│  │ 🔐 ChaCha20│           │
│  │            │  │            │  │            │           │
│  │ [🔑 Claves]│  │ [⚠️ Expiró]│  │ [🔒 Bloqueado]         │
│  │ [▶️ Ver]   │  │            │  │            │           │
│  │ [ℹ️ Info]  │  │ [ℹ️ Info]  │  │ [ℹ️ Info]  │           │
│  └────────────┘  └────────────┘  └────────────┘           │
└─────────────────────────────────────────────────────────────┘
```

---

### **Opción 2: API REST (Swagger UI)** 🔧

#### Paso 1: Acceder a Swagger
```
http://localhost:5140/swagger
```

#### Paso 2: Autenticarse

1. **Expandir** `POST /api/auth/login`
2. **Clic** en "Try it out"
3. **Ingresar** credenciales:
```json
{
  "nombreUsuario": "tu_usuario",
  "contraseña": "tu_contraseña"
}
```
4. **Copiar** el token JWT de la respuesta
5. **Clic** en el botón "🔓 Authorize" (arriba a la derecha)
6. **Pegar** el token: `Bearer TU_TOKEN_AQUI`
7. **Clic** en "Authorize"

#### Paso 3: Probar Endpoints del Grid

##### **A. Ver Todos los Videos**
```
GET /api/videogrid
```
- Clic en "Try it out"
- Clic en "Execute"
- Ver la respuesta JSON con todos los videos

##### **B. Buscar con Filtros**
```
GET /api/videogrid/search
```
Parámetros opcionales:
- `searchTerm`: Buscar por título (ej: "tutorial")
- `administrador`: Filtrar por admin (ej: "admin1")
- `soloConPermiso`: Solo videos con acceso (true/false)

##### **C. Ver un Video Específico**
```
GET /api/videogrid/{videoId}
```
- Reemplazar `{videoId}` con un ID real (ej: 1)

---

## 📊 Respuesta JSON de Ejemplo

```json
{
  "success": true,
  "message": "Videos obtenidos exitosamente",
  "data": [
    {
      "idVideo": 1,
      "tituloVideo": "Tutorial de Criptografía",
      "descripcion": "Introducción a ChaCha20-Poly1305",
      "tamañoArchivo": 15728640,
      "tamañoArchivoFormateado": "15.0 MB",
      "duracion": 330,
      "duracionFormateada": "05:30",
      "formatoVideo": "mp4",
      "fechaSubida": "2024-11-20T10:30:00Z",
      "nombreAdministrador": "admin1",
      "algoritmoCifrado": "ChaCha20-Poly1305",
      "tienePermiso": true,
      "idPermiso": 42,
      "tipoPermiso": "Lectura",
      "fechaOtorgamiento": "2024-11-15T08:00:00Z",
      "fechaExpiracion": null,
      "numeroAccesos": 5,
      "ultimoAcceso": "2024-11-24T14:20:00Z",
      "permiteVisualizacion": true,
      "estadoPermiso": "Activo"
    },
    {
      "idVideo": 2,
      "tituloVideo": "Video de Prueba",
      "descripcion": "Demo",
      "tamañoArchivo": 8912896,
      "tamañoArchivoFormateado": "8.5 MB",
      "duracion": 195,
      "duracionFormateada": "03:15",
      "formatoVideo": "mp4",
      "fechaSubida": "2024-11-22T15:45:00Z",
      "nombreAdministrador": "admin2",
      "algoritmoCifrado": "ChaCha20-Poly1305",
      "tienePermiso": true,
      "idPermiso": 43,
      "tipoPermiso": "Temporal",
      "fechaOtorgamiento": "2024-11-20T10:00:00Z",
      "fechaExpiracion": "2024-11-23T10:00:00Z",
      "numeroAccesos": 2,
      "ultimoAcceso": "2024-11-22T18:00:00Z",
      "permiteVisualizacion": false,
      "estadoPermiso": "Expirado"
    },
    {
      "idVideo": 3,
      "tituloVideo": "Video Privado",
      "descripcion": "Solo para admins",
      "tamañoArchivo": 23068672,
      "tamañoArchivoFormateado": "22.0 MB",
      "duracion": 645,
      "duracionFormateada": "10:45",
      "formatoVideo": "mp4",
      "fechaSubida": "2024-11-18T09:00:00Z",
      "nombreAdministrador": "admin3",
      "algoritmoCifrado": "ChaCha20-Poly1305",
      "tienePermiso": false,
      "idPermiso": null,
      "tipoPermiso": null,
      "fechaOtorgamiento": null,
      "fechaExpiracion": null,
      "numeroAccesos": 0,
      "ultimoAcceso": null,
      "permiteVisualizacion": false,
      "estadoPermiso": "Sin Permiso"
    }
  ]
}
```

---

## 🎯 Funcionalidades del Grid

### **1. Filtros Avanzados** 🔍
- ✅ **Buscar por título**: Encuentra videos por nombre
- ✅ **Filtrar por administrador**: Ve videos de un admin específico
- ✅ **Filtrar por estado**:
  - `Activo`: Videos con permiso válido
  - `Expirado`: Permisos caducados
  - `Sin Permiso`: Videos bloqueados

### **2. Información Visual** 📊
Cada tarjeta muestra:
- 🎬 **Título del video**
- 👤 **Administrador propietario**
- 💾 **Tamaño formateado** (15.2 MB, 8.5 MB, etc.)
- ⏱️ **Duración formateada** (05:30, 03:15, etc.)
- 🔐 **Algoritmo de cifrado** (ChaCha20-Poly1305)
- 📅 **Fecha de subida**
- ✅/⚠️/🔒 **Badge de estado**

### **3. Información de Permisos** 🔑
Si tienes permiso:
- ✅ Fecha en que te otorgaron el acceso
- ⏰ Fecha de expiración (si es temporal)
- 👁️ Número de visualizaciones

### **4. Acciones Disponibles** ⚡
- **🔑 Solicitar Claves**: Obtén las claves criptográficas
- **▶️ Ver Video**: Reproducir (pendiente Entregable 3)
- **ℹ️ Ver Detalles**: Modal con información completa

### **5. Estadísticas en Tiempo Real** 📈
- 📊 **Total de videos** en el sistema
- ✅ **Videos con acceso activo**
- ⚠️ **Permisos expirados**
- 🔒 **Videos sin permiso**

---

## 🎨 Características Visuales

### **Colores por Estado**
```
✅ Verde  (Activo)      → Badge verde, botones habilitados
⚠️ Amarillo (Expirado)  → Badge amarillo, botón de renovación
🔒 Rojo (Sin Permiso)   → Badge rojo, botones bloqueados
```

### **Efectos Interactivos**
- 🎯 **Hover**: Las tarjetas se elevan al pasar el mouse
- 🎬 **Animación**: Iconos de play aumentan al hover
- 🌈 **Gradientes**: Fondo degradado profesional
- 💫 **Shadows**: Sombras suaves para profundidad

---

## 🧪 Casos de Prueba

### **Test 1: Ver Grid Completo**
```
1. Ir a http://localhost:5140/VideoGrid
2. Verificar que se muestran todos los videos
3. Verificar estadísticas en la parte superior
```

### **Test 2: Buscar Videos**
```
1. Escribir en el campo "Buscar": "tutorial"
2. Clic en "Buscar"
3. Verificar que solo aparecen videos con "tutorial"
```

### **Test 3: Filtrar por Estado**
```
1. Seleccionar "Activo" en el dropdown de Estado
2. Clic en "Buscar"
3. Verificar que solo aparecen videos con badge verde
```

### **Test 4: Ver Detalles**
```
1. Clic en "Ver Detalles" de cualquier video
2. Se abre modal con información completa
3. Verificar datos técnicos y de seguridad
```

### **Test 5: Solicitar Claves**
```
1. En un video con badge "Activo"
2. Clic en "Solicitar Claves"
3. Se descarga JSON con claves cifradas
```

---

## 🚀 Próximos Pasos (Entregable 3)

Cuando hagas clic en **"Ver Video"** actualmente muestra un alert porque falta implementar:

```
📌 Módulo de Descarga/Streaming (Pendiente)
├── DownloadController.cs    → Endpoints de descarga
├── StreamingService.cs       → Streaming por chunks
├── VideoPlayer.cshtml        → Reproductor con Web Crypto API
└── DecryptionHelper.cs       → Descifrado en cliente
```

---

## 📞 Solución de Problemas

### ❌ No veo videos en el grid
**Solución**: Verifica que haya videos en la base de datos
```sql
SELECT * FROM Videos WHERE EstadoProcesamiento = 'Disponible'
```

### ❌ Error 401 (Unauthorized)
**Solución**: Tu sesión expiró, vuelve a iniciar sesión

### ❌ No puedo solicitar claves
**Solución**: Necesitas tener un permiso activo para ese video

### ❌ Filtros no funcionan
**Solución**: Verifica que el backend esté corriendo en puerto 5140

---

## ✅ Checklist de Verificación

```
☐ Aplicación corriendo en http://localhost:5140
☐ Puedo acceder a /VideoGrid
☐ Se muestran las tarjetas de videos
☐ Los badges de estado son correctos
☐ Los filtros funcionan
☐ El modal de detalles se abre
☐ Puedo solicitar claves (si tengo permiso)
☐ Las estadísticas son precisas
☐ Los tamaños están formateados (MB, GB)
☐ Las duraciones están formateadas (MM:SS)
```

---

## 🎓 Conceptos Demostrados

### **Backend**
- ✅ Clean Architecture (Servicios, Controllers, DTOs)
- ✅ LINQ con Entity Framework Core
- ✅ Joins complejos (Videos + Permisos)
- ✅ Filtros dinámicos
- ✅ Formateo de datos

### **Frontend**
- ✅ Razor Pages con C#
- ✅ Bootstrap 5 responsive
- ✅ JavaScript async/await
- ✅ Fetch API
- ✅ Modales dinámicos
- ✅ Iconos Bootstrap Icons

### **Seguridad**
- ✅ Autenticación JWT
- ✅ Autorización basada en permisos
- ✅ Validación de estados (Activo/Expirado)
- ✅ Auditoría de accesos

---

**¡Disfruta explorando el Grid de Videos!** 🎉

Para más detalles técnicos, consulta:
- `ENTREGABLE_2.md` - Documentación completa
- `PRUEBAS_ENTREGABLE_2.md` - Guía de pruebas detallada
- `ARQUITECTURA.md` - Diagramas del sistema
