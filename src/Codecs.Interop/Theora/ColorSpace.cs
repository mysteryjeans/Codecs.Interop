using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Codecs.Interop.Theora
{
    // The currently defined color space tags.
    public enum th_colorspace : int
    {
        /**The color space was not specified at the encoder.
        It may be conveyed by an external means.*/
        TH_CS_UNSPECIFIED = 0,
        /**A color space designed for NTSC content.*/
        TH_CS_ITU_REC_470M,
        /**A color space designed for PAL/SECAM content.*/
        TH_CS_ITU_REC_470BG,
        /**The total number of currently defined color spaces.*/
        TH_CS_NSPACES
    }
}
