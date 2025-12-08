/**
 * Register Page Script - Gestión de registro SIMPLIFICADA (Modelo de Claves Efímeras)
 * SEGURIDAD: NO se generan ni almacenan claves privadas durante el registro
 * Las claves temporales se generarán solo al momento de ver videos
 */

document.addEventListener('DOMContentLoaded', function() {
    console.log('📝 Página de registro cargada');
    console.log('[Security] 🔐 Modelo de seguridad: Ephemeral Keys (sin almacenamiento)');

    const registerForm = document.querySelector('form');
    const userTypeSelect = document.querySelector('select[name="UserType"]');
    const submitButton = registerForm.querySelector('button[type="submit"]');
    
    // Instanciar RSA crypto manager (solo para generar clave pública para el servidor)
    const rsaCrypto = new RsaCryptoManager();

    // Campo oculto para la clave pública RSA (solo para enviar al servidor)
    let publicKeyInput = document.createElement('input');
    publicKeyInput.type = 'hidden';
    publicKeyInput.name = 'PublicKeyRSA';
    publicKeyInput.id = 'PublicKeyRSA';
    registerForm.appendChild(publicKeyInput);

    // Mostrar información según tipo de usuario
    userTypeSelect.addEventListener('change', function() {
        const infoDiv = document.querySelector('.key-generation-info');
        if (infoDiv) {
            infoDiv.remove();
        }

        if (this.value === 'Usuario') {
            const info = document.createElement('div');
            info.className = 'alert alert-success mt-3 key-generation-info';
            info.innerHTML = `
                <i class="bi bi-shield-check"></i>
                <strong>Seguridad Mejorada:</strong><br>
                Al registrarte como Usuario (Consumidor), se generará una clave pública RSA para el servidor.
                <strong>¡Sin descargas ni respaldos necesarios!</strong><br>
                <small class="text-muted">
                    Cuando veas videos, se generarán claves temporales que solo existen en memoria RAM
                    y se destruyen automáticamente. Esto elimina el riesgo de robo de claves.
                </small>
            `;
            userTypeSelect.closest('.mb-3').after(info);
        }
    });

    // Interceptar envío del formulario
    registerForm.addEventListener('submit', async function(event) {
        event.preventDefault();

        const userType = userTypeSelect.value;
        const originalButtonText = submitButton.innerHTML;

        try {
            // Si es Usuario (consumidor), generar SOLO clave pública para el servidor
            if (userType === 'Usuario') {
                submitButton.disabled = true;
                submitButton.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Generando clave pública...';

                console.log('[Security] 🔐 Generando clave pública RSA para el servidor...');
                console.log('[Security] ⚠️ NO se generará ni almacenará clave privada');

                // Generar par de claves RSA-2048 (solo usaremos la pública)
                const { publicKey } = await rsaCrypto.generateKeyPair(2048);

                console.log('[Security] ✅ Clave pública generada (la privada se descarta inmediatamente)');
                console.log('[Security] ℹ️ Las claves temporales se generarán al ver videos');

                // Agregar solo la clave pública al formulario
                publicKeyInput.value = publicKey;
            }

            // Enviar formulario
            submitButton.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Registrando...';
            registerForm.submit();

        } catch (error) {
            console.error('❌ Error al procesar registro:', error);
            alert('Error al generar clave de seguridad: ' + error.message);
            submitButton.disabled = false;
            submitButton.innerHTML = originalButtonText;
        }
    });


    // Trigger inicial para mostrar info si ya está seleccionado Usuario
    if (userTypeSelect.value === 'Usuario') {
        userTypeSelect.dispatchEvent(new Event('change'));
    }
});
