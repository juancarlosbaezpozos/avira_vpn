namespace Avira.Acp.Endpoints
{
    public interface IEndpointRepository
    {
        string Add(IEndpoint endpoint);

        void Remove(IEndpoint endpoint);
    }
}