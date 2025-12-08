/**
 * video-decryption.js
 * Maneja la reproducción de videos con descifrado en el servidor
 * usando ChaCha20-Poly1305 (el descifrado ocurre server-side)
 */

// Elementos del DOM
let videoPlayer, loadingOverlay, errorOverlay, loadingStatus, loadingDetails, errorMessage;
let decryptionStatus;
let stepIcons = {};
let stepTexts = {};

// Estado del proceso
let currentStep = 0;

/**
 * Inicializa el reproductor de video (versión simplificada con descifrado server-side)
 */
async function initializeVideoPlayer(videoId, userId) {
    console.log('[VideoPlayer] Inicializando reproductor para video:', videoId);
    
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
        console.log('[VideoPlayer] === INICIANDO PROCESO DE STREAMING ===');
        console.log('[VideoPlayer] Video ID:', videoId);
        console.log('[VideoPlayer] User ID:', userId);
        console.log('[VideoPlayer] Elementos DOM cargados:', {
            videoPlayer: !!videoPlayer,
            loadingOverlay: !!loadingOverlay,
            errorOverlay: !!errorOverlay,
            loadingStatus: !!loadingStatus,
            loadingDetails: !!loadingDetails
        });
        
        // Paso 1: Verificar autenticación
        updateStep(1, 'loading', 'Verificando autenticación...');
        console.log('[VideoPlayer] Paso 1: Verificando token de autenticación...');
        
        const token = localStorage.getItem('jwtToken');
        if (!token) {
            throw new Error('No se encontró token de autenticación');
        }
        updateStep(1, 'success', '✓ Autenticación verificada');
        console.log('[VideoPlayer] Paso 1: ✅ Token encontrado');

        // Paso 2: Configurar URL del video (descifrado server-side)
        updateStep(2, 'loading', 'Configurando streaming...');
        const streamUrl = `/api/videos/${videoId}/stream`;
        console.log('[VideoPlayer] Paso 2: URL de streaming:', streamUrl);
        updateStep(2, 'success', '✓ Streaming configurado');

        // Paso 3: Configurar video player
        updateStep(3, 'loading', 'Preparando reproductor...');
        videoPlayer.src = streamUrl;
        videoPlayer.load();
        updateStep(3, 'success', '✓ Reproductor preparado');
        console.log('[VideoPlayer] Paso 3: ✅ Video src configurado');

        // Paso 4: Esperar carga inicial
        updateStep(4, 'loading', 'Cargando video...');
        await new Promise((resolve, reject) => {
            const loadedHandler = () => {
                console.log('[VideoPlayer] Paso 4: ✅ Metadata de video cargada');
                videoPlayer.removeEventListener('loadedmetadata', loadedHandler);
                videoPlayer.removeEventListener('error', errorHandler);
                resolve();
            };
            
            const errorHandler = (e) => {
                console.error('[VideoPlayer] Paso 4: ❌ Error al cargar video:', e);
                videoPlayer.removeEventListener('loadedmetadata', loadedHandler);
                videoPlayer.removeEventListener('error', errorHandler);
                reject(new Error('Error al cargar el video desde el servidor'));
            };
            
            videoPlayer.addEventListener('loadedmetadata', loadedHandler);
            videoPlayer.addEventListener('error', errorHandler);
        });
        updateStep(4, 'success', '✓ Video cargado');

        // Paso 5: Ocultar overlay y mostrar controles
        updateStep(5, 'loading', 'Finalizando...');
        loadingOverlay.style.display = 'none';
        videoPlayer.controls = true;
        updateStep(5, 'success', '✓ Listo para reproducir');
        console.log('[VideoPlayer] Paso 5: ✅ Video listo para reproducción');

        // Actualizar estado
        decryptionStatus.innerHTML = '<i class="bi bi-check-circle-fill"></i> Listo para reproducir';
        decryptionStatus.className = 'badge bg-success';

        console.log('[VideoPlayer] === PROCESO COMPLETADO EXITOSAMENTE ===');

    } catch (error) {
        console.error('[VideoPlayer] Error en el proceso de streaming:', error);
        updateStep(currentStep, 'error', '✗ Error: ' + error.message);
        showError(error.message || 'Error desconocido durante la carga del video');
    }
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
    textEl.innerHTML = `${stepNumber}. ${text}`;
}

