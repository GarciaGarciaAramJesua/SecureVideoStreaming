/**
 * video-decryption-e2e-chacha20.js
 * Descifrado E2E usando @stablelib/chacha20poly1305
 * Compatible con ChaCha20-Poly1305 del backend
 */

// Importar desde CDN (Skypack)
import { ChaCha20Poly1305 } from 'https://cdn.skypack.dev/@stablelib/chacha20poly1305';

class E2EVideoPlayerChaCha20 {
    constructor(videoId, userId) {
        this.videoId = videoId;
        this.userId = userId;
        this.ephemeralKeyPair = null;
        this.videoElement = null;
        this.progressCallback = null;
    }

    /**
     * Genera un par de claves RSA efímeras para recibir la KEK de forma segura
     */
    async generateEphemeralKeyPair() {
        console.log('[E2E] Generando claves RSA efímeras...');
        this.ephemeralKeyPair = await window.crypto.subtle.generateKey(
            {
                name: 'RSA-OAEP',
                modulusLength: 2048,
                publicExponent: new Uint8Array([1, 0, 1]),
                hash: 'SHA-256'
            },
            true, // extractable
            ['encrypt', 'decrypt']
        );
        console.log('[E2E] ✓ Claves efímeras generadas');
    }

    /**
     * Exporta la clave pública en formato SPKI Base64
     */
    async exportPublicKey() {
        const exported = await window.crypto.subtle.exportKey(
            'spki',
            this.ephemeralKeyPair.publicKey
        );
        const exportedAsBase64 = btoa(String.fromCharCode(...new Uint8Array(exported)));
        console.log('[E2E] Clave pública exportada (SPKI Base64):', exportedAsBase64.substring(0, 50) + '...');
        return exportedAsBase64;
    }

    /**
     * Descifra la KEK usando la clave privada efímera
     */
    async decryptKEK(encryptedKEKBase64) {
        console.log('[E2E] Descifrando KEK con clave privada efímera...');
        const encryptedKEK = Uint8Array.from(atob(encryptedKEKBase64), c => c.charCodeAt(0));
        
        const kek = await window.crypto.subtle.decrypt(
            {
                name: 'RSA-OAEP'
            },
            this.ephemeralKeyPair.privateKey,
            encryptedKEK
        );
        
        console.log('[E2E] ✓ KEK descifrada exitosamente');
        return new Uint8Array(kek);
    }

    /**
     * Descifra el video usando ChaCha20-Poly1305 con @stablelib
     */
    async decryptVideo(encryptedVideoBase64, kek, nonceBase64, authTagBase64) {
        console.log('[E2E] Descifrando video con ChaCha20-Poly1305...');
        
        // Convertir de Base64 a Uint8Array
        const encryptedVideo = Uint8Array.from(atob(encryptedVideoBase64), c => c.charCodeAt(0));
        const nonce = Uint8Array.from(atob(nonceBase64), c => c.charCodeAt(0));
        const authTag = Uint8Array.from(atob(authTagBase64), c => c.charCodeAt(0));
        
        console.log('[E2E] Tamaños:', {
            encryptedVideo: encryptedVideo.length,
            kek: kek.length,
            nonce: nonce.length,
            authTag: authTag.length
        });

        // Crear instancia de ChaCha20-Poly1305
        const cipher = new ChaCha20Poly1305(kek);
        
        // Combinar video cifrado + authTag (formato estándar de AEAD)
        const ciphertext = new Uint8Array(encryptedVideo.length + authTag.length);
        ciphertext.set(encryptedVideo, 0);
        ciphertext.set(authTag, encryptedVideo.length);
        
        // Descifrar
        const decrypted = cipher.open(nonce, ciphertext);
        
        if (decrypted === null) {
            throw new Error('Fallo de autenticación: AuthTag inválido');
        }
        
        console.log('[E2E] ✓ Video descifrado exitosamente:', decrypted.length, 'bytes');
        return decrypted;
    }

    /**
     * Flujo completo E2E
     */
    async loadAndDecryptVideo() {
        try {
            // Paso 1: Generar claves RSA efímeras
            this.updateProgress('Generando claves efímeras...', 10);
            await this.generateEphemeralKeyPair();
            
            // Paso 2: Exportar clave pública
            this.updateProgress('Exportando clave pública...', 20);
            const publicKeyBase64 = await this.exportPublicKey();
            
            // Paso 3: Solicitar video cifrado al servidor
            this.updateProgress('Solicitando video al servidor...', 30);
            const token = localStorage.getItem('token');
            
            const response = await fetch(`/api/videos/${this.videoId}/stream-e2e`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    clientPublicKey: publicKeyBase64,
                    userId: this.userId
                })
            });
            
            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Error al obtener video');
            }
            
            const data = await response.json();
            console.log('[E2E] Respuesta del servidor recibida');
            
            // Paso 4: Descifrar KEK
            this.updateProgress('Descifrando KEK...', 50);
            const kek = await this.decryptKEK(data.encryptedKEK);
            
            // Paso 5: Descifrar video
            this.updateProgress('Descifrando video...', 70);
            const decryptedVideo = await this.decryptVideo(
                data.encryptedVideo,
                kek,
                data.nonce,
                data.authTag
            );
            
            // Paso 6: Crear Blob y reproducir
            this.updateProgress('Preparando reproducción...', 90);
            const videoBlob = new Blob([decryptedVideo], { type: `video/${data.format}` });
            const videoUrl = URL.createObjectURL(videoBlob);
            
            if (this.videoElement) {
                this.videoElement.src = videoUrl;
                this.videoElement.load();
            }
            
            this.updateProgress('✓ Video listo para reproducir', 100);
            console.log('[E2E] ✓ Proceso E2E completado exitosamente');
            
            // Limpiar KEK de memoria
            kek.fill(0);
            
            return videoUrl;
            
        } catch (error) {
            console.error('[E2E] Error en proceso E2E:', error);
            throw error;
        }
    }

    /**
     * Destruye las claves efímeras
     */
    cleanup() {
        if (this.ephemeralKeyPair) {
            this.ephemeralKeyPair = null;
            console.log('[E2E] Claves efímeras destruidas');
        }
    }

    /**
     * Callback para actualizar progreso
     */
    updateProgress(message, percent) {
        console.log(`[E2E] ${message} (${percent}%)`);
        if (this.progressCallback) {
            this.progressCallback(message, percent);
        }
    }

    /**
     * Establece el elemento de video
     */
    setVideoElement(element) {
        this.videoElement = element;
    }

    /**
     * Establece callback de progreso
     */
    onProgress(callback) {
        this.progressCallback = callback;
    }
}

// Exportar para uso global
window.E2EVideoPlayerChaCha20 = E2EVideoPlayerChaCha20;
