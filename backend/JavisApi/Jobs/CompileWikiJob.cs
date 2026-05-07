using JavisApi.AI;
using JavisApi.Data;
using JavisApi.Services;

namespace JavisApi.Jobs;

/// <summary>
/// Hangfire background job: Compile wiki pages from an ingested source.
/// Equivalent to Python arq worker's compile_wiki_task.
/// </summary>
public class CompileWikiJob
{
    private readonly AppDbContext _db;
    private readonly WikiAgent _agent;
    private readonly WikiAnalyzer _analyzer;
    private readonly AuditService _audit;
    private readonly ILogger<CompileWikiJob> _logger;

    public CompileWikiJob(
        AppDbContext db,
        WikiAgent agent,
        WikiAnalyzer analyzer,
        AuditService audit,
        ILogger<CompileWikiJob> logger)
    {
        _db = db;
        _agent = agent;
        _analyzer = analyzer;
        _audit = audit;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid sourceId)
    {
        var source = await _db.Sources.FindAsync(sourceId);
        if (source is null) return;

        try
        {
            source.Status = "processing";
            source.Progress = 20;
            source.ProgressMessage = "Analyzing document structure...";
            await _db.SaveChangesAsync();

            // Run wiki agent loop
            await _agent.RunAsync(sourceId);

            // Log to audit trail
            await _audit.LogAsync(
                principalId: source.ContributedByEmployeeId ?? Guid.Empty,
                action: "compile",
                resourceType: "source",
                resourceId: sourceId.ToString(),
                decision: "allow",
                reason: "Wiki compilation complete",
                principalType: "agent"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wiki compilation failed for source {SourceId}", sourceId);

            source = await _db.Sources.FindAsync(sourceId);
            if (source is not null)
            {
                source.Status = "error";
                source.ErrorMessage = $"Compilation failed: {ex.Message}";
                source.Progress = 0;
                await _db.SaveChangesAsync();
            }
        }
    }
}
