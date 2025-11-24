namespace SecureVideoStreaming.API.Middleware
{
    /// <summary>
    /// Excepciones personalizadas para el dominio de la aplicación
    /// </summary>
    public class VideoNotFoundException : KeyNotFoundException
    {
        public VideoNotFoundException(int videoId)
            : base($"El video con ID {videoId} no fue encontrado")
        {
            VideoId = videoId;
        }

        public int VideoId { get; }
    }

    public class UserNotFoundException : KeyNotFoundException
    {
        public UserNotFoundException(int userId)
            : base($"El usuario con ID {userId} no fue encontrado")
        {
            UserId = userId;
        }

        public UserNotFoundException(string email)
            : base($"El usuario con email '{email}' no fue encontrado")
        {
            Email = email;
        }

        public int? UserId { get; }
        public string? Email { get; }
    }

    public class VideoNotOwnedException : UnauthorizedAccessException
    {
        public VideoNotOwnedException(int videoId, int userId)
            : base($"El usuario {userId} no es propietario del video {videoId}")
        {
            VideoId = videoId;
            UserId = userId;
        }

        public int VideoId { get; }
        public int UserId { get; }
    }

    public class DuplicateEmailException : InvalidOperationException
    {
        public DuplicateEmailException(string email)
            : base($"El email '{email}' ya está registrado")
        {
            Email = email;
        }

        public string Email { get; }
    }

    public class DuplicateUsernameException : InvalidOperationException
    {
        public DuplicateUsernameException(string username)
            : base($"El nombre de usuario '{username}' ya está en uso")
        {
            Username = username;
        }

        public string Username { get; }
    }

    public class InvalidCredentialsException : UnauthorizedAccessException
    {
        public InvalidCredentialsException()
            : base("Email o contraseña incorrectos")
        {
        }
    }

    public class VideoIntegrityException : InvalidOperationException
    {
        public VideoIntegrityException(int videoId, string reason)
            : base($"La integridad del video {videoId} está comprometida: {reason}")
        {
            VideoId = videoId;
            Reason = reason;
        }

        public int VideoId { get; }
        public string Reason { get; }
    }

    public class CryptoKeyNotFoundException : KeyNotFoundException
    {
        public CryptoKeyNotFoundException(int userId, string keyType)
            : base($"No se encontró la clave {keyType} para el usuario {userId}")
        {
            UserId = userId;
            KeyType = keyType;
        }

        public int UserId { get; }
        public string KeyType { get; }
    }

    public class VideoProcessingException : InvalidOperationException
    {
        public VideoProcessingException(string message, Exception? innerException = null)
            : base($"Error al procesar el video: {message}", innerException)
        {
        }
    }

    public class InsufficientPermissionsException : UnauthorizedAccessException
    {
        public InsufficientPermissionsException(string requiredRole)
            : base($"Se requiere el rol '{requiredRole}' para realizar esta operación")
        {
            RequiredRole = requiredRole;
        }

        public string RequiredRole { get; }
    }

    public class VideoUploadException : InvalidOperationException
    {
        public VideoUploadException(string message, Exception? innerException = null)
            : base($"Error al subir el video: {message}", innerException)
        {
        }
    }

    public class FileStorageException : IOException
    {
        public FileStorageException(string operation, string filePath, Exception? innerException = null)
            : base($"Error al {operation} el archivo '{filePath}'", innerException)
        {
            Operation = operation;
            FilePath = filePath;
        }

        public string Operation { get; }
        public string FilePath { get; }
    }
}
