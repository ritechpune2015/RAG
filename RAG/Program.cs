using RAG.Services.Chunking;
using RAG.Services.Document;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDocumentLoadingService,
    DocumentLoadingService>();
builder.Services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();
builder.Services.AddSingleton<ITextChunker, TextChunker>();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
