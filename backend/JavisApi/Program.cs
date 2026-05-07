using System.Text;
using Hangfire;
using JavisApi.AI;
using JavisApi.Data;
using JavisApi.Jobs;
using JavisApi.Models;
using JavisApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Services
// ============================================================

// --- Database ---
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=javis.db"));

// --- Authentication ---
var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? "dev-secret-key-change-in-production-32chars";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "JavisApi",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "JavisApp",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// --- CORS ---
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// --- Hangfire (background jobs) ---
builder.Services.AddHangfire(config =>
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseInMemoryStorage());

builder.Services.AddHangfireServer(options =>
    options.WorkerCount = int.Parse(builder.Configuration["Worker:MaxJobs"] ?? "3"));

// --- HTTP Clients ---
builder.Services.AddHttpClient("ChromaDB", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ChromaDB:BaseUrl"] ?? "http://localhost:8001");
});
builder.Services.AddHttpClient();

// --- Application Services ---
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PermissionEngine>();
builder.Services.AddScoped<WikiService>();
builder.Services.AddScoped<ChromaService>();
builder.Services.AddScoped<ConfigService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<McpAuthService>();
builder.Services.AddScoped<KbService>();
builder.Services.AddScoped<ProviderRegistry>();
builder.Services.AddScoped<WikiAgent>();
builder.Services.AddScoped<WikiAnalyzer>();
builder.Services.AddSingleton<IStorageService, LocalStorageService>();

// --- Hangfire Job Types ---
builder.Services.AddScoped<IngestFileJob>();
builder.Services.AddScoped<CompileWikiJob>();

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Javis API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = []
    });
});

builder.Services.AddControllers();

// ============================================================
// Build app
// ============================================================

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAuthFilter()]
});

app.MapControllers();

app.MapGet("/api/files/{key}", async (string key, IStorageService storage) =>
{
    try { return Results.File(await storage.DownloadAsync(key), "application/octet-stream", key); }
    catch { return Results.NotFound(); }
});

await SeedDefaultAdminAsync(app);

app.Run();

// ============================================================
// Seed
// ============================================================

async Task SeedDefaultAdminAsync(WebApplication application)
{
    using var scope = application.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var authSvc = scope.ServiceProvider.GetRequiredService<AuthService>();
    var cfg = application.Configuration;

    await db.Database.EnsureCreatedAsync();

    if (!await db.Employees.AnyAsync())
    {
        var dept = new Department { Name = "Administration" };
        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        db.Employees.Add(new Employee
        {
            Name = "Administrator",
            Email = cfg["DefaultAdmin:Email"] ?? "admin@javis.local",
            PasswordHash = authSvc.HashPassword(cfg["DefaultAdmin:Password"] ?? "admin123"),
            Role = "admin",
            DepartmentId = dept.Id
        });
        await db.SaveChangesAsync();
        application.Logger.LogInformation("Default admin seeded.");
    }
}

public class HangfireAuthFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context) => true;
}
