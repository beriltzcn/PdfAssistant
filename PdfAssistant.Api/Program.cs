
using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Services;
using PdfAssistant.Infrastructure.Pdf;
using PdfAssistant.Infrastructure.Storage;
using PdfAssistant.Infrastructure.Configuration;
using PdfAssistant.Infrastructure.Embeddings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();

builder.Services.Configure<OpenRouterOptions>(
    builder.Configuration.GetSection(OpenRouterOptions.SectionName));

builder.Services.AddHttpClient<IEmbeddingService, OpenRouterEmbeddingService>();

builder.Services.AddScoped<ITextChunker, TextChunker>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
builder.Services.AddScoped<ITextChunker, TextChunker>();

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
