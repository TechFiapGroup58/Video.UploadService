using Microsoft.EntityFrameworkCore;
using UploadService.Core.Entities;
using UploadService.Infrastructure.DataSources.Database.Mappings;

namespace UploadService.Infrastructure.DataSources.Database.Context;

public class UploadDbContext(DbContextOptions<UploadDbContext> options) : DbContext(options)
{
    public DbSet<VideoUpload> VideoUploads => Set<VideoUpload>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new VideoUploadMapping());
    }
}
