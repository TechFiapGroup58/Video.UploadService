using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using UploadService.Application.DTOs;
using UploadService.Application.Interfaces;
using UploadService.Infrastructure.Data;
using Xunit;

namespace UploadService.Tests.Integration.Controllers;

// ── Factory ──────────────────────────────────────────────────────────────────

public sealed class UploadWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    public Mock<IStorageService>   StorageMock   { get; } = new();
    public Mock<IMessagePublisher> PublisherMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "test-secret-key-at-least-32-chars-long!!",
                ["Jwt:Issuer"]    = "fiapx-auth",
                ["Jwt:Audience"]  = "fiapx-services",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Troca Postgres por InMemory
            var db = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (db is not null) services.Remove(db);
            services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(_dbName));

            // Remove dependências externas reais
            var storage   = services.SingleOrDefault(d => d.ServiceType == typeof(IStorageService));
            var publisher = services.SingleOrDefault(d => d.ServiceType == typeof(IMessagePublisher));
            var validator = services.SingleOrDefault(d => d.ServiceType == typeof(IVideoValidator));
            if (storage   is not null) services.Remove(storage);
            if (publisher is not null) services.Remove(publisher);
            if (validator is not null) services.Remove(validator);

            // Mocks: storage retorna path, publisher não faz nada, validator aceita tudo
            StorageMock
                .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), default))
                .ReturnsAsync("videos/test-file.mp4");

            services.AddSingleton(StorageMock.Object);
            services.AddSingleton(PublisherMock.Object);
            services.AddSingleton<IVideoValidator>(new PassthroughValidator());

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        });
    }

    // Validator que aceita tudo — isola a lógica de negócio nos testes unitários
    private sealed class PassthroughValidator : IVideoValidator
    {
        public void Validate(string contentType, long fileSizeBytes) { }
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public sealed class UploadsControllerTests : IClassFixture<UploadWebAppFactory>
{
    private readonly UploadWebAppFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private const string JwtSecret = "test-secret-key-at-least-32-chars-long!!";

    public UploadsControllerTests(UploadWebAppFactory factory) => _factory = factory;

    private HttpClient AuthClient(Guid? userId = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(userId ?? Guid.NewGuid()));
        return client;
    }

    private static string GenerateToken(Guid userId)
    {
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
        };
        var token = new JwtSecurityToken(
            issuer:             "fiapx-auth",
            audience:           "fiapx-services",
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static MultipartFormDataContent MakeVideoFile(string name = "video.mp4")
    {
        var form    = new MultipartFormDataContent();
        var content = new ByteArrayContent(new byte[] { 0x00, 0x01, 0x02 });
        content.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
        form.Add(content, "file", name);
        return form;
    }

    // ── POST /api/uploads ────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_WithValidToken_Returns201()
    {
        var response = await AuthClient().PostAsync("/api/uploads", MakeVideoFile());

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = JsonSerializer.Deserialize<UploadVideoResponse>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        body!.OriginalFileName.Should().Be("video.mp4");
        body.StoragePath.Should().Be("videos/test-file.mp4");
    }

    [Fact]
    public async Task Upload_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().PostAsync("/api/uploads", MakeVideoFile());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Upload_NoFile_Returns400()
    {
        var response = await AuthClient().PostAsync("/api/uploads", new MultipartFormDataContent());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/uploads ─────────────────────────────────────────────────────

    [Fact]
    public async Task List_WithValidToken_Returns200()
    {
        var client = AuthClient();
        await client.PostAsync("/api/uploads", MakeVideoFile("a.mp4"));
        await client.PostAsync("/api/uploads", MakeVideoFile("b.mp4"));

        var response = await client.GetAsync("/api/uploads");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = JsonSerializer.Deserialize<List<VideoUploadSummary>>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        items!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/uploads");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_ReturnsOnlyCurrentUserUploads()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        await AuthClient(userA).PostAsync("/api/uploads", MakeVideoFile("a.mp4"));
        await AuthClient(userB).PostAsync("/api/uploads", MakeVideoFile("b.mp4"));

        var response = await AuthClient(userA).GetAsync("/api/uploads");
        var items    = JsonSerializer.Deserialize<List<VideoUploadSummary>>(
            await response.Content.ReadAsStringAsync(), JsonOpts);

        items!.Should().OnlyContain(i => i.OriginalFileName == "a.mp4");
    }

    // ── GET /api/uploads/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task GetById_OwnUpload_Returns200()
    {
        var client   = AuthClient();
        var upload   = await client.PostAsync("/api/uploads", MakeVideoFile());
        var created  = JsonSerializer.Deserialize<UploadVideoResponse>(
            await upload.Content.ReadAsStringAsync(), JsonOpts);

        var response = await client.GetAsync($"/api/uploads/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_AnotherUsersUpload_Returns403()
    {
        var ownerClient  = AuthClient();
        var upload       = await ownerClient.PostAsync("/api/uploads", MakeVideoFile());
        var created      = JsonSerializer.Deserialize<UploadVideoResponse>(
            await upload.Content.ReadAsStringAsync(), JsonOpts);

        var response = await AuthClient().GetAsync($"/api/uploads/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var response = await AuthClient().GetAsync($"/api/uploads/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Health ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
