using System;
using Codecs.Interop.Ogg;
using System.IO;
using NVorbisCLI = NVorbis;
using System.Runtime.InteropServices;
using Flettu.Util;

namespace Codecs.Interop.NVorbis
{
    public class NVorbisEncoder : OggEncoder
    {
        private NVorbisCLI.Encoder encoder;

        public int Rate { get; private set; }

        public NVorbisEncoder(int channels, int bits, int rate, float baseQuality, Stream outputStream)
            : base(outputStream)
        {
            this.Rate = rate;
            this.NoHeaderPackets = 3;

            this.encoder = new NVorbisCLI.Encoder(this.SerialNo, channels, bits, rate, baseQuality, outputStream);
        }

        public void Encode(byte[] inputBuffer, int readSize)
        {
            Guard.CheckNull(inputBuffer, "Encode(inputBuffer)");

            lock (this.OutputStream)
            {
                this.encoder.Encode(inputBuffer, readSize);
            }
        }

        public void EndOfStream()
        {
            lock (this.OutputStream)
            {
                this.encoder.EndOfStream();
            }
        }

        public override int Pageout(OggPage oggPage = null)
        {
            if (oggPage != null)
            {
                int result;
                IntPtr og = Marshal.AllocHGlobal(Marshal.SizeOf(oggPage.ogg_page));
                try
                {
                    Marshal.StructureToPtr(oggPage.ogg_page, og, true);

                    lock (this.OutputStream)
                    {
                        result = this.encoder.Pageout(og);
                    }

                    Marshal.PtrToStructure(og, oggPage.ogg_page);

                    return result;
                }
                finally
                {
                    Marshal.FreeHGlobal(og);
                }
            }

            lock (this.OutputStream)
            {
                return this.encoder.Pageout();
            }
        }

        public override int Flush(OggPage oggPage = null)
        {
            if (oggPage != null)
            {
                int result;
                IntPtr og = Marshal.AllocHGlobal(Marshal.SizeOf(oggPage.ogg_page));
                try
                {
                    Marshal.StructureToPtr(oggPage.ogg_page, og, true);

                    lock (this.OutputStream)
                    {
                        result = this.encoder.Flush(og);
                    }

                    Marshal.PtrToStructure(og, oggPage.ogg_page);

                    return result;
                }
                finally
                {
                    Marshal.FreeHGlobal(og);
                }
            }

            lock (this.OutputStream)
            {
                return this.encoder.Flush();
            }
        }

        protected override void Write(OggPage oggPage)
        {
            Guard.CheckNull(oggPage, "Write(oggPage)");

            IntPtr og = Marshal.AllocHGlobal(Marshal.SizeOf(oggPage.ogg_page));
            try
            {
                Marshal.StructureToPtr(oggPage.ogg_page, og, true);
                lock (this.OutputStream)
                {
                    this.encoder.Write(og);
                }
                Marshal.PtrToStructure(og, oggPage.ogg_page);
            }
            finally
            {
                Marshal.FreeHGlobal(og);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.encoder != null)
                {
                    this.encoder.Dispose();
                    this.encoder = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
