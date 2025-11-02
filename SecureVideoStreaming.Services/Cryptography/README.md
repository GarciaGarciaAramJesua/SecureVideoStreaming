# Módulo de Criptografía - Secure Video Streaming

## Algoritmos Implementados

### 1. ChaCha20-Poly1305
- **Propósito**: Cifrado autenticado de videos
- **Características**: 
  - Cifrado de flujo rápido y seguro
  - Autenticación integrada (AEAD)
  - Clave: 256 bits (32 bytes)
  - Nonce: 96 bits (12 bytes)
  - Tag de autenticación: 128 bits (16 bytes)

### 2. RSA-2048/4096 con OAEP
- **Propósito**: Cifrado de claves simétricas y firma digital
- **Características**:
  - Cifrado asimétrico con padding OAEP-SHA256
  - Firma digital con SHA256withRSA
  - Formato: PEM

### 3. SHA-256
- **Propósito**: Hash criptográfico e integridad
- **Características**:
  - Hash de 256 bits
  - PBKDF2 para derivación de claves
  - Soporte para streams (archivos grandes)

### 4. HMAC-SHA256
- **Propósito**: Autenticación de mensajes
- **Características**:
  - MAC de 256 bits
  - Verificación de tiempo constante
  - Clave recomendada: 64 bytes

### 5. KMAC256
- **Propósito**: MAC basado en SHA-3
- **Características**:
  - Alternativa moderna a HMAC
  - Soporte para customización
  - Salida variable

## Uso Básico

### ChaCha20-Poly1305
```csharp
var service = new ChaCha20Poly1305Service();
var key = service.GenerateKey();
var data = Encoding.UTF8.GetBytes("Mensaje secreto");

// Cifrar
var (ciphertext, nonce, authTag) = service.Encrypt(data, key);

// Descifrar
var decrypted = service.Decrypt(ciphertext, key, nonce, authTag);
```

### RSA
```csharp
var service = new RsaService();
var (publicKey, privateKey) = service.GenerateKeyPair(2048);

// Cifrar
var encrypted = service.Encrypt(data, publicKey);

// Descifrar
var decrypted = service.Decrypt(encrypted, privateKey);

// Firmar
var signature = service.Sign(data, privateKey);

// Verificar
var isValid = service.VerifySignature(data, signature, publicKey);
```

## Consideraciones de Seguridad

1. **Nunca reutilizar nonces** en ChaCha20-Poly1305
2. **Proteger claves privadas RSA**
3. **Usar PBKDF2 con al menos 100,000 iteraciones**
4. **Generar salts aleatorios** para cada usuario
5. **Verificación de tiempo constante** para MACs
