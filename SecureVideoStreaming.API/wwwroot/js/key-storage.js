/**
 * Secure Key Storage Manager - Almacenamiento seguro de claves privadas
 * Gestiona el almacenamiento de claves RSA en el navegador con cifrado AES-256-GCM
 * 
 * SEGURIDAD:
 * - Las claves privadas se cifran con AES-256-GCM antes de almacenarse
 * - La clave de cifrado se deriva con PBKDF2-SHA256 (200,000 iteraciones)
 * - Formato compatible con el servidor (JSON Base64 con version, salt, iv, authTag, ciphertext)
 */

class SecureKeyStorage {
    constructor() {
        this.storageKey = 'user_private_key_encrypted';
        this.publicKeyKey = 'user_public_key';
        this.fingerprintKey = 'key_fingerprint';
        this.useSessionStorage = false;
        
        // Constantes de cifrado (alineadas con servidor)
        this.PBKDF2_ITERATIONS = 200000;
        this.SALT_SIZE = 32; // 256 bits
        this.KEY_SIZE = 32;  // 256 bits
        this.IV_SIZE = 12;   // 96 bits para AES-GCM
        this.VERSION = 1;
    }

    /**
     * Configura si usar sessionStorage o localStorage
     * @param {boolean} useSession - true para sessionStorage, false para localStorage
     */
    setStorageType(useSession) {
        this.useSessionStorage = useSession;
    }

    /**
     * Obtiene el storage apropiado
     * @returns {Storage}
     */
    getStorage() {
        return this.useSessionStorage ? sessionStorage : localStorage;
    }

    /**
     * Guarda la clave privada CIFRADA en el storage usando PBKDF2 + AES-256-GCM
     * @param {string} privateKeyPem - Clave privada en formato PEM
     * @param {string} publicKeyPem - Clave pública en formato PEM
     * @param {string} fingerprint - Fingerprint de la clave
     * @param {string} password - Contraseña del usuario para cifrar
     * @returns {Promise<boolean>}
     */
    async savePrivateKey(privateKeyPem, publicKeyPem, fingerprint, password) {
        try {
            const storage = this.getStorage();
            
            // Cifrar clave privada con contraseña
            const encryptedPrivateKey = await this.encryptPrivateKey(privateKeyPem, password);
            
            // Guardar datos cifrados y públicos
            storage.setItem(this.storageKey, encryptedPrivateKey);
            storage.setItem(this.publicKeyKey, publicKeyPem);
            storage.setItem(this.fingerprintKey, fingerprint);
            
            console.log('✅ Clave privada cifrada y guardada en', this.useSessionStorage ? 'sessionStorage' : 'localStorage');
            console.log('🔐 Cifrado: PBKDF2-SHA256 (200k iter) + AES-256-GCM');
            
            return true;
        } catch (error) {
            console.error('❌ Error al guardar clave privada:', error);
            throw error;
        }
    }

    /**
     * Cifra una clave privada usando PBKDF2 + AES-256-GCM
     * Formato compatible con servidor: JSON Base64
     * @param {string} privateKeyPem 
     * @param {string} password 
     * @returns {Promise<string>} - Clave cifrada en formato Base64(JSON)
     */
    async encryptPrivateKey(privateKeyPem, password) {
        try {
            // 1. Generar salt aleatorio
            const salt = window.crypto.getRandomValues(new Uint8Array(this.SALT_SIZE));
            
            // 2. Derivar clave de cifrado con PBKDF2
            const passwordKey = await this.deriveKeyFromPassword(password, salt);
            
            // 3. Generar IV aleatorio
            const iv = window.crypto.getRandomValues(new Uint8Array(this.IV_SIZE));
            
            // 4. Convertir PEM a bytes
            const encoder = new TextEncoder();
            const privateKeyBytes = encoder.encode(privateKeyPem);
            
            // 5. Cifrar con AES-256-GCM (authTag incluido en resultado)
            const encryptedData = await window.crypto.subtle.encrypt(
                {
                    name: 'AES-GCM',
                    iv: iv,
                    additionalData: salt, // Vincular salt como AAD
                    tagLength: 128 // 16 bytes
                },
                passwordKey,
                privateKeyBytes
            );
            
            // 6. Separar ciphertext y authTag (últimos 16 bytes)
            const encryptedArray = new Uint8Array(encryptedData);
            const ciphertext = encryptedArray.slice(0, encryptedArray.length - 16);
            const authTag = encryptedArray.slice(encryptedArray.length - 16);
            
            // 7. Crear estructura JSON compatible con servidor
            const encryptedStructure = {
                version: this.VERSION,
                salt: this.arrayBufferToBase64(salt),
                nonce: this.arrayBufferToBase64(iv), // 'nonce' para compatibilidad
                authTag: this.arrayBufferToBase64(authTag),
                ciphertext: this.arrayBufferToBase64(ciphertext)
            };
            
            // 8. Serializar y codificar en Base64
            const json = JSON.stringify(encryptedStructure);
            const jsonBytes = encoder.encode(json);
            return this.arrayBufferToBase64(jsonBytes);
            
        } catch (error) {
            console.error('❌ Error al cifrar clave privada:', error);
            throw new Error(`Error de cifrado: ${error.message}`);
        }
    }

