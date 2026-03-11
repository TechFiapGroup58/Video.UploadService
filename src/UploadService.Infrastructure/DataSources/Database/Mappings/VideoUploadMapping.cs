using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UploadService.Core.Entities;

namespace UploadService.Infrastructure.DataSources.Database.Mappings;

public sealed class VideoUploadMapping : IEntityTypeConfiguration<VideoUpload>
{
    public void Configure(EntityTypeBuilder<VideoUpload> b)
    {
        b.ToTable("video_uploads");
        b.HasKey(v => v.Id);
        b.Property(v => v.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(v => v.UserId).HasColumnName("user_id").IsRequired();
        b.Property(v => v.FileName).HasColumnName("file_name").HasMaxLength(512).IsRequired();
        b.Property(v => v.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(512).IsRequired();
        b.Property(v => v.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
        b.Property(v => v.FileSizeBytes).HasColumnName("file_size_bytes").IsRequired();
        b.Property(v => v.StoragePath).HasColumnName("storage_path").HasMaxLength(1024);
        b.Property(v => v.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        b.Property(v => v.ErrorMessage).HasColumnName("error_message").HasMaxLength(2048);
        b.Property(v => v.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.Property(v => v.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(v => v.UserId);
        b.HasIndex(v => v.Status);
    }
}
