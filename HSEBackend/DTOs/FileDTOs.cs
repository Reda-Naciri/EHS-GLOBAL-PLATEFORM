using System.ComponentModel.DataAnnotations;

namespace HSEBackend.DTOs
{
    public class FileUploadResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? FileName { get; set; }
        public string? FileUrl { get; set; }
        public long FileSize { get; set; }
        public string? ContentType { get; set; }
    }

    public class FileDownloadResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public byte[]? FileData { get; set; }
        public string? ContentType { get; set; }
        public string? FileName { get; set; }
    }

    public class FileInfoDto
    {
        public string FileName { get; set; } = "";
        public string OriginalName { get; set; } = "";
        public long FileSize { get; set; }
        public string ContentType { get; set; } = "";
        public DateTime UploadedAt { get; set; }
        public string Category { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
    }

    public class FileUploadRequestDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;
        
        public string Category { get; set; } = "general";
        public string? Description { get; set; }
    }

    public class BulkFileUploadResponseDto
    {
        public int TotalFiles { get; set; }
        public int SuccessfulUploads { get; set; }
        public int FailedUploads { get; set; }
        public List<FileUploadResponseDto> Results { get; set; } = new();
    }
}