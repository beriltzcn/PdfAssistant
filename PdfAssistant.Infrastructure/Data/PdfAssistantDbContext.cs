using Microsoft.EntityFrameworkCore;
using PdfAssistant.Infrastructure.Models;

namespace PdfAssistant.Infrastructure.Data;

public class PdfAssistantDbContext : DbContext
{
    public PdfAssistantDbContext(
        DbContextOptions<PdfAssistantDbContext> options)
        : base(options)
    {
    }

    public DbSet<PdfDocumentEntity> Documents =>
        Set<PdfDocumentEntity>();

    public DbSet<DocumentChunkEntity> DocumentChunks =>
        Set<DocumentChunkEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PdfDocumentEntity>(entity =>
        {
            entity.ToTable("Documents");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.StoredFileName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.OriginalFileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasMaxLength(32)
                .IsRequired();

            entity.HasIndex(x => x.StoredFileName)
                .IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}