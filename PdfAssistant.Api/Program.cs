
using Microsoft.EntityFrameworkCore;
using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Services;
using PdfAssistant.Infrastructure.Chat;
using PdfAssistant.Infrastructure.Configuration;
using PdfAssistant.Infrastructure.Data;
using PdfAssistant.Infrastructure.Embeddings;
using PdfAssistant.Infrastructure.Pdf;
using PdfAssistant.Infrastructure.Repositories;
using PdfAssistant.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

const string clientCorsPolicy = "ClientDevelopment";

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy(clientCorsPolicy, policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var databasePath = Path.Combine(
    builder.Environment.ContentRootPath,
    "App_Data",
    "pdfassistant.db");

Directory.CreateDirectory(
    Path.GetDirectoryName(databasePath)!);

builder.Services.AddDbContext<PdfAssistantDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

builder.Services.Configure<OpenRouterOptions>(
    builder.Configuration.GetSection(OpenRouterOptions.SectionName));

builder.Services.AddHttpClient<IEmbeddingService, OpenRouterEmbeddingService>();

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<
    IDocumentProcessingService,
    DocumentProcessingService>();
builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
builder.Services.AddScoped<ITextChunker, TextChunker>();
builder.Services.AddScoped<IDocumentEmbeddingService, DocumentEmbeddingService>();
builder.Services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
builder.Services.AddScoped<
    IDocumentSearchService,
    DocumentSearchService>();
builder.Services.AddHttpClient<
    IChatCompletionService,
    OpenRouterChatCompletionService>();
builder.Services.AddScoped<
    IDocumentAnswerService,
    DocumentAnswerService>();

var uploadFolder = Path.Combine(
    builder.Environment.ContentRootPath,
    "App_Data",
    "UploadedFiles");

builder.Services.AddScoped<IDocumentStorage>(
    _ => new LocalDocumentStorage(uploadFolder));

// Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(clientCorsPolicy);
app.UseAuthorization();

app.MapControllers();
app.MapGet(
    "/health",
    () => Results.Ok(new
    {
        Status = "ok",
        Service = "PdfAssistant.Api"
    }));

app.Run();