/**
 * Solicita la contraseña al usuario y recupera la clave privada
 */
async function getPrivateKeyWithPassword() {
    console.log('[VideoPlayer] Solicitando clave privada...');
    
    // Usar SecureKeyStorage para acceder a las claves
    const keyStorage = new SecureKeyStorage();
    
    // Verificar si hay clave privada almacenada
    if (!keyStorage.hasPrivateKey()) {
        console.error('[VideoPlayer] No se encontró clave privada en localStorage');
        console.log('[VideoPlayer] Contenido de localStorage:', {
            user_private_key_encrypted: localStorage.getItem('user_private_key_encrypted') ? 'EXISTS' : 'NULL',
            user_public_key: localStorage.getItem('user_public_key') ? 'EXISTS' : 'NULL',
            key_fingerprint: localStorage.getItem('key_fingerprint') ? 'EXISTS' : 'NULL'
        });
        throw new Error('No se encontró clave privada almacenada. Por favor, configura tus claves en /SetupKeys');
    }

    console.log('[VideoPlayer] Clave privada encontrada en localStorage');

    // Solicitar contraseña al usuario mediante modal
    const password = await showPasswordModal();
    if (!password) {
        throw new Error('Se requiere contraseña para descifrar el video');
    }

    loadingDetails.textContent = 'Descifrando clave privada...';

    try {
        console.log('[VideoPlayer] Descifrando clave privada con contraseña proporcionada...');
        
        // Obtener clave privada descifrada usando SecureKeyStorage
        const privateKeyPEM = await keyStorage.getPrivateKey(password);
        
        if (!privateKeyPEM) {
            throw new Error('Error al descifrar la clave privada');
        }
        
        console.log('[VideoPlayer] Clave privada descifrada exitosamente');
        
        // Importar clave privada RSA
        const privateKey = await importRSAPrivateKey(privateKeyPEM);
        
        console.log('[VideoPlayer] Clave RSA importada correctamente');
        
        return privateKey;
    } catch (error) {
        console.error('[VideoPlayer] Error al recuperar clave privada:', error);
        throw new Error('Contraseña incorrecta o error al descifrar la clave privada');
    }
}

/**
 * Obtiene el paquete de claves desde el servidor
 */
async function getKeyPackage(videoId, userId) {
    loadingDetails.textContent = 'Contactando servidor...';

    try {
        console.log('[VideoPlayer] Paso 2: Obteniendo clave pública...');
        console.log('[VideoPlayer] localStorage keys:', Object.keys(localStorage));
        
        // Obtener clave pública del usuario usando SecureKeyStorage
        const keyStorage = new SecureKeyStorage();
        const publicKeyPEM = localStorage.getItem(keyStorage.publicKeyKey); // 'user_public_key'
        
        console.log('[VideoPlayer] Clave pública encontrada:', publicKeyPEM ? '✅ Sí' : '❌ No');
        
        if (!publicKeyPEM) {
            console.error('[VideoPlayer] No se encontró clave pública en localStorage');
            console.log('[VideoPlayer] Claves buscadas:', {
                storageKey: keyStorage.storageKey,
                publicKeyKey: keyStorage.publicKeyKey,
                fingerprintKey: keyStorage.fingerprintKey
            });
            throw new Error('No se encontró clave pública del usuario');
        }

        console.log('[VideoPlayer] Solicitando key package al servidor...');
        console.log('[VideoPlayer] Request:', {
            videoId: videoId,
            publicKeyLength: publicKeyPEM.length
        });

        // Usar handler de VideoPlayer en lugar de API endpoint (usa Session en lugar de JWT)
        const response = await fetch('/VideoPlayer?handler=GetKeyPackage', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: JSON.stringify({
                videoId: videoId,
                userPublicKey: publicKeyPEM
            })
        });

        console.log('[VideoPlayer] Response status:', response.status, response.statusText);

        if (!response.ok) {
            const error = await response.json();
            console.error('[VideoPlayer] ❌ Error del servidor:', error);
            throw new Error(error.message || 'Error al obtener paquete de claves');
        }

        const result = await response.json();
        console.log('[VideoPlayer] ✅ Key package recibido:', {
            success: result.success,
            hasData: !!result.data,
            dataKeys: result.data ? Object.keys(result.data) : []
        });
        
        if (!result.success || !result.data) {
            console.error('[VideoPlayer] ❌ Respuesta inválida:', result);
            throw new Error(result.message || 'Respuesta inválida del servidor');
        }

        console.log('[VideoPlayer] Paso 2: ✅ Key package obtenido del servidor');
        return result.data;
    } catch (error) {
        console.error('[VideoPlayer] ❌ Error al obtener paquete de claves:', error);
        throw new Error('No se pudo obtener el paquete de claves: ' + error.message);
    }
}

