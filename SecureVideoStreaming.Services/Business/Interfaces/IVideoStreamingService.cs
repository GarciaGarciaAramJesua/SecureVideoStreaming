namespace SecureVideoStreaming.Services.Business.Interfaces
{
    /// <summary>
    /// Servicio para streaming de videos cifrados en chunks
    /// </summary>
    public interface IVideoStreamingService
    {
        /// <summary>
        /// Obtener chunk de video cifrado para streaming
        /// </summary>
        /// <param name="videoPath">Ruta del archivo de video cifrado</param>
        /// <param name="rangeStart">Byte de inicio (para HTTP Range request)</param>
        /// <param name="rangeEnd">Byte de fin (nullable)</param>
        /// <returns>Stream del chunk, tamaño total, rango actual</returns>
        Task<(Stream stream, long totalSize, long start, long end)> GetVideoChunkAsync(
            string videoPath, 
            long rangeStart, 
            long? rangeEnd = null);

        /// <summary>
        /// Obtener información de video para streaming
        /// </summary>
        Task<(long fileSize, string contentType)> GetVideoInfoAsync(string videoPath);

        /// <summary>
        /// Validar que el archivo de video existe y es accesible
        /// </summary>
        Task<bool> ValidateVideoFileAsync(string videoPath);
    }
}
