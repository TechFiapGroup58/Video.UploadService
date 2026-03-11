using System.Security.Claims;

namespace UploadService.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? principal.FindFirstValue("sub");

        if (Guid.TryParse(value, out var id)) return id;

        throw new UnauthorizedAccessException("Token inválido: claim 'sub' ausente.");
    }
}
