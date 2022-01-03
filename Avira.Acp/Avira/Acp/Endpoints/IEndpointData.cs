namespace Avira.Acp.Endpoints
{
    public interface IEndpointData
    {
        string Host { get; }

        string ChannelType { get; }

        string NamedPipeName { get; }
    }
}