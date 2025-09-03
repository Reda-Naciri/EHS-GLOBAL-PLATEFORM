using HSEBackend.DTOs;

namespace HSEBackend.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileService> _logger;
        private readonly IConfiguration _configuration;
        
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv" };
        private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB

        public FileService(IWebHostEnvironment environment, ILogger<FileService> logger, IConfiguration configuration)
        {
            _environment = environment;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<FileUploadResponseDto> UploadFileAsync(IFormFile file, string category = "general")
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return new FileUploadResponseDto
                    {
                        Success = false,
                        Message = "No file provided"
                    };
                }

                if (!IsValidFileType(file.FileName))
                {
                    return new FileUploadResponseDto
                    {
                        Success = false,
                        Message = "File type not allowed"
                    };
                }

                if (!IsValidFileSize(file.Length))
                {
                    return new FileUploadResponseDto
                    {
                        Success = false,
                        Message = $"File size exceeds maximum limit of {_maxFileSize / 1024 / 1024}MB"
                    };
                }

                // Create upload directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads", category);
                Directory.CreateDirectory(uploadsPath);

                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("File uploaded successfully: {FileName}", uniqueFileName);

                return new FileUploadResponseDto
                {
                    Success = true,
                    Message = "File uploaded successfully",
                    FileName = uniqueFileName,
                    FileUrl = $"/api/files/{category}/{uniqueFileName}",
                    FileSize = file.Length,
                    ContentType = file.ContentType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
                return new FileUploadResponseDto
                {
                    Success = false,
                    Message = "Error uploading file"
                };
            }
        }

        public async Task<FileDownloadResponseDto> DownloadFileAsync(string fileName)
        {
            try
            {
                // Search for file in all categories
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                var filePath = FindFileInDirectory(uploadsPath, fileName);

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    return new FileDownloadResponseDto
                    {
                        Success = false,
                        Message = "File not found"
                    };
                }

                var fileData = await File.ReadAllBytesAsync(filePath);
                var contentType = GetContentType(fileName);

                return new FileDownloadResponseDto
                {
                    Success = true,
                    Message = "File retrieved successfully",
                    FileData = fileData,
                    ContentType = contentType,
                    FileName = fileName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {FileName}", fileName);
                return new FileDownloadResponseDto
                {
                    Success = false,
                    Message = "Error downloading file"
                };
            }
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            try
            {
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                var filePath = FindFileInDirectory(uploadsPath, fileName);

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    return false;
                }

                File.Delete(filePath);
                _logger.LogInformation("File deleted successfully: {FileName}", fileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FileName}", fileName);
                return false;
            }
        }

        public async Task<IEnumerable<FileInfoDto>> GetFilesAsync(string category = "")
        {
            try
            {
                var files = new List<FileInfoDto>();
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                
                if (!Directory.Exists(uploadsPath))
                {
                    return files;
                }

                var searchPath = string.IsNullOrEmpty(category) ? uploadsPath : Path.Combine(uploadsPath, category);
                
                if (!Directory.Exists(searchPath))
                {
                    return files;
                }

                var fileInfos = Directory.GetFiles(searchPath, "*", SearchOption.AllDirectories)
                    .Select(f => new FileInfo(f))
                    .ToList();

                foreach (var fileInfo in fileInfos)
                {
                    var relativePath = Path.GetRelativePath(uploadsPath, fileInfo.FullName);
                    var categoryName = Path.GetDirectoryName(relativePath) ?? "general";
                    
                    files.Add(new FileInfoDto
                    {
                        FileName = fileInfo.Name,
                        OriginalName = fileInfo.Name,
                        FileSize = fileInfo.Length,
                        ContentType = GetContentType(fileInfo.Name),
                        UploadedAt = fileInfo.CreationTime,
                        Category = categoryName,
                        DownloadUrl = $"/api/files/{categoryName}/{fileInfo.Name}"
                    });
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files for category: {Category}", category);
                return new List<FileInfoDto>();
            }
        }

        public async Task<long> GetTotalFileSizeAsync()
        {
            try
            {
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                
                if (!Directory.Exists(uploadsPath))
                {
                    return 0;
                }

                var files = Directory.GetFiles(uploadsPath, "*", SearchOption.AllDirectories);
                return files.Sum(f => new FileInfo(f).Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total file size");
                return 0;
            }
        }

        public bool IsValidFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _allowedExtensions.Contains(extension);
        }

        public bool IsValidFileSize(long fileSize)
        {
            return fileSize <= _maxFileSize;
        }

        private string? FindFileInDirectory(string rootPath, string fileName)
        {
            try
            {
                var files = Directory.GetFiles(rootPath, fileName, SearchOption.AllDirectories);
                return files.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }
    }
}