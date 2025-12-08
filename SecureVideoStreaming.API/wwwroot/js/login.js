/**
 * Login Page Script - Gestión de login y verificación de claves
 */

document.addEventListener('DOMContentLoaded', function() {
    console.log('🔐 Página de login cargada');

    const loginForm = document.querySelector('form');
    const emailInput = document.querySelector('input[name="Email"]');
    
    // Instanciar key storage
    const keyStorage = new SecureKeyStorage();

    // Mostrar advertencia si no hay clave privada guardada
    displayKeyWarningIfNeeded();

    // Verificar clave privada después del login
    loginForm.addEventListener('submit', function(event) {
        const email = emailInput.value;
        
        // Guardar email para verificación post-login
        sessionStorage.setItem('last_login_email', email);
    });

    /**
     * Muestra advertencia si no hay clave privada guardada
     */
    function displayKeyWarningIfNeeded() {
        if (!keyStorage.hasPrivateKey()) {
            const warningDiv = document.createElement('div');
            warningDiv.className = 'alert alert-warning mt-3';
            warningDiv.innerHTML = `
                <i class="bi bi-exclamation-triangle"></i>
                <strong>Nota:</strong> Si eres un usuario consumidor y no encuentras tu clave privada,
                no podrás descifrar los videos. Asegúrate de tener un respaldo de tu clave.
                <button type="button" class="btn btn-sm btn-outline-warning mt-2" id="importKeyBtn">
                    <i class="bi bi-upload"></i> Importar clave privada
                </button>
            `;
            
            const cardBody = document.querySelector('.card-body');
            const form = cardBody.querySelector('form');
            form.parentNode.insertBefore(warningDiv, form);

            // Botón para importar clave
            document.getElementById('importKeyBtn').addEventListener('click', showImportKeyModal);
        } else {
            const fingerprint = keyStorage.getFingerprint();
            const infoDiv = document.createElement('div');
            infoDiv.className = 'alert alert-success mt-3';
            infoDiv.innerHTML = `
                <i class="bi bi-shield-check"></i>
                <strong>Clave privada encontrada</strong><br>
                <small>Fingerprint: ${fingerprint ? fingerprint.substring(0, 40) + '...' : 'No disponible'}</small>
            `;
            
            const cardBody = document.querySelector('.card-body');
            const form = cardBody.querySelector('form');
            form.parentNode.insertBefore(infoDiv, form);
        }
    }

    /**
     * Muestra modal para importar clave privada
     */
    function showImportKeyModal() {
        const modalHtml = `
            <div class="modal fade" id="importKeyModal" tabindex="-1">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">
                                <i class="bi bi-upload"></i> Importar Clave Privada
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <p>Selecciona el archivo JSON que contiene tu clave privada:</p>
                            <input type="file" class="form-control" id="keyFileInput" accept=".json">
                            <div class="mt-3 alert alert-info">
                                <i class="bi bi-info-circle"></i>
                                Este archivo fue descargado cuando te registraste como usuario consumidor.
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancelar</button>
                            <button type="button" class="btn btn-primary" id="importBtn">Importar</button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', modalHtml);
        const modal = new bootstrap.Modal(document.getElementById('importKeyModal'));
        modal.show();

        document.getElementById('importBtn').addEventListener('click', async () => {
            const fileInput = document.getElementById('keyFileInput');
            const file = fileInput.files[0];

            if (!file) {
                alert('Por favor, selecciona un archivo');
                return;
            }

            try {
                const text = await file.text();
                const keysObject = JSON.parse(text);
                
                if (keyStorage.importKeys(keysObject)) {
                    alert('✅ Clave privada importada exitosamente');
                    modal.hide();
                    location.reload();
                } else {
                    alert('❌ Error al importar la clave. Verifica el archivo.');
                }
            } catch (error) {
                console.error('Error al importar:', error);
                alert('❌ Error al leer el archivo. Asegúrate de que sea un archivo JSON válido.');
            }
        });

        // Limpiar modal al cerrar
        document.getElementById('importKeyModal').addEventListener('hidden.bs.modal', () => {
            document.getElementById('importKeyModal').remove();
        });
    }
});
