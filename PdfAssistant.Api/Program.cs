
using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Services;
using PdfAssistant.Infrastructure.Pdf;
using PdfAssistant.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();

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
