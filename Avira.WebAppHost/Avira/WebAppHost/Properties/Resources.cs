using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Avira.WebAppHost.Properties
{
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [DebuggerNonUserCode]
    [CompilerGenerated]
    internal class Resources
    {
        private static ResourceManager resourceMan;

        private static CultureInfo resourceCulture;

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static ResourceManager ResourceManager
        {
            get
            {
                if (resourceMan == null)
                {
                    resourceMan = new ResourceManager("Avira.WebAppHost.Properties.Resources",
                        typeof(Resources).Assembly);
                }

                return resourceMan;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static CultureInfo Culture
        {
            get { return resourceCulture; }
            set { resourceCulture = value; }
        }

        internal static Icon appIcon => (Icon)ResourceManager.GetObject("appIcon", resourceCulture);

        internal static Icon Connected => (Icon)ResourceManager.GetObject("Connected", resourceCulture);

        internal static Icon Connecting => (Icon)ResourceManager.GetObject("Connecting", resourceCulture);

        internal static Icon Disconnected => (Icon)ResourceManager.GetObject("Disconnected", resourceCulture);

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources()
        {
        }
    }
}