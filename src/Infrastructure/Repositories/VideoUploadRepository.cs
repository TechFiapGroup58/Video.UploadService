using Microsoft.EntityFrameworkCore;
using UploadService.Application.Interfaces;
using UploadService.Domain.Entities;
using UploadService.Infrastructure.Data;

namespace UploadService.Infrastructure.Repositories;

public sealed class VideoUploadRepository : IVideoUploadRepository
{
    private readonly AppDbContext _db;

    public VideoUploadRepository(AppDbContext db) => _db = db;

    public async Task<VideoUpload?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.VideoUploads.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IEnumerable<VideoUpload>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _db.VideoUploads
            .AsNoTracking()
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(VideoUpload upload, CancellationToken ct = default)
    {
        await _db.VideoUploads.AddAsync(upload, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(VideoUpload upload, CancellationToken ct = default)
    {
        _db.VideoUploads.Update(upload);
        await _db.SaveChangesAsync(ct);
    }
}
