using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Codecs.Interop.Theora
{
    // The currently defined pixel format tags.
    public enum th_pixel_fmt : int
    {
        //Chroma decimation by 2 in both the X and Y directions (4:2:0).
        //The Cb and Cr chroma planes are half the width and half the
        //height of the luma plane.
        TH_PF_420 = 0,
        // Currently reserved.
        TH_PF_RSVD,
        /**Chroma decimation by 2 in the X direction (4:2:2).
           The Cb and Cr chroma planes are half the width of the luma plane, but full
            height.*/
        TH_PF_422,
        /**No chroma decimation (4:4:4).
           The Cb and Cr chroma planes are full width and full height.*/
        TH_PF_444,
        /**The total number of currently defined pixel formats.*/
        TH_PF_NFORMATS
    }

    public enum PixelFormat 
    {
        //Chroma decimation by 2 in both the X and Y directions (4:2:0).
        //The Cb and Cr chroma planes are half the width and half the
        //height of the luma plane.
        TH_PF_420 = th_pixel_fmt.TH_PF_420,
        // Currently reserved.
        TH_PF_RSVD = th_pixel_fmt.TH_PF_RSVD,
        /**Chroma decimation by 2 in the X direction (4:2:2).
           The Cb and Cr chroma planes are half the width of the luma plane, but full
            height.*/
        TH_PF_422 = th_pixel_fmt.TH_PF_422,
        /**No chroma decimation (4:4:4).
           The Cb and Cr chroma planes are full width and full height.*/
        TH_PF_444 = th_pixel_fmt.TH_PF_444,
        /**The total number of currently defined pixel formats.*/
        TH_PF_NFORMATS = th_pixel_fmt.TH_PF_NFORMATS
    }
}
