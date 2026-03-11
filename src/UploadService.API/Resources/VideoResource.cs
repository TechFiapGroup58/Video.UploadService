using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UploadService.API.Extensions;
using UploadService.API.Presenters;
using UploadService.Core.UseCases.Uploads;
using UploadService.Core.UseCases.Videos;

namespace UploadService.API.Resources;

[ApiController]
[Route("api/videos")]
[Authorize]
[Produces("application/json")]
public sealed class VideoResource(
    IUploadVideoUseCase uploadUseCase,
    IListUserVideosUseCase listUseCase,
    IGetVideoUseCase getUseCase,
    IGetVideoDownloadUrlUseCase downloadUrlUseCase) : ControllerBase
{
    /// <summary>Faz upload de um vídeo. Aceita multipart/form-data.</summary>
    [HttpPost]
    [RequestSizeLimit(2L * 1024 * 1024 * 1024)] // 2 GB
    [ProducesResponseType(typeof(UploadPresenter), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();

        var input = new UploadVideoInput(
            User.GetUserId(),
            file.FileName,
            file.ContentType,
            file.Length,
            stream);

        var output = await uploadUseCase.ExecuteAsync(input, ct);
        return Created($"/api/videos/{output.Id}", UploadPresenterMapper.ToPresenter(output));
    }

    /// <summary>Lista todos os vídeos do usuário autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VideoPresenter>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var outputs = await listUseCase.ExecuteAsync(User.GetUserId(), ct);
        return Ok(outputs.Select(VideoPresenterMapper.ToPresenter).ToList());
    }

    /// <summary>Retorna detalhes de um vídeo específico.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VideoPresenter), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var output = await getUseCase.ExecuteAsync(id, User.GetUserId(), ct);
        return Ok(VideoPresenterMapper.ToPresenter(output));
    }

    /// <summary>Retorna URL pré-assinada para download temporário (15 min).</summary>
    [HttpGet("{id:guid}/download-url")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadUrl(Guid id, CancellationToken ct)
    {
        var url = await downloadUrlUseCase.ExecuteAsync(id, User.GetUserId(), ct);
        return Ok(new { url });
    }
}
