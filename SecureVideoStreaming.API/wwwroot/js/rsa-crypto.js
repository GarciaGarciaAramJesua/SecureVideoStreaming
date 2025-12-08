/**
 * RSA Crypto Manager - Generación de claves RSA en el cliente
 * Utiliza Web Crypto API para generación segura de pares de claves
 */

class RsaCryptoManager {
    constructor() {
        this.keyPair = null;
        this.publicKeyPem = null;
        this.privateKeyPem = null;
    }

    /**
     * Genera un par de claves RSA-OAEP (2048 o 4096 bits)
     * @param {number} keySize - Tamaño de la clave (2048 o 4096)
     * @returns {Promise<{publicKey: string, privateKey: string}>}
     */
    async generateKeyPair(keySize = 2048) {
        try {
            console.log(`Generando par de claves RSA-${keySize}...`);

            // Generar par de claves usando Web Crypto API
            this.keyPair = await window.crypto.subtle.generateKey(
                {
                    name: "RSA-OAEP",
                    modulusLength: keySize,
                    publicExponent: new Uint8Array([1, 0, 1]), // 65537
                    hash: "SHA-256"
                },
                true, // extractable
                ["encrypt", "decrypt"]
            );

            // Exportar claves a formato PEM
            this.publicKeyPem = await this.exportPublicKeyToPem(this.keyPair.publicKey);
            this.privateKeyPem = await this.exportPrivateKeyToPem(this.keyPair.privateKey);

            console.log("✅ Par de claves RSA generado exitosamente");

            return {
                publicKey: this.publicKeyPem,
                privateKey: this.privateKeyPem
            };
        } catch (error) {
            console.error("❌ Error al generar par de claves RSA:", error);
            throw new Error(`Error al generar claves RSA: ${error.message}`);
        }
    }

    /**
     * Exporta la clave pública a formato PEM
     * @param {CryptoKey} publicKey 
     * @returns {Promise<string>}
     */
    async exportPublicKeyToPem(publicKey) {
        const exported = await window.crypto.subtle.exportKey("spki", publicKey);
        const exportedAsBase64 = this.arrayBufferToBase64(exported);
        const pem = `-----BEGIN PUBLIC KEY-----\n${this.formatPem(exportedAsBase64)}\n-----END PUBLIC KEY-----`;
        return pem;
    }

    /**
     * Exporta la clave privada a formato PEM
     * @param {CryptoKey} privateKey 
     * @returns {Promise<string>}
     */
    async exportPrivateKeyToPem(privateKey) {
        const exported = await window.crypto.subtle.exportKey("pkcs8", privateKey);
        const exportedAsBase64 = this.arrayBufferToBase64(exported);
        const pem = `-----BEGIN PRIVATE KEY-----\n${this.formatPem(exportedAsBase64)}\n-----END PRIVATE KEY-----`;
        return pem;
    }

    /**
     * Convierte ArrayBuffer a Base64
     * @param {ArrayBuffer} buffer 
     * @returns {string}
     */
    arrayBufferToBase64(buffer) {
        const bytes = new Uint8Array(buffer);
        let binary = '';
        for (let i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return window.btoa(binary);
    }

    /**
     * Formatea string Base64 para PEM (64 caracteres por línea)
     * @param {string} base64 
     * @returns {string}
     */
    formatPem(base64) {
        const lines = [];
        for (let i = 0; i < base64.length; i += 64) {
            lines.push(base64.substring(i, i + 64));
        }
        return lines.join('\n');
    }

    /**
     * Importa una clave privada desde formato PEM
     * @param {string} pemKey 
     * @returns {Promise<CryptoKey>}
     */
    async importPrivateKeyFromPem(pemKey) {
        try {
            // Remover headers y convertir a ArrayBuffer
            const pemContents = pemKey
                .replace('-----BEGIN PRIVATE KEY-----', '')
                .replace('-----END PRIVATE KEY-----', '')
                .replace(/\s/g, '');
            
            const binaryDer = window.atob(pemContents);
            const binaryArray = new Uint8Array(binaryDer.length);
            for (let i = 0; i < binaryDer.length; i++) {
                binaryArray[i] = binaryDer.charCodeAt(i);
            }

            return await window.crypto.subtle.importKey(
                "pkcs8",
                binaryArray.buffer,
                {
                    name: "RSA-OAEP",
                    hash: "SHA-256"
                },
                true,
                ["decrypt"]
            );
        } catch (error) {
            console.error("❌ Error al importar clave privada:", error);
            throw error;
        }
    }

    /**
     * Descifra datos usando la clave privada
     * @param {string} encryptedBase64 - Datos cifrados en Base64
     * @param {string} privateKeyPem - Clave privada en formato PEM
     * @returns {Promise<Uint8Array>}
     */
    async decrypt(encryptedBase64, privateKeyPem) {
        try {
            const privateKey = await this.importPrivateKeyFromPem(privateKeyPem);
            
            // Convertir Base64 a ArrayBuffer
            const encryptedData = Uint8Array.from(
                window.atob(encryptedBase64),
                c => c.charCodeAt(0)
            );

            // Descifrar
            const decrypted = await window.crypto.subtle.decrypt(
                {
                    name: "RSA-OAEP"
                },
                privateKey,
                encryptedData
            );

            return new Uint8Array(decrypted);
        } catch (error) {
            console.error("❌ Error al descifrar:", error);
            throw error;
        }
    }

    /**
     * Obtiene el fingerprint SHA-256 de la clave pública
     * @param {string} publicKeyPem 
     * @returns {Promise<string>}
     */
    async getPublicKeyFingerprint(publicKeyPem) {
        const encoder = new TextEncoder();
        const data = encoder.encode(publicKeyPem);
        const hashBuffer = await window.crypto.subtle.digest('SHA-256', data);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
    }
}

// Exportar instancia global
window.RsaCryptoManager = RsaCryptoManager;
