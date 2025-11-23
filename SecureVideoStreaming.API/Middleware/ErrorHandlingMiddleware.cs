using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Authentication;
using System.Text.Json;

namespace SecureVideoStreaming.API.Middleware
{
    /// <summary>
    /// Middleware global para manejo centralizado de excepciones
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ErrorHandlingMiddleware(
            RequestDelegate next,
            ILogger<ErrorHandlingMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Log detallado de la excepción
            _logger.LogError(exception,
                "Excepción no controlada en {Path}. Usuario: {User}, IP: {IP}",
                context.Request.Path,
                context.User?.Identity?.Name ?? "Anónimo",
                context.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

            // Determinar código de estado HTTP y mensaje
            var (statusCode, message, errorType) = MapExceptionToResponse(exception);

            // Configurar respuesta
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            // Construir respuesta de error
            var errorResponse = new ErrorResponse
            {
                Success = false,
                Message = message,
                ErrorType = errorType,
                StatusCode = (int)statusCode,
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path,
                Method = context.Request.Method
            };

            // Incluir detalles solo en Development
            if (_environment.IsDevelopment())
            {
                errorResponse.Details = exception.Message;
                errorResponse.StackTrace = exception.StackTrace;
                errorResponse.InnerException = exception.InnerException?.Message;
            }

            // Serializar y enviar respuesta
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            };

            var json = JsonSerializer.Serialize(errorResponse, jsonOptions);
            await context.Response.WriteAsync(json);
        }

        private (HttpStatusCode statusCode, string message, string errorType) MapExceptionToResponse(Exception exception)
        {
            return exception switch
            {
                // 400 - Bad Request (más específico primero)
                ArgumentNullException => (
                    HttpStatusCode.BadRequest,
                    "Se requiere un parámetro obligatorio",
                    "MissingParameter"
                ),
                
                ArgumentException => (
                    HttpStatusCode.BadRequest,
                    "Los parámetros proporcionados no son válidos",
                    "ValidationError"
                ),

                InvalidOperationException => (
                    HttpStatusCode.BadRequest,
                    exception.Message,
                    "InvalidOperation"
                ),

                // 401 - Unauthorized
                UnauthorizedAccessException => (
                    HttpStatusCode.Unauthorized,
                    "No tiene autorización para realizar esta operación",
                    "Unauthorized"
                ),

                // 403 - Forbidden
                InvalidCredentialException => (
                    HttpStatusCode.Forbidden,
                    "Credenciales inválidas",
                    "Forbidden"
                ),

                // 404 - Not Found
                KeyNotFoundException => (
                    HttpStatusCode.NotFound,
                    "El recurso solicitado no fue encontrado",
                    "NotFound"
                ),

                FileNotFoundException => (
                    HttpStatusCode.NotFound,
                    "El archivo solicitado no existe",
                    "FileNotFound"
                ),

                // 409 - Conflict
                DbUpdateConcurrencyException => (
                    HttpStatusCode.Conflict,
                    "Conflicto de concurrencia. El recurso fue modificado por otro usuario",
                    "ConcurrencyConflict"
                ),

                // 422 - Unprocessable Entity
                DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("duplicate key") == true => (
                    HttpStatusCode.UnprocessableEntity,
                    "El registro ya existe en la base de datos",
                    "DuplicateKey"
                ),

                DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("FOREIGN KEY constraint") == true => (
                    HttpStatusCode.UnprocessableEntity,
                    "No se puede completar la operación debido a restricciones de integridad referencial",
                    "ForeignKeyConstraint"
                ),

                // 500 - Internal Server Error
                DbUpdateException => (
                    HttpStatusCode.InternalServerError,
                    "Error al actualizar la base de datos",
                    "DatabaseError"
                ),

                IOException => (
                    HttpStatusCode.InternalServerError,
                    "Error al acceder al sistema de archivos",
                    "FileSystemError"
                ),

                // Excepciones criptográficas
                System.Security.Cryptography.CryptographicException => (
                    HttpStatusCode.InternalServerError,
                    "Error en operación criptográfica",
                    "CryptographicError"
                ),

                // Default - 500 Internal Server Error
                _ => (
                    HttpStatusCode.InternalServerError,
                    "Ha ocurrido un error interno en el servidor",
                    "InternalServerError"
                )
            };
        }
    }

    /// <summary>
    /// Modelo de respuesta de error estandarizado
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Indica si la operación fue exitosa (siempre false para errores)
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Mensaje de error user-friendly
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de error (para procesamiento en cliente)
        /// </summary>
        public string ErrorType { get; set; } = string.Empty;

        /// <summary>
        /// Código de estado HTTP
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Timestamp del error (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Path del request que causó el error
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Método HTTP del request
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Detalles adicionales (solo en Development)
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Stack trace completo (solo en Development)
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Mensaje de excepción interna (solo en Development)
        /// </summary>
        public string? InnerException { get; set; }
    }

    /// <summary>
    /// Extension methods para registrar el middleware
    /// </summary>
    public static class ErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ErrorHandlingMiddleware>();
        }
    }
}
