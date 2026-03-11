using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;
using RabbitMQ.Client;
using UploadService.Core.Gateways;
using UploadService.Core.UseCases.Uploads;
using UploadService.Core.UseCases.Videos;
using UploadService.Infrastructure.DataSources.Database.Context;
using UploadService.Infrastructure.DataSources.Database.Repositories;
using UploadService.Infrastructure.DataSources.Messaging;
using UploadService.Infrastructure.DataSources.Storage;
using UploadService.Infrastructure.Security;

namespace UploadService.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataSources(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<UploadDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        // MinIO
        var minio = config.GetSection("Minio").Get<MinioSettings>()!;
        services.Configure<MinioSettings>(config.GetSection("Minio"));
        services.AddSingleton<IMinioClient>(_ =>
            new MinioClient()
                .WithEndpoint(minio.Endpoint)
                .WithCredentials(minio.AccessKey, minio.SecretKey)
                .WithSSL(minio.UseSSL)
                .Build());

        // RabbitMQ
        var rabbit = config.GetSection("RabbitMq").Get<RabbitMqSettings>()!;
        services.Configure<RabbitMqSettings>(config.GetSection("RabbitMq"));
        services.AddSingleton<IConnection>(_ =>
            new ConnectionFactory
            {
                HostName    = rabbit.Host,
                Port        = rabbit.Port,
                UserName    = rabbit.Username,
                Password    = rabbit.Password,
                VirtualHost = rabbit.VirtualHost
            }.CreateConnection("upload-service"));

        return services;
    }

    public static IServiceCollection AddGateways(this IServiceCollection services)
    {
        services.AddScoped<IVideoUploadGateway, VideoUploadRepository>();
        services.AddScoped<IStorageGateway, MinioStorageDataSource>();
        services.AddScoped<IVideoProcessingGateway, RabbitMqVideoProcessingDataSource>();
        services.AddSingleton<IVideoValidatorGateway, VideoValidatorDataSource>();
        return services;
    }

    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<IUploadVideoUseCase, UploadVideoUseCase>();
        services.AddScoped<IListUserVideosUseCase, ListUserVideosUseCase>();
        services.AddScoped<IGetVideoUseCase, GetVideoUseCase>();
        services.AddScoped<IGetVideoDownloadUrlUseCase, GetVideoDownloadUrlUseCase>();
        return services;
    }

    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration config)
    {
        var key = config["Jwt:SecretKey"]!;
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts => opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = config["Jwt:Issuer"],
                ValidAudience            = config["Jwt:Audience"],
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
            });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "FIAP X — Upload Service", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Bearer. Exemplo: 'Bearer {token}'",
                Name = "Authorization", In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey, Scheme = "Bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });
        return services;
    }
}
