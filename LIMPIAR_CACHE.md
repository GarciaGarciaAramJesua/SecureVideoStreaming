# 🧹 Guía para Limpiar Caché del Navegador

## 🔴 Problema Detectado

**Error**: `Uncaught ReferenceError: SecureKeyStorage is not defined`

**Causa**: Tu navegador está usando una versión antigua en caché de los archivos JavaScript que todavía contenía referencias a `SecureKeyStorage` (clase eliminada en el modelo de claves efímeras).

---

## ✅ Solución Implementada

He agregado versionado a todos los archivos JavaScript críticos:
- `rsa-crypto.js?v=2.0`
- `register.js?v=2.0`
- `video-decryption-simplified.js?v=2.0`

---

## 🔧 Instrucciones para Limpiar el Caché

### Opción 1: Recarga Forzada (Más Rápido) ⚡

1. **Abre la página de registro** en tu navegador
2. Presiona las siguientes teclas:

   | Navegador | Combinación de Teclas |
   |-----------|----------------------|
   | **Chrome/Edge** | `Ctrl + Shift + R` (Windows/Linux)<br>`Cmd + Shift + R` (Mac) |
   | **Firefox** | `Ctrl + Shift + R` (Windows/Linux)<br>`Cmd + Shift + R` (Mac) |
   | **Safari** | `Cmd + Option + R` |

3. **Verifica en la consola** (F12):
   ```
   ✅ Debe aparecer: "Página de registro cargada"
   ✅ Debe aparecer: "[Security] 🔐 Modelo de seguridad: Ephemeral Keys"
   ❌ NO debe aparecer: "ReferenceError: SecureKeyStorage"
   ```

---

### Opción 2: Limpiar Caché Completo del Navegador 🗑️

#### **Google Chrome / Microsoft Edge**
1. Presiona `Ctrl + Shift + Delete` (Windows/Linux) o `Cmd + Shift + Delete` (Mac)
2. Selecciona:
   - ☑️ **Imágenes y archivos en caché**
   - Intervalo de tiempo: **Desde siempre** o **Última hora**
3. Clic en **Borrar datos**
4. Cierra y vuelve a abrir el navegador

#### **Mozilla Firefox**
1. Presiona `Ctrl + Shift + Delete` (Windows/Linux) o `Cmd + Shift + Delete` (Mac)
2. Selecciona:
   - ☑️ **Caché**
   - Intervalo de tiempo: **Todo**
3. Clic en **Limpiar ahora**
4. Cierra y vuelve a abrir el navegador

#### **Safari (Mac)**
1. Ve a **Safari** → **Preferencias** → **Avanzado**
2. Activa "Mostrar menú Desarrollo"
3. **Menú Desarrollo** → **Vaciar cachés**
4. Cierra y vuelve a abrir Safari

---

### Opción 3: Modo Incógnito/Privado (Para Pruebas) 🕵️

1. Abre una ventana de navegación privada:
   - **Chrome/Edge**: `Ctrl + Shift + N`
   - **Firefox**: `Ctrl + Shift + P`
   - **Safari**: `Cmd + Shift + N`

2. Navega a: `https://localhost:7217/Register`

3. El navegador cargará todos los archivos sin caché

---

## 📋 Checklist Post-Limpieza

Después de limpiar el caché, verifica que TODO funcione correctamente:

### ✅ Página de Registro
1. Abre DevTools (F12) → Pestaña **Console**
2. Recarga la página
3. Verifica mensajes:
   ```
   ✅ "📝 Página de registro cargada"
   ✅ "[Security] 🔐 Modelo de seguridad: Ephemeral Keys (sin almacenamiento)"
   ```
4. Selecciona "Usuario (Ver videos)"
5. Debe aparecer:
   ```
   "Seguridad Mejorada: Al registrarte como Usuario (Consumidor)..."
   "¡Sin descargas ni respaldos necesarios!"
   ```