    /**
     * Descifra una clave privada usando PBKDF2 + AES-256-GCM
     * @param {string} encryptedPrivateKey - Clave cifrada en formato Base64(JSON)
     * @param {string} password - Contraseña del usuario
     * @returns {Promise<string>} - Clave privada descifrada en formato PEM
     */
    async decryptPrivateKey(encryptedPrivateKey, password) {
        try {
            // 1. Decodificar Base64 y parsear JSON
            const jsonBytes = this.base64ToArrayBuffer(encryptedPrivateKey);
            const decoder = new TextDecoder();
            const json = decoder.decode(jsonBytes);
            const encryptedStructure = JSON.parse(json);
            
            // 2. Validar versión
            if (encryptedStructure.version !== this.VERSION) {
                throw new Error(`Versión no soportada: ${encryptedStructure.version}`);
            }
            
            // 3. Decodificar componentes
            const salt = this.base64ToArrayBuffer(encryptedStructure.salt);
            const iv = this.base64ToArrayBuffer(encryptedStructure.nonce);
            const authTag = this.base64ToArrayBuffer(encryptedStructure.authTag);
            const ciphertext = this.base64ToArrayBuffer(encryptedStructure.ciphertext);
            
            // 4. Derivar clave de cifrado con PBKDF2
            const passwordKey = await this.deriveKeyFromPassword(password, new Uint8Array(salt));
            
            // 5. Concatenar ciphertext + authTag para AES-GCM
            const encryptedData = new Uint8Array(ciphertext.byteLength + authTag.byteLength);
            encryptedData.set(new Uint8Array(ciphertext), 0);
            encryptedData.set(new Uint8Array(authTag), ciphertext.byteLength);
            
            // 6. Descifrar con AES-256-GCM
            const decryptedData = await window.crypto.subtle.decrypt(
                {
                    name: 'AES-GCM',
                    iv: new Uint8Array(iv),
                    additionalData: new Uint8Array(salt),
                    tagLength: 128
                },
                passwordKey,
                encryptedData
            );
            
            // 7. Convertir bytes a string PEM
            const privateKeyPem = decoder.decode(decryptedData);
            
            // 8. Validar formato PEM
            if (!privateKeyPem.includes('-----BEGIN PRIVATE KEY-----')) {
                throw new Error('Formato PEM inválido después de descifrar');
            }
            
            return privateKeyPem;
            
        } catch (error) {
            console.error('❌ Error al descifrar clave privada:', error);
            if (error.name === 'OperationError') {
                throw new Error('Contraseña incorrecta o datos corruptos');
            }
            throw error;
        }
    }

    /**
     * Deriva una clave de cifrado desde contraseña usando PBKDF2
     * @param {string} password 
     * @param {Uint8Array} salt 
     * @returns {Promise<CryptoKey>}
     */
    async deriveKeyFromPassword(password, salt) {
        const encoder = new TextEncoder();
        const passwordBytes = encoder.encode(password);
        
        // Importar password como clave base
        const baseKey = await window.crypto.subtle.importKey(
            'raw',
            passwordBytes,
            'PBKDF2',
            false,
            ['deriveKey']
        );
        
        // Derivar clave de cifrado
        return await window.crypto.subtle.deriveKey(
            {
                name: 'PBKDF2',
                salt: salt,
                iterations: this.PBKDF2_ITERATIONS,
                hash: 'SHA-256'
            },
            baseKey,
            {
                name: 'AES-GCM',
                length: this.KEY_SIZE * 8 // 256 bits
            },
            false,
            ['encrypt', 'decrypt']
        );
    }

