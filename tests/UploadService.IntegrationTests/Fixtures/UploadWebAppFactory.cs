using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RabbitMQ.Client;
using UploadService.Core.Gateways;
using UploadService.Infrastructure.DataSources.Database.Context;

namespace UploadService.IntegrationTests.Fixtures;

public sealed class UploadWebAppFactory : WebApplicationFactory<Program>
{
    // Storage e messaging são substituídos por mocks
    public Mock<IStorageGateway>         StorageMock     { get; } = new();
    public Mock<IVideoProcessingGateway> ProcessingMock  { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"]  = "test-secret-key-long-enough-for-hmac-256-bits!!",
                ["Jwt:Issuer"]     = "fiapx-auth",
                ["Jwt:Audience"]   = "fiapx-services",
            });
        });

        builder.ConfigureServices(services =>
        {
            // ── InMemory DB ────────────────────────────────────────────────────
            var dbDesc = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<UploadDbContext>));
            if (dbDesc is not null) services.Remove(dbDesc);
            services.AddDbContext<UploadDbContext>(o => o.UseInMemoryDatabase($"UploadDb_{Guid.NewGuid()}"));

            // ── Remover RabbitMQ e MinIO reais ────────────────────────────────
            RemoveService<IConnection>(services);
            RemoveService<IStorageGateway>(services);
            RemoveService<IVideoProcessingGateway>(services);

            // ── Mock Storage ──────────────────────────────────────────────────
            StorageMock.Setup(s => s.UploadAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), default))
                .ReturnsAsync("videos/test-file.mp4");

            services.AddSingleton(StorageMock.Object);
            services.AddSingleton(ProcessingMock.Object);

            // ── Seed DB ────────────────────────────────────────────────────────
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            scope.ServiceProvider.GetRequiredService<UploadDbContext>().Database.EnsureCreated();
        });
    }

    private static void RemoveService<T>(IServiceCollection services)
    {
        var desc = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (desc is not null) services.Remove(desc);
    }
}
