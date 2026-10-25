using OpenAI.Embeddings;
using Qdrant.Client;
using RAG.Services.Chunking;
using RAG.Services.Document;
using RAG.Services.Embeddings;
using RAG.Services.Qdrantsvr;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
var host = builder.Configuration["Qdrant:Host"]??"localhost";
var port = Convert.ToInt32(builder.Configuration["Qdrant:Port"]??"6334");

var openAiApiKey =builder.Configuration["OpenAI:OpenAIKey"]
    ?? throw new InvalidOperationException(
        "Configure OpenAI:ApiKey using User Secrets or environment variables.");

var embeddingModel = builder.Configuration["OpenAI:EmbeddingModel"]
    ?? "text-embedding-3-small";

var configuredDimensions = builder.Configuration.GetValue<int>(
    "OpenAI:Dimensions", 1536);

if (configuredDimensions <= 0)
{
    throw new InvalidOperationException(
        "OpenAI:Dimensions must be greater than zero.");
}

builder.Services.AddSingleton( _ => new EmbeddingClient(embeddingModel, openAiApiKey));


builder.Services.AddSingleton(new QdrantClient(host:host ,port: port));
builder.Services.AddScoped<IQdrantService, QdrantService>();
builder.Services.AddSingleton<IDocumentLoadingService,
    DocumentLoadingService>();
builder.Services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();
builder.Services.AddSingleton<ITextChunker, TextChunker>();
builder.Services.AddScoped<DocumentIndexingService>();
builder.Services.AddScoped<IQudrantVectorStore, QdrantVectorStore>();

builder.Services.AddScoped<IEmbeddingService, OpenAIEmbeddingService>();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
