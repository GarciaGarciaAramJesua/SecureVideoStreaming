/**
 * Setup Keys Page Script - Generación e importación de claves RSA
 */

document.addEventListener('DOMContentLoaded', function() {
    console.log('🔑 Página de configuración de claves cargada');

    const generateKeysBtn = document.getElementById('generateKeysBtn');
    const importKeysBtn = document.getElementById('importKeysBtn');
    const downloadBackupBtn = document.getElementById('downloadBackupBtn');
    const continueBtn = document.getElementById('continueBtn');
    const importConfirmBtn = document.getElementById('importConfirmBtn');

    const generatingSection = document.getElementById('generatingSection');
    const progressSection = document.getElementById('progressSection');
    const successSection = document.getElementById('successSection');
    const progressText = document.getElementById('progressText');

    const rsaCrypto = new RsaCryptoManager();
    const keyStorage = new SecureKeyStorage();

    let generatedPassword = null;
    let generatedFingerprint = null;

    // Mostrar advertencia si ya tiene claves
    if (keyStorage.hasPrivateKey()) {
        console.log('⚠️ Usuario ya tiene claves almacenadas en localStorage');
        const warningDiv = document.createElement('div');
        warningDiv.className = 'alert alert-warning mt-3';
        warningDiv.innerHTML = `
            <i class="bi bi-exclamation-triangle"></i>
            <strong>Ya tienes claves configuradas.</strong> Si deseas generar nuevas claves, primero debes eliminar las anteriores.
            <button type="button" class="btn btn-sm btn-outline-danger mt-2" id="deleteKeysBtn">
                <i class="bi bi-trash"></i> Eliminar claves existentes
            </button>
            <a href="/Home" class="btn btn-sm btn-primary mt-2">
                <i class="bi bi-house"></i> Ir al inicio
            </a>
        `;
        document.querySelector('.card-body').prepend(warningDiv);
        
        document.getElementById('deleteKeysBtn').addEventListener('click', function() {
            if (confirm('¿Estás seguro? Esta acción eliminará tus claves y no podrás acceder a videos cifrados.')) {
                keyStorage.clearKeys();
                location.reload();
            }
        });
    }

    // Botón de generación
    generateKeysBtn.addEventListener('click', async () => {
        try {
            // Ocultar sección de inicio y mostrar progreso
            generatingSection.style.display = 'none';
            progressSection.style.display = 'block';
            progressText.textContent = 'Generando claves RSA-2048...';

            console.log('🔐 Iniciando generación de par de claves RSA...');

            // Generar par de claves RSA-2048
            const { publicKey, privateKey } = await rsaCrypto.generateKeyPair(2048);
            console.log('✅ Par de claves RSA generado exitosamente');

            // Calcular fingerprint
            progressText.textContent = 'Calculando fingerprint...';
            const fingerprint = await rsaCrypto.getPublicKeyFingerprint(publicKey);
            generatedFingerprint = fingerprint;
            console.log('✅ Fingerprint calculado:', fingerprint);

            // Solicitar contraseña para cifrar la clave privada
            const password = await promptForPassword();
            if (!password) {
                throw new Error('Se requiere una contraseña para proteger la clave privada');
            }
            generatedPassword = password;

            // Cifrar y almacenar clave privada
            progressText.textContent = 'Cifrando clave privada con AES-256-GCM...';
            keyStorage.setStorageType(false); // localStorage
            await keyStorage.savePrivateKey(privateKey, publicKey, fingerprint, password);
            console.log('✅ Clave privada cifrada y almacenada');

            // Registrar clave pública en el servidor
            progressText.textContent = 'Registrando clave pública en el servidor...';
            const registerSuccess = await registerPublicKeyOnServer(publicKey, fingerprint);
            
            if (!registerSuccess) {
                throw new Error('No se pudo registrar la clave pública en el servidor');
            }

            console.log('✅ Clave pública registrada en el servidor');

            // Mostrar sección de éxito
            progressSection.style.display = 'none';
            successSection.style.display = 'block';

        } catch (error) {
            console.error('❌ Error al generar claves:', error);
            alert('Error al generar claves: ' + error.message);
            generatingSection.style.display = 'block';
            progressSection.style.display = 'none';
        }
    });

    // Botón de importación
    importKeysBtn.addEventListener('click', () => {
        const importModal = new bootstrap.Modal(document.getElementById('importModal'));
        importModal.show();
    });

    // Confirmar importación
    importConfirmBtn.addEventListener('click', async () => {
        const fileInput = document.getElementById('keyFileInput');
        const file = fileInput.files[0];

        if (!file) {
            alert('Por favor, selecciona un archivo');
            return;
        }

        try {
            importConfirmBtn.disabled = true;
            importConfirmBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Importando...';

            const text = await file.text();
            const keysObject = JSON.parse(text);

            if (keyStorage.importKeys(keysObject)) {
                console.log('✅ Claves importadas exitosamente');
                
                // Extraer clave pública para registrar en servidor
                const publicKey = keysObject.publicKey;
                const fingerprint = keysObject.fingerprint;

                if (publicKey && fingerprint) {
                    await registerPublicKeyOnServer(publicKey, fingerprint);
                }

                alert('✅ Claves importadas exitosamente');
                window.location.href = '/Home';
            } else {
                alert('❌ Error al importar las claves. Verifica el archivo.');
            }
        } catch (error) {
            console.error('Error al importar:', error);
            alert('❌ Error al leer el archivo: ' + error.message);
        } finally {
            importConfirmBtn.disabled = false;
            importConfirmBtn.innerHTML = 'Importar';
        }
    });

    // Botón de descargar respaldo
    downloadBackupBtn.addEventListener('click', async () => {
        try {
            downloadBackupBtn.disabled = true;
            downloadBackupBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Descargando...';

            await keyStorage.downloadKeys(generatedPassword, `backup-keys-${Date.now()}.json`);
            alert('✅ Respaldo descargado. Guárdalo en un lugar seguro.');

            downloadBackupBtn.disabled = false;
            downloadBackupBtn.innerHTML = '<i class="bi bi-download"></i> Descargar Respaldo';
        } catch (error) {
            console.error('Error al descargar:', error);
            alert('❌ Error al descargar: ' + error.message);
            downloadBackupBtn.disabled = false;
            downloadBackupBtn.innerHTML = '<i class="bi bi-download"></i> Descargar Respaldo';
        }
    });

    // Botón continuar
    continueBtn.addEventListener('click', () => {
        window.location.href = '/Home';
    });

    /**
     * Solicita contraseña al usuario mediante un prompt
     */
    async function promptForPassword() {
        return new Promise((resolve) => {
            const modalHtml = `
                <div class="modal fade" id="passwordModal" tabindex="-1" data-bs-backdrop="static" data-bs-keyboard="false">
                    <div class="modal-dialog modal-dialog-centered">
                        <div class="modal-content">
                            <div class="modal-header bg-primary text-white">
                                <h5 class="modal-title">
                                    <i class="bi bi-lock"></i> Protege tu Clave Privada
                                </h5>
                            </div>
                            <div class="modal-body">
                                <p>Elige una contraseña fuerte para cifrar tu clave privada:</p>
                                <div class="mb-3">
                                    <label for="passwordInput" class="form-label">Contraseña</label>
                                    <input type="password" class="form-control" id="passwordInput" placeholder="Mínimo 8 caracteres" minlength="8" required>
                                </div>
                                <div class="mb-3">
                                    <label for="confirmPasswordInput" class="form-label">Confirmar Contraseña</label>
                                    <input type="password" class="form-control" id="confirmPasswordInput" placeholder="Repite la contraseña" required>
                                </div>
                                <div class="alert alert-info">
                                    <i class="bi bi-info-circle"></i>
                                    Esta contraseña se usará para proteger tu clave privada localmente. <strong>¡No la olvides!</strong>
                                </div>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-primary" id="confirmPasswordBtn">Confirmar</button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

            document.body.insertAdjacentHTML('beforeend', modalHtml);
            const modal = new bootstrap.Modal(document.getElementById('passwordModal'));
            modal.show();

            document.getElementById('confirmPasswordBtn').addEventListener('click', () => {
                const password = document.getElementById('passwordInput').value;
                const confirmPassword = document.getElementById('confirmPasswordInput').value;

                if (!password || password.length < 8) {
                    alert('La contraseña debe tener al menos 8 caracteres');
                    return;
                }

                if (password !== confirmPassword) {
                    alert('Las contraseñas no coinciden');
                    return;
                }

                modal.hide();
                document.getElementById('passwordModal').remove();
                resolve(password);
            });
        });
    }

    /**
     * Registra la clave pública en el servidor mediante API
     */
    async function registerPublicKeyOnServer(publicKey, fingerprint) {
        try {
            const response = await fetch('/SetupKeys?handler=RegisterPublicKey', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                },
                body: JSON.stringify({
                    publicKey: publicKey,
                    fingerprint: fingerprint
                })
            });

            const data = await response.json();
            return data.success === true;
        } catch (error) {
            console.error('Error al registrar clave pública:', error);
            return false;
        }
    }
});
