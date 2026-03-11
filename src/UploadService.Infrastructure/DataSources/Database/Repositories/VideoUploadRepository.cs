using Microsoft.EntityFrameworkCore;
using UploadService.Core.Entities;
using UploadService.Core.Gateways;
using UploadService.Infrastructure.DataSources.Database.Context;

namespace UploadService.Infrastructure.DataSources.Database.Repositories;

public sealed class VideoUploadRepository(UploadDbContext db) : IVideoUploadGateway
{
    public Task<VideoUpload?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => db.VideoUploads.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IReadOnlyList<VideoUpload>> FindByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await db.VideoUploads
            .AsNoTracking()
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(ct);

    public async Task SaveAsync(VideoUpload upload, CancellationToken ct = default)
    {
        await db.VideoUploads.AddAsync(upload, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(VideoUpload upload, CancellationToken ct = default)
    {
        db.VideoUploads.Update(upload);
        await db.SaveChangesAsync(ct);
    }
}