/**
 * Descifra la KEK usando la clave privada RSA del usuario
 */
async function decryptKEK(encryptedKEKBase64, privateKey) {
    loadingDetails.textContent = 'Descifrando KEK con RSA-OAEP...';

    try {
        console.log('[VideoPlayer] Paso 3: Descifrando KEK con RSA...');
        console.log('[VideoPlayer] KEK cifrada (Base64 length):', encryptedKEKBase64.length);
        console.log('[VideoPlayer] KEK cifrada (primeros 50 chars):', encryptedKEKBase64.substring(0, 50));
        
        // Limpiar el Base64 (remover espacios en blanco, saltos de línea, etc.)
        const cleanBase64 = encryptedKEKBase64.replace(/\s/g, '');
        console.log('[VideoPlayer] KEK limpia (Base64 length):', cleanBase64.length);
        
        // Convertir KEK cifrada de Base64 a ArrayBuffer
        const encryptedKEK = base64ToArrayBuffer(cleanBase64);
        console.log('[VideoPlayer] KEK cifrada (bytes):', encryptedKEK.byteLength);

        // Descifrar con RSA-OAEP
        const decryptedKEK = await window.crypto.subtle.decrypt(
            {
                name: 'RSA-OAEP'
            },
            privateKey,
            encryptedKEK
        );

        console.log('[VideoPlayer] KEK descifrada (bytes):', decryptedKEK.byteLength);
        
        if (decryptedKEK.byteLength !== 32) {
            throw new Error(`KEK inválida: se esperaban 32 bytes, se obtuvieron ${decryptedKEK.byteLength}`);
        }

        return decryptedKEK;
    } catch (error) {
        console.error('[VideoPlayer] Error al descifrar KEK:', error);
        throw new Error('Error al descifrar KEK con clave privada RSA: ' + error.message);
    }
}

/**
 * Obtiene el video cifrado desde el servidor
 */
async function getEncryptedVideo(videoId, userId) {
    loadingDetails.textContent = 'Descargando video...';

    try {
        console.log('[VideoPlayer] Paso 4: Descargando video cifrado...');
        console.log('[VideoPlayer] VideoId:', videoId, 'UserId:', userId);
        
        const response = await fetch(`/api/videostreaming/stream/${videoId}?userId=${userId}`, {
            method: 'GET'
        });

        console.log('[VideoPlayer] Response status:', response.status);

        if (!response.ok) {
            throw new Error('Error al obtener video cifrado del servidor');
        }

        const result = await response.json();
        console.log('[VideoPlayer] Video response:', {
            success: result.success,
            hasData: !!result.data,
            dataKeys: result.data ? Object.keys(result.data) : []
        });
        
        if (!result.success || !result.data) {
            throw new Error(result.message || 'Respuesta inválida del servidor');
        }

        // El ciphertext puede venir como "ciphertext" o "Ciphertext" (ASP.NET camelCase)
        const ciphertextBase64 = result.data.ciphertext || result.data.Ciphertext;
        
        if (!ciphertextBase64) {
            throw new Error('Ciphertext faltante en la respuesta del servidor');
        }

        console.log('[VideoPlayer] Ciphertext (Base64 length):', ciphertextBase64.length);
        
        // Convertir ciphertext de Base64 a ArrayBuffer
        const ciphertextBuffer = base64ToArrayBuffer(ciphertextBase64);
        console.log('[VideoPlayer] Ciphertext (bytes):', ciphertextBuffer.byteLength);
        
        return ciphertextBuffer;
    } catch (error) {
        console.error('[VideoPlayer] ❌ Error al obtener video cifrado:', error);
        throw new Error('No se pudo descargar el video: ' + error.message);
    }
}

