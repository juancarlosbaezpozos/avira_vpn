using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace mshtml
{
    //[ComImport]
    [CompilerGenerated]
    [ComEventInterface(typeof(HTMLDocumentEvents2), typeof(HTMLDocumentEvents2))]
    [TypeIdentifier("3050f1c5-98b5-11cf-bb82-00aa00bdce0b", "mshtml.HTMLDocumentEvents2_Event")]
    public interface HTMLDocumentEvents2_Event
    {
        event HTMLDocumentEvents2_onkeydownEventHandler onkeydown;

        event HTMLDocumentEvents2_onkeyupEventHandler onkeyup;

        event HTMLDocumentEvents2_onmousewheelEventHandler onmousewheel;

        void _VtblGap1_6();

        void _VtblGap2_52();
    }
}