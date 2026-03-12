using System.Text;
using Amazon.S3;
using UploadService.API.Middlewares;
using UploadService.Application.Interfaces;
using UploadService.Infrastructure.Data;
using UploadService.Infrastructure.Messaging;
using UploadService.Infrastructure.Repositories;
using UploadService.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Database ───────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── JWT (valida tokens emitidos pelo AuthService) ──────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(opt =>
    {
        opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opt.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opt => opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSection["Issuer"],
        ValidAudience            = jwtSection["Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(
                                       Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!))
    });

builder.Services.AddAuthorization();

// ── MinIO (compatível com S3) ──────────────────────────────────────────────
builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection("Minio"));
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var cfg = builder.Configuration.GetSection("Minio").Get<MinioSettings>()!;
    return new AmazonS3Client(cfg.AccessKey, cfg.SecretKey, new AmazonS3Config
    {
        ServiceURL            = $"{(cfg.UseSSL ? "https" : "http")}://{cfg.Endpoint}",
        ForcePathStyle        = true,
        AuthenticationRegion  = "us-east-1"
    });
});

// ── RabbitMQ ───────────────────────────────────────────────────────────────
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

// ── DI ─────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IVideoUploadRepository, VideoUploadRepository>();
builder.Services.AddScoped<IStorageService, MinioStorageService>();
builder.Services.AddSingleton<IVideoValidator, VideoValidator>();
builder.Services.AddScoped<IUploadService, UploadService.Application.Services.UploadService>();

// ── Swagger ────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FIAP X — Upload Service", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Bearer {token}",
        Name = "Authorization", In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey, Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        },
        Array.Empty<string>()
    }});
});

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

// ── App ────────────────────────────────────────────────────────────────────
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Upload Service v1"));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
