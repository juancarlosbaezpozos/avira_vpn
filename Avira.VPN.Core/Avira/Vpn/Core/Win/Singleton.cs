namespace Avira.VPN.Core.Win
{
    public class Singleton<TInterface, TImplementation>
        where TInterface : class where TImplementation : TInterface, new()
    {
        private static TInterface instance;

        public static TInterface Instance
        {
            get { return instance ?? (instance = (TInterface)(object)new TImplementation()); }
            internal set { instance = value; }
        }
    }
}