namespace SecureVideoStreaming.Services.Exceptions
{
    public class VideoNotFoundException : Exception
    {
        public VideoNotFoundException(string message) : base(message) { }
    }

    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string message) : base(message) { }
    }

    public class VideoNotOwnedException : Exception
    {
        public VideoNotOwnedException(string message) : base(message) { }
    }

    public class PermissionDeniedException : Exception
    {
        public PermissionDeniedException(string message) : base(message) { }
    }

    public class PermissionExpiredException : Exception
    {
        public PermissionExpiredException(string message) : base(message) { }
    }
}
