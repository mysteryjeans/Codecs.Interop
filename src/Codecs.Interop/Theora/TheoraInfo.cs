using System;
using System.Runtime.InteropServices;


namespace Codecs.Interop.Theora
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct th_info
    {
        internal byte version_major; //  unsigned char 
        internal byte version_minor;// unsigned char 
        internal byte version_subminor; // unsigned char 
        internal uint frame_width; // typedef unsigned __int32 ogg_uint32_t;
        internal uint frame_height;
        internal uint pic_width;
        internal uint pic_height;
        internal uint pic_x;
        internal uint pic_y;
        internal uint fps_numerator;
        internal uint fps_denominator;
        internal uint aspect_numerator;
        internal uint aspect_denominator;
        /**The color space.*/
        internal th_colorspace colorspace;
        /**The pixel format.*/
        internal th_pixel_fmt pixel_fmt;
        internal int target_bitrate;
        internal int quality;
        internal int keyframe_granule_shift;
    }

    public class TheoraInfo
    {
        #region Variables
        internal bool changed = true;
		internal th_info th_info = new th_info();
	
		#endregion

        #region theora_info_init
        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern void th_info_init([In, Out] ref th_info _info);

        public static void Init(TheoraInfo info)
        {
            th_info_init(ref info.th_info);
            info.changed = true;
        }
        #endregion

        #region theora_info_clear
        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern void th_info_clear([In,Out] ref th_info _info);

        public static void Clear(TheoraInfo info)
        {
            th_info_clear(ref info.th_info);
            info.changed = true;
        }
        #endregion
    }
}
