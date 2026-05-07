using JavisApi.Data;
using JavisApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace JavisApi.Controllers;

/// <summary>
/// MCP (Model Context Protocol) endpoint for Claude Desktop.
/// Authenticates via Bearer token (ark_xxx).
/// </summary>
[ApiController]
[Route("mcp")]
public class McpController : ControllerBase
{
    private readonly McpAuthService _mcpAuth;
    private readonly WikiService _wiki;
    private readonly AppDbContext _db;
    private readonly PermissionEngine _permissions;

    public McpController(McpAuthService mcpAuth, WikiService wiki,
        AppDbContext db, PermissionEngine permissions)
    {
        _mcpAuth = mcpAuth;
        _wiki = wiki;
        _db = db;
        _permissions = permissions;
    }

    [HttpPost]
    public async Task<IActionResult> Handle([FromBody] McpRequest req)
    {
        // Authenticate MCP token
        var authHeader = Request.Headers.Authorization.ToString();
        var token = authHeader.StartsWith("Bearer ") ? authHeader[7..] : authHeader;

        var employee = await _mcpAuth.VerifyTokenAsync(token);
        if (employee is null)
            return Unauthorized(McpError(-32001, "Unauthorized: invalid MCP token"));

        return req.Method switch
        {
            "tools/list" => Ok(GetToolsList()),
            "tools/call" => await HandleToolCall(req, employee),
            _ => Ok(McpError(-32601, $"Method not found: {req.Method}"))
        };
    }

    private async Task<IActionResult> HandleToolCall(McpRequest req, Models.Employee employee)
    {
        var toolName = req.Params?.GetValueOrDefault("name")?.ToString() ?? "";
        var argsJson = req.Params?.GetValueOrDefault("arguments")?.ToString() ?? "{}";
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(argsJson) ?? [];

        var result = toolName switch
        {
            "search_wiki" => await SearchWiki(employee, args),
            "read_wiki_page" => await ReadWikiPage(employee, args),
            "list_wiki_pages" => await ListWikiPages(employee, args),
            _ => new { error = $"Unknown tool: {toolName}" }
        };

        return Ok(new
        {
            jsonrpc = "2.0",
            id = req.Id,
            result = new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(result) } } }
        });
    }

    private async Task<object> SearchWiki(Models.Employee employee, Dictionary<string, string> args)
    {
        var query = args.GetValueOrDefault("query", "");
        var results = await _wiki.FullTextSearchAsync(query, "global", null, 10);

        // Filter by employee permissions
        var filtered = results
            .Select(p => new { p.Slug, p.Title, p.Summary })
            .ToList();

        return new { results = filtered, count = filtered.Count };
    }

    private async Task<object> ReadWikiPage(Models.Employee employee, Dictionary<string, string> args)
    {
        var slug = args.GetValueOrDefault("slug", "");
        var page = await _wiki.GetBySlugAsync(slug);
        if (page is null) return new { error = $"Page not found: {slug}" };

        return new { page.Slug, page.Title, page.ContentMd, page.Summary };
    }

    private async Task<object> ListWikiPages(Models.Employee employee, Dictionary<string, string> args)
    {
        var scopeType = args.GetValueOrDefault("scope_type", "global");
        var index = await _wiki.BuildWikiIndexAsync(scopeType);
        return new { index };
    }

    private static object GetToolsList() => new
    {
        jsonrpc = "2.0",
        result = new
        {
            tools = new object[]
            {
                new { name = "search_wiki", description = "Search wiki pages by keyword",
                    inputSchema = new { type = "object",
                        properties = new { query = new { type = "string", description = "Search query" } },
                        required = new[] { "query" } } },
                new { name = "read_wiki_page", description = "Read full content of a wiki page",
                    inputSchema = new { type = "object",
                        properties = new { slug = new { type = "string", description = "Page slug" } },
                        required = new[] { "slug" } } },
                new { name = "list_wiki_pages", description = "List all available wiki pages",
                    inputSchema = new { type = "object", properties = new { } } }
            }
        }
    };

    private static object McpError(int code, string message) => new
    {
        jsonrpc = "2.0",
        error = new { code, message }
    };
}

public class McpRequest
{
    public string JsonRpc { get; set; } = "2.0";
    public string? Id { get; set; }
    public string Method { get; set; } = "";
    public Dictionary<string, object?>? Params { get; set; }
}
