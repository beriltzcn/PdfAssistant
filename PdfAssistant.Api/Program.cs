
using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Services;
using PdfAssistant.Infrastructure.Pdf;
using PdfAssistant.Infrastructure.Storage;
using PdfAssistant.Infrastructure.Configuration;
using PdfAssistant.Infrastructure.Embeddings;
using Microsoft.EntityFrameworkCore;
using PdfAssistant.Infrastructure.Data;
using PdfAssistant.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var databasePath = Path.Combine(
    builder.Environment.ContentRootPath,
    "App_Data",
    "pdfassistant.db");

Directory.CreateDirectory(
    Path.GetDirectoryName(databasePath)!);

builder.Services.AddDbContext<PdfAssistantDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();

builder.Services.Configure<OpenRouterOptions>(
    builder.Configuration.GetSection(OpenRouterOptions.SectionName));

builder.Services.AddHttpClient<IEmbeddingService, OpenRouterEmbeddingService>();

builder.Services.AddScoped<ITextChunker, TextChunker>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
builder.Services.AddScoped<ITextChunker, TextChunker>();
builder.Services.AddScoped<IDocumentEmbeddingService, DocumentEmbeddingService>();
builder.Services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
builder.Services.AddScoped<
    IDocumentSearchService,
    DocumentSearchService>();

var uploadFolder = Path.Combine(
    builder.Environment.ContentRootPath,
    "App_Data",
    "UploadedFiles");

builder.Services.AddScoped<IDocumentStorage>(
    _ => new LocalDocumentStorage(uploadFolder));

// Swagger servisleri
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
