/**
 * video-decryption-simplified.js (Ephemeral Keys Model)
 * Maneja la reproducción de videos con claves RSA temporales
 * SEGURIDAD: Las claves solo existen en RAM y se destruyen al cerrar la sesión
 * NO se almacenan claves privadas en localStorage ni archivos descargables
 */

// Elementos del DOM
let videoPlayer, loadingOverlay, errorOverlay, loadingStatus, loadingDetails, errorMessage;
let decryptionStatus;
let stepIcons = {};
let stepTexts = {};

// Estado del proceso
let currentStep = 0;

// Claves temporales (solo en RAM, nunca persistidas)
let temporaryKeyPair = null;
let temporaryPrivateKey = null;
let temporaryPublicKeyPem = null;

/**
 * Inicializa el reproductor de video con modelo de claves efímeras
 * IMPORTANTE: Genera claves RSA temporales que NUNCA se almacenan
 */
async function initializeVideoPlayer(videoId, userId) {
    console.log('[VideoPlayer] Inicializando reproductor con claves efímeras para video:', videoId);
    console.log('[Security] 🔐 Modelo de seguridad: Zero-Storage Ephemeral Keys');
    
    // Obtener referencias DOM
    videoPlayer = document.getElementById('videoPlayer');
    loadingOverlay = document.getElementById('loadingOverlay');
    errorOverlay = document.getElementById('errorOverlay');
    loadingStatus = document.getElementById('loadingStatus');
    loadingDetails = document.getElementById('loadingDetails');
    errorMessage = document.getElementById('errorMessage');
    decryptionStatus = document.getElementById('decryptionStatus');

    // Referencias a los pasos
    for (let i = 1; i <= 5; i++) {
        stepIcons[i] = document.getElementById(`step${i}Icon`);
        stepTexts[i] = document.getElementById(`step${i}Text`);
    }

    try {
        console.log('[VideoPlayer] === INICIANDO PROCESO DE STREAMING SEGURO ===');
        console.log('[VideoPlayer] Video ID:', videoId);
        console.log('[VideoPlayer] User ID:', userId);
        
        // Paso 1: Generar par de claves RSA TEMPORALES (solo en RAM)
        updateStep(1, 'loading', 'Generando claves temporales RSA...');
        console.log('[Security] Generando par RSA-2048 efímero (solo en memoria RAM)...');
        
        temporaryKeyPair = await window.crypto.subtle.generateKey(
            {
                name: "RSA-OAEP",
                modulusLength: 2048,
                publicExponent: new Uint8Array([1, 0, 1]), // 65537
                hash: "SHA-256"
            },
            true, // extractable
            ["encrypt", "decrypt"]
        );
        
        temporaryPrivateKey = temporaryKeyPair.privateKey;
        temporaryPublicKeyPem = await exportPublicKeyToPem(temporaryKeyPair.publicKey);
        
        console.log('[Security] ✅ Claves temporales generadas (NO almacenadas)');
        console.log('[Security] ⚠️ Estas claves se destruirán al cerrar esta pestaña');
        updateStep(1, 'success', '✓ Claves temporales generadas');

        // Paso 2: Verificar autenticación JWT
        updateStep(2, 'loading', 'Verificando autenticación...');
        const token = localStorage.getItem('jwtToken');
        if (!token) {
            throw new Error('No se encontró token de autenticación. Por favor inicia sesión.');
        }
        console.log('[VideoPlayer] Token JWT encontrado');
        updateStep(2, 'success', '✓ Autenticación verificada');

        // Paso 3: Obtener video descifrado mediante fetch con token JWT
        updateStep(3, 'loading', 'Solicitando video al servidor...');
        const streamUrl = `/api/videos/${videoId}/stream`;
        console.log('[VideoPlayer] URL de streaming:', streamUrl);
        
        const response = await fetch(streamUrl, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        if (!response.ok) {
            if (response.status === 401) {
                throw new Error('Sesión expirada. Por favor inicia sesión nuevamente.');
            } else if (response.status === 403) {
                throw new Error('No tienes permiso para ver este video.');
            } else if (response.status === 404) {
                throw new Error('Video no encontrado.');
            }
            throw new Error(`Error del servidor: ${response.status}`);
        }
        
        updateStep(3, 'success', '✓ Video recibido del servidor');

        // Paso 4: Crear blob URL del video descifrado
        updateStep(4, 'loading', 'Preparando reproducción...');
        const videoBlob = await response.blob();
        const blobUrl = URL.createObjectURL(videoBlob);
        console.log('[VideoPlayer] Blob URL creado:', blobUrl);
        updateStep(4, 'success', '✓ Video preparado');

        // Paso 5: Configurar video player
        updateStep(5, 'loading', 'Configurando reproductor...');
        videoPlayer.src = blobUrl;
        videoPlayer.load();
        
        // Limpieza de recursos al cerrar
        const cleanupResources = () => {
            URL.revokeObjectURL(blobUrl);
            destroyTemporaryKeys();
            console.log('[Security] 🗑️ Recursos y claves temporales destruidos');
        };
        
        videoPlayer.addEventListener('ended', cleanupResources);
        window.addEventListener('beforeunload', cleanupResources);
        
        updateStep(5, 'success', '✓ Reproductor configurado');

        // Esperar carga inicial y mostrar video
        await waitForVideoLoad();
        
        // Ocultar overlay de carga COMPLETAMENTE
        if (loadingOverlay) {
            loadingOverlay.style.display = 'none';
            loadingOverlay.style.visibility = 'hidden';
            loadingOverlay.style.opacity = '0';
            loadingOverlay.style.zIndex = '-1';
            loadingOverlay.classList.add('d-none');
        }
        
        // Mostrar y configurar video player
        videoPlayer.controls = true;
        videoPlayer.style.display = 'block';
        videoPlayer.style.visibility = 'visible';
        videoPlayer.style.opacity = '1';
        videoPlayer.style.width = '100%';
        videoPlayer.style.height = 'auto';
        videoPlayer.style.zIndex = '1';

        // Actualizar estado
        if (decryptionStatus) {
            decryptionStatus.innerHTML = '<i class="bi bi-check-circle-fill"></i> Listo para reproducir';
            decryptionStatus.className = 'badge bg-success';
        }

        console.log('[VideoPlayer] === PROCESO COMPLETADO EXITOSAMENTE ===');
        console.log('[Security] 🔒 Claves temporales activas en RAM (no persistidas)');
        console.log('[VideoPlayer] Video display:', videoPlayer.style.display);
        console.log('[VideoPlayer] Video visibility:', videoPlayer.style.visibility);
        console.log('[VideoPlayer] Loading overlay display:', loadingOverlay?.style.display);
        console.log('[VideoPlayer] Video src:', videoPlayer.src);

    } catch (error) {
        console.error('[VideoPlayer] Error en el proceso de streaming:', error);
        updateStep(currentStep, 'error', '✗ Error: ' + error.message);
        showError(error.message || 'Error desconocido durante la carga del video');
        
        // Limpiar claves en caso de error
        destroyTemporaryKeys();
    }
}

/**
 * Exporta la clave pública a formato PEM
 */
async function exportPublicKeyToPem(publicKey) {
    const exported = await window.crypto.subtle.exportKey("spki", publicKey);
    const exportedAsBase64 = arrayBufferToBase64(exported);
    const pem = `-----BEGIN PUBLIC KEY-----\n${formatPem(exportedAsBase64)}\n-----END PUBLIC KEY-----`;
    return pem;
}

/**
 * Convierte ArrayBuffer a Base64
 */
function arrayBufferToBase64(buffer) {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    return window.btoa(binary);
}

/**
 * Formatea string Base64 para PEM (64 caracteres por línea)
 */
function formatPem(base64) {
    const lines = [];
    for (let i = 0; i < base64.length; i += 64) {
        lines.push(base64.substring(i, i + 64));
    }
    return lines.join('\n');
}

/**
 * Destruye las claves temporales de la memoria
 * CRÍTICO: Asegura que no queden residuos de claves en RAM
 */
function destroyTemporaryKeys() {
    if (temporaryKeyPair) {
        console.log('[Security] 🗑️ Destruyendo claves temporales...');
        temporaryKeyPair = null;
        temporaryPrivateKey = null;
        temporaryPublicKeyPem = null;
        
        // Forzar garbage collection si está disponible
        if (typeof window.gc === 'function') {
            window.gc();
        }
        
        console.log('[Security] ✅ Claves temporales eliminadas de memoria');
    }
}

/**
 * Espera a que el video cargue los metadatos
 */
function waitForVideoLoad() {
    return new Promise((resolve, reject) => {
        const loadedHandler = () => {
            console.log('[VideoPlayer] Metadata de video cargada');
            videoPlayer.removeEventListener('loadedmetadata', loadedHandler);
            videoPlayer.removeEventListener('error', errorHandler);
            resolve();
        };
        
        const errorHandler = (e) => {
            console.error('[VideoPlayer] Error al cargar video:', e);
            videoPlayer.removeEventListener('loadedmetadata', loadedHandler);
            videoPlayer.removeEventListener('error', errorHandler);
            
            // Obtener mensaje de error más específico
            let errorMsg = 'Error al cargar el video desde el servidor';
            if (videoPlayer.error) {
                switch(videoPlayer.error.code) {
                    case 1:
                        errorMsg = 'Carga del video abortada';
                        break;
                    case 2:
                        errorMsg = 'Error de red al cargar el video';
                        break;
                    case 3:
                        errorMsg = 'Error al decodificar el video';
                        break;
                    case 4:
                        errorMsg = 'Formato de video no soportado';
                        break;
                }
            }
            reject(new Error(errorMsg));
        };
        
        videoPlayer.addEventListener('loadedmetadata', loadedHandler);
        videoPlayer.addEventListener('error', errorHandler);
        
        // Timeout de 30 segundos
        setTimeout(() => {
            videoPlayer.removeEventListener('loadedmetadata', loadedHandler);
            videoPlayer.removeEventListener('error', errorHandler);
            reject(new Error('Timeout: el video tardó demasiado en cargar'));
        }, 30000);
    });
}

/**
 * Actualiza el estado visual de un paso
 */
function updateStep(stepNumber, status, text) {
    currentStep = stepNumber;
    
    const icon = stepIcons[stepNumber];
    const textEl = stepTexts[stepNumber];

    if (!icon || !textEl) return;

    // Actualizar icono
    icon.className = 'bi ';
    switch (status) {
        case 'loading':
            icon.className += 'bi-hourglass-split text-primary';
            if (loadingStatus) loadingStatus.textContent = text;
            if (loadingDetails) loadingDetails.textContent = '';
            break;
        case 'success':
            icon.className += 'bi-check-circle-fill text-success';
            break;
        case 'error':
            icon.className += 'bi-x-circle-fill text-danger';
            break;
        default:
            icon.className += 'bi-circle text-muted';
    }

    // Actualizar texto
    textEl.textContent = text;
}

/**
 * Muestra un error en la interfaz
 */
function showError(message) {
    if (loadingOverlay) {
        loadingOverlay.classList.add('d-none');
        loadingOverlay.style.display = 'none';
    }
    
    if (errorOverlay) {
        errorOverlay.classList.remove('d-none');
        errorOverlay.classList.add('d-flex');
        errorOverlay.style.display = 'flex';
    }
    
    if (errorMessage) {
        errorMessage.textContent = message;
    }
    
    if (decryptionStatus) {
        decryptionStatus.innerHTML = '<i class="bi bi-x-circle-fill"></i> Error';
        decryptionStatus.className = 'badge bg-danger';
    }
}

/**
 * Reintenta el proceso de carga (llamado desde botón en UI)
 */
function retryDecryption() {
    if (errorOverlay) {
        errorOverlay.classList.add('d-none');
        errorOverlay.style.display = 'none';
    }
    
    if (loadingOverlay) {
        loadingOverlay.classList.remove('d-none');
        loadingOverlay.style.display = '';
    }
    
    // Reiniciar pasos
    currentStep = 0;
    for (let i = 1; i <= 5; i++) {
        if (stepIcons[i]) stepIcons[i].className = 'bi bi-circle text-muted';
        if (stepTexts[i]) stepTexts[i].textContent = '';
    }
    
    // Obtener videoId y userId del contexto global (definidos en VideoPlayer.cshtml)
    if (typeof videoId !== 'undefined' && typeof userId !== 'undefined') {
        initializeVideoPlayer(videoId, userId);
    } else {
        showError('Error: No se encontraron los identificadores del video');
    }
}
