using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UploadService.Application.DTOs;
using UploadService.Application.Interfaces;

namespace UploadService.API.Controllers;

[ApiController]
[Route("api/uploads")]
[Authorize]
[Produces("application/json")]
public sealed class UploadsController : ControllerBase
{
    private readonly IUploadService _uploadService;

    public UploadsController(IUploadService uploadService)
        => _uploadService = uploadService;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? throw new System.UnauthorizedAccessException("Token inválido."));

    /// <summary>Faz upload de um vídeo. Aceita multipart/form-data com campo 'file'.</summary>
    [HttpPost]
    [RequestSizeLimit(2L * 1024 * 1024 * 1024)]
    [ProducesResponseType(typeof(UploadVideoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();

        var response = await _uploadService.UploadAsync(
            CurrentUserId,
            file.FileName,
            file.ContentType,
            file.Length,
            stream,
            ct);

        return Created($"/api/uploads/{response.Id}", response);
    }

    /// <summary>Lista todos os uploads do usuário autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<VideoUploadSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await _uploadService.ListByUserAsync(CurrentUserId, ct);
        return Ok(items);
    }

    /// <summary>Retorna detalhes de um upload específico.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VideoUploadSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await _uploadService.GetByIdAsync(id, CurrentUserId, ct);
        return Ok(item);
    }
}