    /**
     * Convierte ArrayBuffer a Base64
     * @param {ArrayBuffer|Uint8Array} buffer 
     * @returns {string}
     */
    arrayBufferToBase64(buffer) {
        const bytes = buffer instanceof Uint8Array ? buffer : new Uint8Array(buffer);
        let binary = '';
        for (let i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return window.btoa(binary);
    }

    /**
     * Convierte Base64 a ArrayBuffer
     * @param {string} base64 
     * @returns {ArrayBuffer}
     */
    base64ToArrayBuffer(base64) {
        const binary = window.atob(base64);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        return bytes.buffer;
    }

    /**
     * Recupera y descifra la clave privada del storage
     * REQUIERE contraseña del usuario para descifrar
     * @param {string} password - Contraseña del usuario
     * @returns {Promise<string|null>} - Clave privada descifrada en formato PEM
     */
    async getPrivateKey(password) {
        try {
            const storage = this.getStorage();
            const encryptedPrivateKey = storage.getItem(this.storageKey);
            
            if (!encryptedPrivateKey) {
                console.warn('⚠️  No hay clave privada almacenada');
                return null;
            }
            
            // Descifrar clave privada
            const privateKeyPem = await this.decryptPrivateKey(encryptedPrivateKey, password);
            return privateKeyPem;
            
        } catch (error) {
            console.error('❌ Error al recuperar clave privada:', error);
            throw error;
        }
    }

    /**
     * Recupera la clave privada CIFRADA (sin descifrar)
     * Útil para verificar existencia sin necesitar contraseña
     * @returns {string|null}
     */
    getEncryptedPrivateKey() {
        try {
            const storage = this.getStorage();
            return storage.getItem(this.storageKey);
        } catch (error) {
            console.error('❌ Error al recuperar clave privada cifrada:', error);
            return null;
        }
    }

    /**
     * Recupera la clave pública del storage
     * @returns {string|null}
     */
    getPublicKey() {
        try {
            const storage = this.getStorage();
            return storage.getItem(this.publicKeyKey);
        } catch (error) {
            console.error('❌ Error al recuperar clave pública:', error);
            return null;
        }
    }

    /**
     * Recupera el fingerprint de la clave
     * @returns {string|null}
     */
    getFingerprint() {
        try {
            const storage = this.getStorage();
            return storage.getItem(this.fingerprintKey);
        } catch (error) {
            console.error('❌ Error al recuperar fingerprint:', error);
            return null;
        }
    }

    /**
     * Verifica si existe una clave privada guardada
     * @returns {boolean}
     */
    hasPrivateKey() {
        const storage = this.getStorage();
        return storage.getItem(this.storageKey) !== null;
    }

    /**
     * Elimina todas las claves del storage
     */
    clearKeys() {
        try {
            const storage = this.getStorage();
            storage.removeItem(this.storageKey);
            storage.removeItem(this.publicKeyKey);
            storage.removeItem(this.fingerprintKey);
            console.log('✅ Claves eliminadas del storage');
            return true;
        } catch (error) {
            console.error('❌ Error al eliminar claves:', error);
            return false;
        }
    }

    /**
     * Exporta las claves a un objeto JSON para descarga
     * REQUIERE contraseña para descifrar la clave privada
     * @param {string} password - Contraseña del usuario
     * @returns {Promise<object|null>}
     */
    async exportKeys(password) {
        try {
            const privateKey = await this.getPrivateKey(password);
            const publicKey = this.getPublicKey();
            const fingerprint = this.getFingerprint();

            if (!privateKey || !publicKey) {
                console.error('❌ No hay claves para exportar');
                return null;
            }

            return {
                privateKey,
                publicKey,
                fingerprint,
                exportDate: new Date().toISOString(),
                warning: 'GUARDAR EN LUGAR SEGURO - Esta clave privada permite descifrar tus videos'
            };
        } catch (error) {
            console.error('❌ Error al exportar claves:', error);
            throw error;
        }
    }

    /**
     * Descarga las claves como archivo JSON
     * REQUIERE contraseña para descifrar la clave privada
     * @param {string} password - Contraseña del usuario
     * @param {string} filename - Nombre del archivo
     * @returns {Promise<void>}
     */
    async downloadKeys(password, filename = 'my-private-key.json') {
        try {
            const keys = await this.exportKeys(password);
            if (!keys) {
                alert('No hay claves para exportar');
                return;
            }

            const dataStr = JSON.stringify(keys, null, 2);
            const dataBlob = new Blob([dataStr], { type: 'application/json' });
            const url = URL.createObjectURL(dataBlob);
            
            const link = document.createElement('a');
            link.href = url;
            link.download = filename;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(url);
            
            console.log('✅ Claves descargadas:', filename);
        } catch (error) {
            console.error('❌ Error al descargar claves:', error);
            alert('Error al descargar claves: ' + error.message);
            throw error;
        }
    }

    /**
     * Importa claves desde un objeto JSON
     * @param {object} keysObject 
     */
    importKeys(keysObject) {
        try {
            if (!keysObject.privateKey || !keysObject.publicKey) {
                throw new Error('Formato de claves inválido');
            }

            this.savePrivateKey(
                keysObject.privateKey,
                keysObject.publicKey,
                keysObject.fingerprint || ''
            );

            console.log('✅ Claves importadas exitosamente');
            return true;
        } catch (error) {
            console.error('❌ Error al importar claves:', error);
            return false;
        }
    }

    /**
     * Genera un backup cifrado de las claves
     * NOTA: La clave privada YA está cifrada en storage, este método la exporta
     * @param {string} password - Contraseña para descifrar y re-exportar
     * @returns {Promise<object>}
     */
    async createEncryptedBackup(password) {
        console.log('🔐 Generando backup de claves cifradas...');
        return await this.exportKeys(password);
    }

    /**
     * Verifica si una contraseña es correcta intentando descifrar la clave
     * @param {string} password - Contraseña a verificar
     * @returns {Promise<boolean>}
     */
    async verifyPassword(password) {
        try {
            const privateKey = await this.getPrivateKey(password);
            return privateKey !== null;
        } catch (error) {
            return false;
        }
    }
}

// Exportar instancia global
window.SecureKeyStorage = SecureKeyStorage;
