using System.Runtime.InteropServices;

namespace Avira.VpnService
{
    public struct OBO_TOKEN
    {
        [MarshalAs(UnmanagedType.I4)] public OBO_TOKEN_TYPE Type;

        [MarshalAs(UnmanagedType.IUnknown)] public object Pncc;

        [MarshalAs(UnmanagedType.LPWStr)] public string Manufacturer;

        [MarshalAs(UnmanagedType.LPWStr)] public string Product;

        [MarshalAs(UnmanagedType.LPWStr)] public string DisplayName;

        [MarshalAs(UnmanagedType.Bool)] public bool Registered;
    }
}