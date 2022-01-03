namespace Avira.Acp.Endpoints
{
    public interface IEndpoint : IEndpointData
    {
        void Close();
    }
}