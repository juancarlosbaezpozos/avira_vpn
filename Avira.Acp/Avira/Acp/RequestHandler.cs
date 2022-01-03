using Avira.Acp.Messages;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    public delegate void RequestHandler(Request request);

    public delegate void RequestHandler<T>(Request<T> resquest);
}