using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[assembly: AssemblyProduct("Avira Phantom VPN")]
[assembly: AssemblyCompany("Avira Operations GmbH & Co. KG")]
[assembly: AssemblyCopyright("Copyright © 2015 Avira Operations GmbH & Co. KG and its Licensors")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyTitle("VPN.Core")]
[assembly: ComVisible(false)]
[assembly: Guid("ac7b3716-4b2c-494a-8f21-e3427eef7806")]
[assembly: InternalsVisibleTo("Avira.VPN.Core.ModuleTest")]
[assembly: InternalsVisibleTo("Avira.VPN.Core.UnitTest")]
[assembly: InternalsVisibleTo("Avira.VpnService.UnitTest")]
[assembly:
    SuppressMessage("Stylecop.CSharp.MaintainabilityRules", "SA1401:.Maintainability", Scope = "Namespace",
        Target = "Avira.VPN.Core", Justification = "Interop")]
[assembly: AssemblyVersion("2.37.7.25881")]