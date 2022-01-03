using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    public delegate void CollectionRequestHandler<T>(CollectionRequest<T> collectionRequest);
}