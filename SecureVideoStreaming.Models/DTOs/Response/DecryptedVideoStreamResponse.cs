namespace SecureVideoStreaming.Models.DTOs.Response
{
    public class DecryptedVideoStreamResponse
    {
        public Stream Stream { get; set; } = null!;
        public string ContentType { get; set; } = "video/mp4";
        public long Length { get; set; }
    }
}
