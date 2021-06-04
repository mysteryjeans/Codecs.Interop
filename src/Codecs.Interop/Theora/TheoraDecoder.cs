using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Codecs.Interop.Ogg;

namespace Codecs.Interop.Theora
{
    public class TheoraDecoder : IDisposable
    {
        #region Theora Decoding

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern int th_decode_headerin([In] ref th_info _info, [In, Out] ref th_comment _tc, [Out] out IntPtr _setup, [In] ref ogg_packet _op);

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr th_decode_alloc([In] ref th_info _info, IntPtr _setup);

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern void th_setup_free(IntPtr _setup);

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern int th_decode_ctl(IntPtr _dec, int _req, IntPtr _buf, UIntPtr _buf_sz);

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern int th_decode_packetin(IntPtr _dec, [In] ref ogg_packet _op, [Out] out Int64 _granpos);

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern int th_decode_ycbcr_out(IntPtr _dec,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)][Out]
            th_img_plane[] _ycbcr);

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern void th_decode_free(IntPtr _dec);

        #endregion

        private IntPtr th_dec_ctx;

        ~TheoraDecoder()
        {
            this.Dispose(false);
        }

        public static TheoraDecoder Create(int width, int height, OggPacket[] headers)
        {
            var info = new TheoraInfo();
            var comment = new TheoraComment();
            var decoder = new TheoraDecoder();
            
            TheoraInfo.Init(info);
            try
            {
                info.th_info.frame_width = (uint)width;
                info.th_info.frame_height = (uint)height;
                info.th_info.pixel_fmt = th_pixel_fmt.TH_PF_420;
            }
            finally
            {
                TheoraInfo.Clear(info);
            }

            int result;
            IntPtr setup = IntPtr.Zero;

            TheoraComment.Init(comment);
            try
            {
                foreach (var header in headers)
                { 
                    if ((result = th_decode_headerin(ref info.th_info, ref comment.th_comment, out setup, ref header.ogg_packet)) != 0)
                        throw new Exception(string.Format("ErrorCode: {0}, Unable to initialize Theora decoder", result));
                }

                decoder.th_dec_ctx = th_decode_alloc(ref info.th_info, setup);
                if (decoder.th_dec_ctx == IntPtr.Zero)
                    throw new Exception("Failed to initialize decoder");
            }
            finally
            {
                TheoraComment.Clear(comment);

                if (setup != IntPtr.Zero)
                    th_setup_free(setup);
            }

            return decoder;
        }

        public void Decode(OggPacket encodedData, YCbCrImage decodedImage)
        {
            int pixels = decodedImage.Width * decodedImage.Height;
            int cbcrWidth = decodedImage.Width / 2;
            int cbcrHeight = decodedImage.Height / 2;
            int cbcrPixels = cbcrWidth * cbcrHeight;

            Int64 granpos;

            if (th_decode_packetin(this.th_dec_ctx, ref encodedData.ogg_packet, out granpos) != 0)
                return;

            th_img_plane[] output = new th_img_plane[3];
            //output[0].data = Marshal.AllocHGlobal(pixels);
            //output[1].data = Marshal.AllocHGlobal(cbcrPixels);
            //output[2].data = Marshal.AllocHGlobal(cbcrPixels);

            if (th_decode_ycbcr_out(this.th_dec_ctx, output) != 0)
                return; // todo

            decodedImage.Y = output[0];
            decodedImage.Cb = output[1];
            decodedImage.Cr = output[2];
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.th_dec_ctx != IntPtr.Zero)
            {
                th_decode_free(this.th_dec_ctx);
                this.th_dec_ctx = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
