using HSEBackend.DTOs;
using HSEBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/files")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(IFileService fileService, ILogger<FilesController> logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        /// <summary>
        /// Upload a single file
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromQuery] string category = "general")
        {
            try
            {
                var result = await _fileService.UploadFileAsync(file, category);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, new { message = "Error uploading file" });
            }
        }

        /// <summary>
        /// Upload multiple files
        /// </summary>
        [HttpPost("upload/bulk")]
        public async Task<IActionResult> UploadFiles([FromForm] List<IFormFile> files, [FromQuery] string category = "general")
        {
            try
            {
                var response = new BulkFileUploadResponseDto
                {
                    TotalFiles = files.Count
                };

                foreach (var file in files)
                {
                    var result = await _fileService.UploadFileAsync(file, category);
                    response.Results.Add(result);
                    
                    if (result.Success)
                        response.SuccessfulUploads++;
                    else
                        response.FailedUploads++;
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading multiple files");
                return StatusCode(500, new { message = "Error uploading files" });
            }
        }

        /// <summary>
        /// Download a file
        /// </summary>
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            try
            {
                var result = await _fileService.DownloadFileAsync(fileName);
                
                if (!result.Success || result.FileData == null)
                {
                    return NotFound(new { message = result.Message });
                }

                return File(result.FileData, result.ContentType ?? "application/octet-stream", result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {FileName}", fileName);
                return StatusCode(500, new { message = "Error downloading file" });
            }
        }

        /// <summary>
        /// Get file by category and filename (for direct access)
        /// </summary>
        [HttpGet("{category}/{fileName}")]
        public async Task<IActionResult> GetFile(string category, string fileName)
        {
            try
            {
                var result = await _fileService.DownloadFileAsync(fileName);
                
                if (!result.Success || result.FileData == null)
                {
                    return NotFound(new { message = result.Message });
                }

                return File(result.FileData, result.ContentType ?? "application/octet-stream", result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file: {Category}/{FileName}", category, fileName);
                return StatusCode(500, new { message = "Error retrieving file" });
            }
        }

        /// <summary>
        /// Delete a file
        /// </summary>
        [HttpDelete("{fileName}")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            try
            {
                var result = await _fileService.DeleteFileAsync(fileName);
                
                if (!result)
                {
                    return NotFound(new { message = "File not found" });
                }

                return Ok(new { message = "File deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FileName}", fileName);
                return StatusCode(500, new { message = "Error deleting file" });
            }
        }

        /// <summary>
        /// Get all files with optional category filter
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> GetFiles([FromQuery] string category = "")
        {
            try
            {
                var files = await _fileService.GetFilesAsync(category);
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files for category: {Category}", category);
                return StatusCode(500, new { message = "Error retrieving files" });
            }
        }

        /// <summary>
        /// Get file storage statistics
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> GetFileStats()
        {
            try
            {
                var allFiles = await _fileService.GetFilesAsync();
                var totalSize = await _fileService.GetTotalFileSizeAsync();
                
                var stats = new
                {
                    totalFiles = allFiles.Count(),
                    totalSize = totalSize,
                    totalSizeMB = Math.Round(totalSize / 1024.0 / 1024.0, 2),
                    filesByCategory = allFiles.GroupBy(f => f.Category)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    averageFileSize = allFiles.Any() ? allFiles.Average(f => f.FileSize) : 0
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file statistics");
                return StatusCode(500, new { message = "Error retrieving file statistics" });
            }
        }

        /// <summary>
        /// Get allowed file types and size limits
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetFileConfig()
        {
            try
            {
                var config = new
                {
                    allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv" },
                    maxFileSize = 10 * 1024 * 1024, // 10MB
                    maxFileSizeMB = 10,
                    allowedCategories = new[] { "general", "reports", "actions", "attachments", "images" }
                };

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file configuration");
                return StatusCode(500, new { message = "Error retrieving file configuration" });
            }
        }
    }
}