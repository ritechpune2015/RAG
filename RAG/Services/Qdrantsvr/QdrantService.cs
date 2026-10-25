using Microsoft.AspNetCore.Http.HttpResults;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace RAG.Services.Qdrantsvr
{
    public class QdrantService:IQdrantService
    {
        private readonly QdrantClient _client;

        public QdrantService(QdrantClient client)
        {
            _client = client;
        }

        public async Task CreateCollection(string collectionname)
        {
            if (!await _client.CollectionExistsAsync(collectionname))
            {
                await _client.CreateCollectionAsync(collectionName: collectionname, vectorsConfig: new VectorParams
                {
                    Size = 1536,
                    Distance = Distance.Cosine
                });
            }
        }
        

    }
}
