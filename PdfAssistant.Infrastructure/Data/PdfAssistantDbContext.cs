
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

    public DbSet<DocumentChunkEntity> DocumentChunks =>
        Set<DocumentChunkEntity>();
}
