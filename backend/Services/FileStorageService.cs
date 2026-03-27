namespace VacationRequestApi.Services
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, int vacationRequestId);
        Task<bool> DeleteFileAsync(string filePath);
        Task<(byte[] fileBytes, string contentType, string fileName)> GetFileAsync(string filePath);
        bool IsValidFileType(string contentType);
        bool IsValidFileSize(long fileSize);
    }

    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileStorageService> _logger;
        private readonly long _maxFileSize;
        private readonly string _uploadPath;
        private readonly List<string> _allowedContentTypes;

        public FileStorageService(
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<FileStorageService> logger)
        {
            _environment = environment;
            _configuration = configuration;
            _logger = logger;

            _maxFileSize = _configuration.GetValue<long>("FileUpload:MaxFileSizeBytes", 10 * 1024 * 1024); // Default 10MB
            _uploadPath = Path.Combine(_environment.ContentRootPath, "uploads");
            
            _allowedContentTypes = new List<string>
            {
                "application/pdf",
                "image/jpeg",
                "image/jpg",
                "image/png",
                "image/gif",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.ms-excel",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };

            // Create upload directory if it doesn't exist
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
                _logger.LogInformation("Created upload directory: {Path}", _uploadPath);
            }
        }

        public bool IsValidFileType(string contentType)
        {
            return _allowedContentTypes.Contains(contentType.ToLower());
        }

        public bool IsValidFileSize(long fileSize)
        {
            return fileSize > 0 && fileSize <= _maxFileSize;
        }

        public async Task<string> SaveFileAsync(IFormFile file, int vacationRequestId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("Fail on tühi.");
                }

                if (!IsValidFileType(file.ContentType))
                {
                    throw new ArgumentException($"Failitüüp {file.ContentType} ei ole lubatud.");
                }

                if (!IsValidFileSize(file.Length))
                {
                    var maxSizeMb = _maxFileSize / (1024 * 1024);
                    throw new ArgumentException($"Fail on liiga suur. Maksimaalne suurus on {maxSizeMb}MB.");
                }

                // Create subdirectory for this vacation request
                var requestFolder = Path.Combine(_uploadPath, $"request_{vacationRequestId}");
                if (!Directory.Exists(requestFolder))
                {
                    Directory.CreateDirectory(requestFolder);
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(requestFolder, uniqueFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation(
                    "File saved: {FileName} ({Size} bytes) for request {RequestId}",
                    file.FileName, file.Length, vacationRequestId
                );

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save file for request {RequestId}", vacationRequestId);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    _logger.LogInformation("File deleted: {FilePath}", filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<(byte[] fileBytes, string contentType, string fileName)> GetFileAsync(string filePath)
        {
            try
            {
                // Validate path to prevent directory traversal
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Faili ei leitud.", filePath);
                }

                var fullPath = Path.GetFullPath(filePath);
                var allowedBasePath = Path.GetFullPath(_uploadPath);
                
                if (!fullPath.StartsWith(allowedBasePath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Keelatud failitee.");
                }

                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var fileName = Path.GetFileName(filePath);
                var contentType = GetContentType(fileName);

                return (fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve file: {FilePath}", filePath);
                throw;
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }
}
