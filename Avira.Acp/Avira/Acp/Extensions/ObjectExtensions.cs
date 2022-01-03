namespace Avira.Acp.Extensions
{
    public static class ObjectExtensions
    {
        public static string GetAcpTypeName(this object obj)
        {
            return obj.GetType().GetAcpTypeName();
        }
    }
}