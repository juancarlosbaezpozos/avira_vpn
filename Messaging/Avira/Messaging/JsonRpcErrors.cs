namespace Avira.Messaging
{
    public enum JsonRpcErrors
    {
        ParseError = -32700,
        InvalidRequest = -32600,
        MethodNotFound = -32601,
        InvalidParams = -32602,
        InternalError = -32603,
        ConnectionBroken = -32604,
        ServerException = -32000
    }
}