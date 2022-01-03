using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    public delegate void CollectionResponseHandler<T>(CollectionResponse<T> responseHandler);
}