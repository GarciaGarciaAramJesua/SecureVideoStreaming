using SecureVideoStreaming.Models.DTOs.Request;
using SecureVideoStreaming.Models.DTOs.Response;

namespace SecureVideoStreaming.Services.Business.Interfaces
{
    public interface IVideoService
    {
        Task<ApiResponse<VideoResponse>> UploadVideoAsync(UploadVideoRequest request, Stream videoStream);
        Task<ApiResponse<List<VideoListResponse>>> GetAllVideosAsync();
        Task<ApiResponse<List<VideoListResponse>>> GetVideosByAdminAsync(int adminId);
        Task<ApiResponse<VideoResponse>> GetVideoByIdAsync(int videoId);
        Task<ApiResponse<bool>> DeleteVideoAsync(int videoId, int adminId);
    }
}
