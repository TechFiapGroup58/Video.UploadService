using Microsoft.EntityFrameworkCore;
using UploadService.Domain.Entities;

namespace UploadService.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<VideoUpload> VideoUploads => Set<VideoUpload>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<VideoUpload>(e =>
        {
            e.ToTable("video_uploads");
            e.HasKey(v => v.Id);
            e.Property(v => v.UserId).IsRequired();
            e.Property(v => v.OriginalFileName).HasMaxLength(512).IsRequired();
            e.Property(v => v.StoredFileName).HasMaxLength(512).IsRequired();
            e.Property(v => v.ContentType).HasMaxLength(100).IsRequired();
            e.Property(v => v.FileSizeBytes).IsRequired();
            e.Property(v => v.StoragePath).HasMaxLength(1024);
            e.Property(v => v.Status).HasConversion<int>().IsRequired();
            e.Property(v => v.ErrorMessage).HasMaxLength(2048);
            e.Property(v => v.CreatedAt).HasDefaultValueSql("NOW()");
            e.Property(v => v.UpdatedAt).HasDefaultValueSql("NOW()");
            e.HasIndex(v => v.UserId);
        });
    }
}
