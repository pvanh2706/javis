using Hangfire;
using JavisApi.AI;
using JavisApi.Data;
using JavisApi.Services;
using Microsoft.EntityFrameworkCore;

namespace JavisApi.Jobs;

/// <summary>
/// Hangfire background job: Extract text from uploaded file.
/// Equivalent to Python arq worker's ingest_file_task.
/// </summary>
public class IngestFileJob
{
    private readonly AppDbContext _db;
    private readonly IStorageService _storage;
    private readonly KbService _kb;
    private readonly IBackgroundJobClient _jobClient;
    private readonly ILogger<IngestFileJob> _logger;

    public IngestFileJob(
        AppDbContext db,
        IStorageService storage,
        KbService kb,
        IBackgroundJobClient jobClient,
        ILogger<IngestFileJob> logger)
    {
        _db = db;
        _storage = storage;
        _kb = kb;
        _jobClient = jobClient;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid sourceId)
    {
        var source = await _db.Sources.FindAsync(sourceId);
        if (source is null)
        {
            _logger.LogWarning("Source {SourceId} not found", sourceId);
            return;
        }

        try
        {
            source.Status = "processing";
            source.Progress = 10;
            source.ProgressMessage = "Extracting text...";
            await _db.SaveChangesAsync();

            string extractedText;

            if (source.SourceType == "url" && !string.IsNullOrEmpty(source.Url))
            {
                extractedText = await _kb.ExtractUrlAsync(source.Url);
            }
            else if (!string.IsNullOrEmpty(source.MinioKey))
            {
                await using var stream = await _storage.DownloadAsync(source.MinioKey);
                extractedText = await _kb.ExtractFromFileAsync(stream, source.FileName ?? "file");
            }
            else
            {
                throw new InvalidOperationException("No file or URL to process");
            }

            source.FullText = extractedText;
            source.Progress = 20;
            source.ProgressMessage = "Text extracted. Queuing wiki compilation...";
            await _db.SaveChangesAsync();

            // Enqueue wiki compilation
            _jobClient.Enqueue<CompileWikiJob>(j => j.ExecuteAsync(sourceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingest failed for source {SourceId}", sourceId);
            source.Status = "error";
            source.ErrorMessage = ex.Message;
            source.Progress = 0;
            await _db.SaveChangesAsync();
        }
    }
}
