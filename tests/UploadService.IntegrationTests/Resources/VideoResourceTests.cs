using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UploadService.API.Presenters;
using UploadService.IntegrationTests.Fixtures;
using Xunit;

namespace UploadService.IntegrationTests.Resources;

public sealed class VideoResourceTests(UploadWebAppFactory factory)
    : IClassFixture<UploadWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private HttpClient AuthClient(Guid? userId = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(userId ?? Guid.NewGuid()));
        return client;
    }

    private static string GenerateToken(Guid userId)
    {
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-secret-key-long-enough-for-hmac-256-bits!!"));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        };
        var token = new JwtSecurityToken(
            issuer: "fiapx-auth",
            audience: "fiapx-services",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static MultipartFormDataContent MakeFormFile(string filename = "video.mp4", string contentType = "video/mp4")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x00, 0x01, 0x02 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", filename);
        return content;
    }

    [Fact]
    public async Task Upload_ValidVideo_Returns201()
    {
        var client   = AuthClient();
        var response = await client.PostAsync("/api/videos", MakeFormFile());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = JsonSerializer.Deserialize<UploadPresenter>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        body!.Status.Should().Be("Queued");
        body.OriginalFileName.Should().Be("video.mp4");
    }

    [Fact]
    public async Task Upload_NoToken_Returns401()
    {
        var client   = factory.CreateClient();
        var response = await client.PostAsync("/api/videos", MakeFormFile());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_AuthenticatedUser_Returns200WithItems()
    {
        var userId = Guid.NewGuid();
        var client = AuthClient(userId);

        await client.PostAsync("/api/videos", MakeFormFile("a.mp4"));
        await client.PostAsync("/api/videos", MakeFormFile("b.mp4"));

        var response = await client.GetAsync("/api/videos");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = JsonSerializer.Deserialize<List<VideoPresenter>>(
            await response.Content.ReadAsStringAsync(), JsonOpts);
        items!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Get_ExistingVideo_Returns200()
    {
        var userId   = Guid.NewGuid();
        var client   = AuthClient(userId);

        var upload   = await client.PostAsync("/api/videos", MakeFormFile());
        var created  = JsonSerializer.Deserialize<UploadPresenter>(
            await upload.Content.ReadAsStringAsync(), JsonOpts);

        var response = await client.GetAsync($"/api/videos/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_AnotherUsersVideo_Returns403()
    {
        var owner  = Guid.NewGuid();
        var other  = Guid.NewGuid();

        var ownerClient  = AuthClient(owner);
        var upload       = await ownerClient.PostAsync("/api/videos", MakeFormFile());
        var created      = JsonSerializer.Deserialize<UploadPresenter>(
            await upload.Content.ReadAsStringAsync(), JsonOpts);

        var otherClient  = AuthClient(other);
        var response     = await otherClient.GetAsync($"/api/videos/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_NotExisting_Returns404()
    {
        var response = await AuthClient().GetAsync($"/api/videos/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
