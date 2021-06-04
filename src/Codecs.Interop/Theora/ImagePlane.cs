using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Codecs.Interop.Theora
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct th_img_plane
    {
        /**The width of this plane.*/
        internal int width;
        /**The height of this plane.*/
        internal int height;
        /**The offset in bytes between successive rows.*/
        internal int stride;
        /**A pointer to the beginning of the first row.*/
        internal IntPtr data;  // unsigned char *
    }
}