### ✅ Registro de Nuevo Usuario
1. Llena el formulario de registro
2. Tipo: **Usuario (Ver videos)**
3. Clic en **Registrarse**
4. En consola debe aparecer:
   ```
   ✅ "[Security] 🔐 Generando clave pública RSA para el servidor..."
   ✅ "[Security] ⚠️ NO se generará ni almacenará clave privada"
   ✅ "[Security] ✅ Clave pública generada (la privada se descarta inmediatamente)"
   ```
5. **NO debe haber ningún error** de `SecureKeyStorage`

### ✅ Reproductor de Video
1. Inicia sesión con el usuario creado
2. Accede a un video con permiso
3. En consola debe aparecer:
   ```
   ✅ "[Ephemeral Keys] 🔐 Generando claves RSA temporales en memoria..."
   ✅ "[Ephemeral Keys] ✅ Claves temporales generadas exitosamente"
   ✅ "Claves temporales destruidas y eliminadas de memoria"
   ```

---

## 🚀 Reiniciar la Aplicación

Después de limpiar el caché del navegador, **reinicia también el servidor**:

```bash
# En el terminal de VS Code:
cd SecureVideoStreaming.API
dotnet run
```

O presiona `Ctrl + C` y vuelve a ejecutar.

---

## 📝 Archivos Modificados (Para Referencia)

Los siguientes archivos ya fueron actualizados con versionado `?v=2.0`:

1. ✅ `Register.cshtml`
   - `rsa-crypto.js?v=2.0`
   - `register.js?v=2.0`

2. ✅ `VideoPlayer.cshtml`
   - `video-decryption-simplified.js?v=2.0`

---

## ⚠️ Si el Error Persiste

Si después de limpiar el caché el error continúa:

### 1. Verifica el Código del Navegador
Abre DevTools (F12) → Pestaña **Sources**:
- Busca: `SecureVideoStreaming.API/wwwroot/js/register.js`
- Verifica la primera línea del comentario:
  ```javascript
  /**
   * Register Page Script - Gestión de registro SIMPLIFICADA (Modelo de Claves Efímeras)
  ```
- Si ves código diferente, el navegador sigue usando caché antiguo

### 2. Desactiva el Caché en DevTools
1. Abre DevTools (F12)
2. Ve a **Network** (Red)
3. Activa la opción: ☑️ **Disable cache**
4. Mantén DevTools abierto mientras pruebas

### 3. Verifica la Versión del Archivo en el Servidor
```bash
# En terminal:
cat SecureVideoStreaming.API/wwwroot/js/register.js | head -5
```

Debe mostrar:
```javascript
/**
 * Register Page Script - Gestión de registro SIMPLIFICADA (Modelo de Claves Efímeras)
 * SEGURIDAD: NO se generan ni almacenan claves privadas durante el registro
```

---

## ✅ Confirmación Final

Una vez que todo funcione:

1. ✅ NO hay errores en consola durante registro
2. ✅ NO aparece modal de descarga de claves
3. ✅ El mensaje de "Seguridad Mejorada" aparece correctamente
4. ✅ Los videos se reproducen usando claves efímeras
5. ✅ Mensaje de destrucción de claves aparece al cerrar video

---

## 📞 Si Necesitas Ayuda

Si el problema persiste después de seguir todos estos pasos:
1. Captura de pantalla del error en consola (F12)
2. Captura del código fuente en DevTools (Sources → register.js)
3. Verifica la versión del navegador

---

## 🎯 Resumen Rápido

```bash
# Método más rápido:
1. Presiona: Ctrl + Shift + R (Windows) o Cmd + Shift + R (Mac)
2. Abre DevTools (F12) y verifica consola
3. Registra un nuevo usuario tipo "Usuario"
4. ✅ No debe haber error de SecureKeyStorage
```

---

**Última actualización**: 8 de diciembre de 2025  
**Versión del sistema**: 2.0 (Modelo de Claves Efímeras)
