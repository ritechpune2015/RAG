namespace RAG.Services.Qdrantsvr
{
    public interface IQdrantService
    {
        Task CreateCollection(string collectionname);


    }
}