/**
 * Descifra el video usando ChaCha20-Poly1305 con libsodium.js
 */
async function decryptVideoWithChaCha20(ciphertext, kek, nonce, authTag) {
    loadingDetails.textContent = 'Descifrando contenido...';

    try {
        console.log('[VideoPlayer] Paso 5: Descifrando video con ChaCha20-Poly1305...');
        console.log('[VideoPlayer] KEK length:', kek.byteLength);
        console.log('[VideoPlayer] Nonce length:', nonce.byteLength);
        console.log('[VideoPlayer] AuthTag length:', authTag.byteLength);
        console.log('[VideoPlayer] Ciphertext length:', ciphertext.byteLength);

        // Convertir ArrayBuffers a Uint8Array
        const kekArray = new Uint8Array(kek);
        const nonceArray = new Uint8Array(nonce);
        const ciphertextArray = new Uint8Array(ciphertext);
        const authTagArray = new Uint8Array(authTag);

        // En ChaCha20-Poly1305, el authTag debe ir concatenado al final del ciphertext
        const combinedCiphertext = new Uint8Array(ciphertextArray.length + authTagArray.length);
        combinedCiphertext.set(ciphertextArray, 0);
        combinedCiphertext.set(authTagArray, ciphertextArray.length);

        console.log('[VideoPlayer] Combined ciphertext+authTag length:', combinedCiphertext.length);

        // Descifrar con libsodium ChaCha20-Poly1305
        const decrypted = window.sodium.crypto_aead_chacha20poly1305_ietf_decrypt(
            null, // nsec (no se usa, pasar null)
            combinedCiphertext,
            null, // additional data (no usamos)
            nonceArray,
            kekArray
        );

        console.log('[VideoPlayer] ✓ Video descifrado exitosamente:', decrypted.length, 'bytes');
        
        // Convertir Uint8Array a ArrayBuffer
        return decrypted.buffer;
    } catch (error) {
        console.error('[VideoPlayer] ❌ Error al descifrar video:', error);
        throw new Error('Error al descifrar el contenido del video: ' + error.message);
    }
}

/**
 * Reproduce el video descifrado
 */
async function playDecryptedVideo(videoData) {
    try {
        loadingStatus.textContent = 'Preparando reproducción...';
        loadingDetails.textContent = 'Creando blob de video...';

        // Crear Blob del video descifrado
        const videoBlob = new Blob([videoData], { type: 'video/mp4' });
        const videoUrl = URL.createObjectURL(videoBlob);

        // Configurar reproductor
        videoPlayer.src = videoUrl;
        videoPlayer.style.display = 'block';
        loadingOverlay.classList.add('d-none');

        console.log('[VideoPlayer] Video listo para reproducir');

        // Liberar URL cuando termine
        videoPlayer.addEventListener('ended', () => {
            URL.revokeObjectURL(videoUrl);
        });

    } catch (error) {
        console.error('[VideoPlayer] Error al preparar video para reproducción:', error);
        throw new Error('No se pudo preparar el video para reproducción: ' + error.message);
    }
}

/**
 * Muestra un error en la interfaz
 */
function showError(message) {
    loadingOverlay.classList.add('d-none');
    errorOverlay.classList.remove('d-none');
    errorOverlay.classList.add('d-flex');
    errorMessage.textContent = message;
    
    decryptionStatus.innerHTML = '<i class="bi bi-x-circle-fill"></i> Error';
    decryptionStatus.className = 'badge bg-danger';
}

