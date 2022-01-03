using System.IO.Pipes;

namespace Avira.Win.Messaging
{
    public interface IAuthorizationChecker
    {
        bool Check(NamedPipeServerStream srv);
    }
}