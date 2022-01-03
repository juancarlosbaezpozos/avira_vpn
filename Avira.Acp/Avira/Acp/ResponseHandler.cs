using Avira.Acp.Messages;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    public delegate void ResponseHandler(Response response);

    public delegate void ResponseHandler<T>(Response<T> response);
}