/**
 * Importa una clave privada RSA desde formato PEM
 */
async function importRSAPrivateKey(pemKey) {
    try {
        // Remover encabezados y footers del PEM
        const pemHeader = '-----BEGIN PRIVATE KEY-----';
        const pemFooter = '-----END PRIVATE KEY-----';
        const pemContents = pemKey.replace(pemHeader, '').replace(pemFooter, '').replace(/\s/g, '');
        
        // Decodificar Base64
        const binaryDer = base64ToArrayBuffer(pemContents);
        
        // Importar clave
        const key = await window.crypto.subtle.importKey(
            'pkcs8',
            binaryDer,
            {
                name: 'RSA-OAEP',
                hash: 'SHA-256'
            },
            false,
            ['decrypt']
        );
        
        return key;
    } catch (error) {
        console.error('[VideoPlayer] Error al importar clave RSA:', error);
        throw new Error('No se pudo importar la clave privada RSA');
    }
}

/**
 * Convierte Base64 a ArrayBuffer
 */
function base64ToArrayBuffer(base64) {
    try {
        // Validar que sea Base64 válido
        if (!base64 || typeof base64 !== 'string') {
            throw new Error('Input inválido: debe ser un string Base64');
        }
        
        // Limpiar espacios en blanco
        const cleanBase64 = base64.trim().replace(/\s/g, '');
        
        // Validar longitud (debe ser múltiplo de 4 para Base64 válido)
        if (cleanBase64.length % 4 !== 0) {
            console.warn('[base64ToArrayBuffer] Longitud no es múltiplo de 4:', cleanBase64.length);
        }
        
        const binaryString = window.atob(cleanBase64);
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        return bytes.buffer;
    } catch (error) {
        console.error('[base64ToArrayBuffer] Error al decodificar Base64:', error);
        console.error('[base64ToArrayBuffer] Input recibido:', base64 ? base64.substring(0, 100) : 'null');
        throw error;
    }
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
 * Muestra modal para solicitar contraseña al usuario
 * @returns {Promise<string|null>} - Contraseña ingresada o null si se cancela
 */
function showPasswordModal() {
    return new Promise((resolve) => {
        console.log('[Modal] ========================================');
        console.log('[Modal] INICIANDO showPasswordModal()');
        console.log('[Modal] ========================================');
        
        // OCULTAR LOADING OVERLAY TEMPORALMENTE
        console.log('[Modal] Ocultando loading overlay...');
        if (loadingOverlay) {
            loadingOverlay.style.display = 'none';
            console.log('[Modal] ✅ Loading overlay ocultado');
        } else {
            console.warn('[Modal] ⚠️ loadingOverlay no encontrado');
        }
        
        console.log('[Modal] Creando HTML del modal...');
        
        // Crear modal HTML
        const modalHtml = `
            <div class="modal fade" id="passwordModal" tabindex="-1" data-bs-backdrop="static" data-bs-keyboard="false">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content">
                        <div class="modal-header bg-primary text-white">
                            <h5 class="modal-title">
                                <i class="bi bi-shield-lock"></i> Contraseña Requerida
                            </h5>
                        </div>
                        <div class="modal-body">
                            <p>Para descifrar el video, necesitas ingresar la contraseña que usaste para cifrar tu clave privada.</p>
                            <div class="mb-3">
                                <label for="passwordInput" class="form-label">Contraseña:</label>
                                <div class="input-group">
                                    <span class="input-group-text"><i class="bi bi-key"></i></span>
                                    <input type="password" class="form-control" id="passwordInput" 
                                           placeholder="Ingresa tu contraseña" autofocus>
                                    <button class="btn btn-outline-secondary" type="button" id="togglePassword">
                                        <i class="bi bi-eye" id="toggleIcon"></i>
                                    </button>
                                </div>
                            </div>
                            <div class="alert alert-info mb-0">
                                <i class="bi bi-info-circle"></i>
                                <small>Esta es la contraseña que estableciste cuando generaste tus claves en la configuración.</small>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" id="cancelBtn">Cancelar</button>
                            <button type="button" class="btn btn-primary" id="confirmBtn">
                                <i class="bi bi-unlock"></i> Descifrar
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Insertar modal en el DOM
        console.log('[Modal] Insertando modal en el DOM...');
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        console.log('[Modal] ✅ Modal HTML insertado');
        
        console.log('[Modal] Obteniendo referencias a elementos...');
        const modalElement = document.getElementById('passwordModal');
        const passwordInput = document.getElementById('passwordInput');
        const togglePassword = document.getElementById('togglePassword');
        const toggleIcon = document.getElementById('toggleIcon');
        const confirmBtn = document.getElementById('confirmBtn');
        const cancelBtn = document.getElementById('cancelBtn');
        
        console.log('[Modal] Referencias obtenidas:', {
            modalElement: !!modalElement,
            passwordInput: !!passwordInput,
            confirmBtn: !!confirmBtn,
            cancelBtn: !!cancelBtn,
            bootstrap: !!bootstrap
        });

        if (!modalElement) {
            console.error('[Modal] ❌ No se pudo obtener el elemento modal');
            resolve(null);
            return;
        }

        console.log('[Modal] Creando instancia de Bootstrap Modal...');
        const modal = new bootstrap.Modal(modalElement, {
            backdrop: 'static',
            keyboard: false
        });
        console.log('[Modal] ✅ Instancia Bootstrap Modal creada');

        // Mostrar modal
        console.log('[Modal] Llamando a modal.show()...');
        try {
            modal.show();
            console.log('[Modal] ✅ modal.show() ejecutado exitosamente');
            console.log('[Modal] Esperando que el modal aparezca en pantalla...');
        } catch (error) {
            console.error('[Modal] ❌ Error al ejecutar modal.show():', error);
            resolve(null);
            return;
        }

        // Toggle para mostrar/ocultar contraseña
        togglePassword.addEventListener('click', () => {
            const type = passwordInput.type === 'password' ? 'text' : 'password';
            passwordInput.type = type;
            toggleIcon.className = type === 'password' ? 'bi bi-eye' : 'bi bi-eye-slash';
        });

        // Confirmar con Enter
        passwordInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                confirmBtn.click();
            }
        });

        // Botón confirmar
        confirmBtn.addEventListener('click', () => {
            console.log('[Modal] Botón Confirmar presionado');
            const password = passwordInput.value;
            console.log('[Modal] Contraseña ingresada:', password ? '***' : '(vacía)');
            console.log('[Modal] Ocultando modal...');
            modal.hide();
            console.log('[Modal] Removiendo modal del DOM...');
            modalElement.remove();
            console.log('[Modal] Restaurando loading overlay...');
            if (loadingOverlay) {
                loadingOverlay.style.display = '';
            }
            console.log('[Modal] ✅ Resolviendo Promise con contraseña');
            resolve(password || null);
        });

        // Botón cancelar
        cancelBtn.addEventListener('click', () => {
            console.log('[Modal] Botón Cancelar presionado');
            console.log('[Modal] Ocultando modal...');
            modal.hide();
            console.log('[Modal] Removiendo modal del DOM...');
            modalElement.remove();
            console.log('[Modal] Restaurando loading overlay...');
            if (loadingOverlay) {
                loadingOverlay.style.display = '';
            }
            console.log('[Modal] ❌ Resolviendo Promise con null (cancelado)');
            resolve(null);
        });

        // Limpiar al cerrar
        modalElement.addEventListener('hidden.bs.modal', () => {
            console.log('[Modal] Evento hidden.bs.modal disparado');
            if (modalElement.parentNode) {
                modalElement.remove();
                console.log('[Modal] Modal removido del DOM');
            }
        });

        console.log('[Modal] ✅ Event listeners configurados');
        console.log('[Modal] ========================================');
        console.log('[Modal] Modal debería estar VISIBLE ahora');
        console.log('[Modal] ========================================');
    });
}
