namespace JavisApi.Services;

/// <summary>
/// Local disk storage — for development.
/// Replace with MinioStorageService for production.
/// </summary>
public interface IStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType);
    Task<Stream> DownloadAsync(string key);
    Task DeleteAsync(string key);
    Task<string> GetUrlAsync(string key);
}

public class LocalStorageService : IStorageService
{
    private readonly string _basePath;
    private readonly IConfiguration _config;

    public LocalStorageService(IConfiguration config)
    {
        _config = config;
        _basePath = config["Storage:LocalPath"] ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JavisFiles");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        var key = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(_basePath, key);

        await using var fs = File.Create(filePath);
        await stream.CopyToAsync(fs);

        return key;
    }

    public Task<Stream> DownloadAsync(string key)
    {
        var filePath = Path.Combine(_basePath, key);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {key}");

        Stream stream = File.OpenRead(filePath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key)
    {
        var filePath = Path.Combine(_basePath, key);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    public Task<string> GetUrlAsync(string key)
    {
        var baseUrl = _config["Storage:BaseUrl"] ?? "http://localhost:5000";
        return Task.FromResult($"{baseUrl}/api/files/{key}");
    }
}
