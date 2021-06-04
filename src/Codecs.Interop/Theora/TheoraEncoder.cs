using System;
using System.Runtime.InteropServices;
using Codecs.Interop.Ogg;
using System.IO;
using Flettu.Util;

namespace Codecs.Interop.Theora
{
    public class TheoraEncoder : OggEncoder
    {
        #region Theora Encoding

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr th_encode_alloc([In] ref th_info _info);

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern void th_encode_free(IntPtr _enc);

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern int th_encode_ctl(IntPtr _enc, int _req, [In, Out] ref int _buf, int _buf_sz);

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern int th_encode_flushheader(IntPtr _enc, [In] ref th_comment _comments, [In, Out] ref ogg_packet _op);

        public static int FlushHeader(TheoraEncoder encoder, TheoraComment comment, OggPacket oggPacket)
        {
            var retVal = th_encode_flushheader(encoder.th_enc_ctx, ref comment.th_comment, ref oggPacket.ogg_packet);
            comment.changed = true;
            oggPacket.changed = true;

            return retVal;
        }

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern int th_encode_ycbcr_in(IntPtr _enc,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]
            th_img_plane[] _ycbcr);

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern int th_encode_ycbcr_in(IntPtr _enc,
            IntPtr _ycbcr);

        [DllImport(Theora.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern int th_encode_packetout(IntPtr _enc, int _last, [In, Out] ref ogg_packet _op);

        public static int Packetout(TheoraEncoder encoder, int last, OggPacket oggPacket)
        {
            var retVal = th_encode_packetout(encoder.th_enc_ctx, last, ref oggPacket.ogg_packet);
            oggPacket.changed = true;

            return retVal;
        }

        #endregion

        private IntPtr th_enc_ctx;
        private TheoraInfo info;
        private TheoraComment comment;

        public int FrameRate { get { return (int)this.info.th_info.fps_numerator; } }

        public int KeyFrameFrequency { get; private set; }

        public int Granuleshift { get { return this.info.th_info.keyframe_granule_shift; } }

        private TheoraEncoder(Stream outputStream)
            : base(outputStream)
        {
            this.info = new TheoraInfo();
            this.comment = new TheoraComment();
        }

        public TheoraEncoder(int width, int height, int frameRate, int bitRate, int keyFrameFrequency, int quality, PixelFormat pixelFormat, Stream outputStream, TheoraSpeedLevel speedLevel = TheoraSpeedLevel.Default)
            : this(outputStream)
        {
            int result;
            OggPacket header;
            
            TheoraInfo.Init(this.info);
            try
            {
                this.info.th_info.frame_width = (uint)(((width + 15) >> 4) << 4);
                this.info.th_info.frame_height = (uint)(((height + 15) >> 4) << 4);
                this.info.th_info.pic_width = (uint)width;
                this.info.th_info.pic_height = (uint)height;
                this.info.th_info.pic_x = 0;
                this.info.th_info.pic_y = 0;
                this.info.th_info.colorspace = th_colorspace.TH_CS_UNSPECIFIED;
                this.info.th_info.pixel_fmt = (th_pixel_fmt)pixelFormat;
                this.info.th_info.target_bitrate = 0; // 0 == VBR; b/s
                this.info.th_info.quality = quality;
                this.info.th_info.fps_numerator = (uint)frameRate;
                this.info.th_info.fps_denominator = 1;
                this.info.th_info.aspect_numerator = 1;
                this.info.th_info.aspect_denominator = 1;
                this.info.th_info.keyframe_granule_shift = keyFrameFrequency >> 1;

                this.th_enc_ctx = th_encode_alloc(ref this.info.th_info);
            }
            finally
            {
                TheoraInfo.Clear(this.info);
            }

            //result = th_encode_ctl(this.th_enc_ctx, Theora.TH_ENCCTL_SET_KEYFRAME_FREQUENCY_FORCE, ref ukeyFrame, sizeof(uint));
            //if (result != 0)
            //    throw new InvalidOperationException(string.Format("ErrorCode: {0} Failed to set Key Frame: {1}", result, ukeyFrame));
            this.KeyFrameFrequency = (int)keyFrameFrequency;

            result = th_encode_ctl(this.th_enc_ctx, Theora.TH_ENCCTL_SET_BITRATE, ref bitRate, sizeof(int));
            if (result != 0)
                throw new InvalidOperationException(string.Format("ErrorCode: {0} Failed to set Bitrate: {1}", result, bitRate));

            int spLevel = 0;
            result = th_encode_ctl(this.th_enc_ctx, Theora.TH_ENCCTL_GET_SPLEVEL_MAX, ref spLevel, sizeof(int));
            if (result != 0)
                throw new InvalidOperationException(string.Format("ErrorCode: {0} Failed to get max speed level", result));

            switch (speedLevel)
            {
                case TheoraSpeedLevel.Slow:
                    spLevel = 0;
                    goto case TheoraSpeedLevel.Max;
                case TheoraSpeedLevel.Medium:
                    spLevel >>= 1;
                    goto case TheoraSpeedLevel.Max;
                case TheoraSpeedLevel.Max:
                    result = th_encode_ctl(this.th_enc_ctx, Theora.TH_ENCCTL_SET_SPLEVEL, ref spLevel, sizeof(int));
                    if (result != 0)
                        throw new InvalidOperationException(string.Format("ErrorCode: {0} Failed to set speed level: {1}", result, speedLevel));
                    break;
                default:
                case TheoraSpeedLevel.Default:
                    break;
            }

            TheoraComment.Init(this.comment);
            try
            {
                OggStreamState.Init(this.OggStream, this.SerialNo);

                while ((result = TheoraEncoder.FlushHeader(this, this.comment, header = new OggPacket())) > 0)
                {
                    this.NoHeaderPackets++;

                    // weld the packet into the bitstream
                    this.Packetin(header);
                }
            }
            finally
            {
                TheoraComment.Clear(this.comment);
            }
        }

        public int Encode(YCbCrImage image, bool isLast, int duplicates = 0)
        {
            int result;
            OggPacket oggPacket;

            Guard.CheckNull(image, "Encode(image)");
            
            result = th_encode_ctl(this.th_enc_ctx, Theora.TH_ENCCTL_SET_DUP_COUNT, ref duplicates, sizeof(int));
            if (result != 0)
                throw new InvalidOperationException(string.Format("ErrorCode: {0} Failed to set number of frames to duplicate: {1}", result, duplicates));

            var inData = new th_img_plane[] { image.Y, image.Cb, image.Cr };
            var handle = GCHandle.Alloc(inData, GCHandleType.Pinned);
            try
            { result = th_encode_ycbcr_in(this.th_enc_ctx, handle.AddrOfPinnedObject()); }
            finally { handle.Free(); }

            if (result != 0)
                throw new InvalidOperationException(string.Format("ErrorCode: {0} Failed to encode image", result));

            while (TheoraEncoder.Packetout(this, isLast ? 1 : 0, oggPacket = new OggPacket()) > 0)
            {
                this.Packetin(oggPacket);

                while (this.Pageout() > 0) result++;
            }

            return result;
        }

        protected override void Dispose(bool disposing)
        {
            if (this.th_enc_ctx != IntPtr.Zero)
            {
                th_encode_free(this.th_enc_ctx);
                this.th_enc_ctx = IntPtr.Zero;
            }

            base.Dispose(disposing);
        }

        private static int iLog(int value)
        {
            int ret = 0;
            for (ret = 0; value > 0; ret++) value >>= 1;
            return ret;
        }
    }
}
