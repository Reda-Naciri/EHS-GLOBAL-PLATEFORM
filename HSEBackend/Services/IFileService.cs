using HSEBackend.DTOs;

namespace HSEBackend.Services
{
    public interface IFileService
    {
        Task<FileUploadResponseDto> UploadFileAsync(IFormFile file, string category = "general");
        Task<FileDownloadResponseDto> DownloadFileAsync(string fileName);
        Task<bool> DeleteFileAsync(string fileName);
        Task<IEnumerable<FileInfoDto>> GetFilesAsync(string category = "");
        Task<long> GetTotalFileSizeAsync();
        bool IsValidFileType(string fileName);
        bool IsValidFileSize(long fileSize);
    }
}