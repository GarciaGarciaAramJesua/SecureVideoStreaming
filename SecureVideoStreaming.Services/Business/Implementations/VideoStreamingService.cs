using SecureVideoStreaming.Models.DTOs.Response;
using SecureVideoStreaming.Services.Business.Interfaces;

namespace SecureVideoStreaming.Services.Business.Implementations
{
    public class VideoStreamingService : IVideoStreamingService
    {
        public async Task<(Stream stream, long totalSize, long start, long end)> GetVideoChunkAsync(
            string videoPath, 
            long rangeStart, 
            long? rangeEnd = null)
        {
            // Validar que el archivo existe
            if (!File.Exists(videoPath))
                throw new FileNotFoundException("Video no encontrado", videoPath);

            var fileInfo = new FileInfo(videoPath);
            var totalSize = fileInfo.Length;

            // Ajustar rangeEnd si no está especificado o excede el tamaño
            var actualEnd = rangeEnd ?? totalSize - 1;
            if (actualEnd >= totalSize)
            {
                actualEnd = totalSize - 1;
            }

            // Validar rango
            if (rangeStart < 0 || rangeStart > actualEnd || actualEnd >= totalSize)
            {
                throw new ArgumentException(
                    $"Rango inválido: {rangeStart}-{actualEnd} (tamaño: {totalSize})");
            }

            // Calcular tamaño del chunk
            var chunkSize = actualEnd - rangeStart + 1;

            // Abrir stream y posicionar en el inicio del rango
            var fileStream = new FileStream(videoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            fileStream.Seek(rangeStart, SeekOrigin.Begin);

            // Crear stream limitado al chunk solicitado
            var limitedStream = new LimitedStream(fileStream, chunkSize);

            return (limitedStream, totalSize, rangeStart, actualEnd);
        }

        public async Task<(long fileSize, string contentType)> GetVideoInfoAsync(string videoPath)
        {
            if (!File.Exists(videoPath))
                throw new FileNotFoundException("Video no encontrado", videoPath);

            var fileInfo = new FileInfo(videoPath);
            var fileSize = fileInfo.Length;
            var contentType = "application/octet-stream"; // Videos cifrados

            return (fileSize, contentType);
        }

        public async Task<bool> ValidateVideoFileAsync(string videoPath)
        {
            if (string.IsNullOrWhiteSpace(videoPath))
                return false;

            if (!File.Exists(videoPath))
                return false;

            var fileInfo = new FileInfo(videoPath);
            if (fileInfo.Length == 0)
                return false;

            try
            {
                // Intentar abrir el archivo para verificar permisos
                using (var fs = File.OpenRead(videoPath))
                {
                    // Solo verificar que se puede abrir
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Stream auxiliar que limita la lectura a un número específico de bytes
        /// </summary>
        private class LimitedStream : Stream
        {
            private readonly Stream _baseStream;
            private readonly long _maxLength;
            private long _position;

            public LimitedStream(Stream baseStream, long maxLength)
            {
                _baseStream = baseStream;
                _maxLength = maxLength;
                _position = 0;
            }

            public override bool CanRead => _baseStream.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _maxLength;
            public override long Position 
            { 
                get => _position; 
                set => throw new NotSupportedException(); 
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var remainingBytes = _maxLength - _position;
                if (remainingBytes <= 0)
                    return 0;

                var bytesToRead = (int)Math.Min(count, remainingBytes);
                var bytesRead = _baseStream.Read(buffer, offset, bytesToRead);
                _position += bytesRead;
                return bytesRead;
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                var remainingBytes = _maxLength - _position;
                if (remainingBytes <= 0)
                    return 0;

                var bytesToRead = (int)Math.Min(count, remainingBytes);
                var bytesRead = await _baseStream.ReadAsync(buffer, offset, bytesToRead, cancellationToken);
                _position += bytesRead;
                return bytesRead;
            }

            public override void Flush() => _baseStream.Flush();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _baseStream?.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
