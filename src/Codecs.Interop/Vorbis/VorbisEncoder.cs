using System;
using System.Runtime.InteropServices;
using Codecs.Interop.Ogg;
using System.IO;
using Flettu.Util;

namespace Codecs.Interop.Vorbis
{
    public class VorbisEncoder : OggEncoder
    {
        #region Vorbis Encoding

        [DllImport(Vorbis.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern int vorbis_encode_init_vbr([In, Out] ref vorbis_info vi,
               int channels,
               int rate,
               float base_quality /* 0. to 1. */
               );


        [DllImport(Vorbis.DllFile, CallingConvention = CallingConvention.Cdecl)]
        private static extern int vorbis_encode_init([In, Out] ref vorbis_info vi,
                              int channels,
                              int rate,
                              int max_bitrate,
                              int nominal_bitrate,
                              int min_bitrate);

        #endregion

        private const int SAMPLE_BUFFER_SIZE = 1024;

        private VorbisInfo info;
        private VorbisComment comment;

        private VorbisDspState dspState;
        private VorbisBlock block;

        public int Bits { get; private set; }

        public int Channels { get { return this.info.Channels; } }

        public int Rate { get { return this.info.Rate; } }

        public VorbisEncoder(Stream outputStream)
            : base(outputStream)
        {
            this.info = new VorbisInfo();
            this.comment = new VorbisComment();
            this.dspState = new VorbisDspState(this.info);
            this.block = new VorbisBlock();
        }

        public VorbisEncoder(int channels, int bits, int rate, float baseQuality, Stream outputStream)
            : this(outputStream)
        {
            int result;

            VorbisInfo.Init(this.info);

            result = vorbis_encode_init_vbr(ref this.info.vorbis_info, channels, rate, baseQuality);

            OggStreamState.Init(this.OggStream, this.SerialNo);

            VorbisComment.Init(this.comment);
            try
            {
                OggPacket header = new OggPacket();
                OggPacket headerComm = new OggPacket();
                OggPacket headerCoder = new OggPacket();

                result = VorbisDspState.AnalysisInit(this.dspState, this.info);
                result = VorbisBlock.Init(this.block, this.dspState);

                if ((result = VorbisDspState.HeaderOut(this.dspState, this.comment, header, headerComm, headerCoder)) == 0)
                    this.NoHeaderPackets = 3;

                this.Packetin(header);
                this.Packetin(headerComm);
                this.Packetin(headerCoder);
            }
            finally
            {
                VorbisComment.Clear(this.comment);
            }
        }

        public void Encode(Stream inputStream)
        {
            Guard.CheckNull(inputStream, "Encode(inputStream)");

            int result;
            int readSize = -1;
            int sampleSize = this.Bits / 8 * this.Channels;
            bool endOfStream = false;

            OggPacket oggPacket;
            OggPage oggPage;

            byte[] inputBuffer = new byte[SAMPLE_BUFFER_SIZE * sampleSize];

            while (!endOfStream)
            {
                readSize = inputStream.Read(inputBuffer, 0, SAMPLE_BUFFER_SIZE * sampleSize);

                if (readSize == 0)
                {
                    /* end of file.  this can be done implicitly in the mainline,
                    but it's easier to see here in non-clever fashion.
                    Tell the library we're at end of stream so that it can handle
                    the last frame and mark end of stream in the output properly */

                    Vorbis.AnalysisWrote(this.dspState, 0);
                }
                else
                {
                    int noSamples = readSize / sampleSize;
                    IntPtr pbuffer = Vorbis.AnalysisBuffer(this.dspState, noSamples);

                    float[][] buffer = new float[this.Channels][];
                    for (int channel = 0; channel < this.Channels; channel++)
                        buffer[channel] = new float[noSamples];

                    switch (this.Bits)
                    {
                        case 8:
                            for (int frame = 0; frame < noSamples; ++frame)
                                for (int channel = 0; channel < this.Channels; ++channel)
                                    buffer[channel][frame] = inputBuffer[frame * sampleSize + channel] / 128F;
                            break;

                        case 16:
                            int sampleIndex;


                            for (int frame = 0; frame < noSamples; ++frame)
                                for (int channel = 0; channel < this.Channels; ++channel)
                                {
                                    sampleIndex = channel * this.Bits / 8;
                                    buffer[channel][frame] = ((inputBuffer[frame * sampleSize + sampleIndex + 1] << 8) |
                                                              (0x00ff & (int)inputBuffer[frame * sampleSize + sampleIndex])) / 32768F;
                                }
                            break;
                        default:
                            throw new NotSupportedException(string.Format("Bit sample size: {0} not supported", this.Bits));
                    }

                    int[] pChannelBuffers = new int[this.Channels];
                    Marshal.Copy(pbuffer, pChannelBuffers, 0, this.Channels);
                    for (int channel = 0; channel < this.Channels; channel++)
                    {
                        IntPtr pChannelBuffer = (IntPtr)pChannelBuffers[channel];
                        Marshal.Copy(buffer[channel], 0, pChannelBuffer, noSamples);
                    }

                    result = Vorbis.AnalysisWrote(this.dspState, (int)noSamples);
                }

                /* vorbis does some data preanalysis, then divvies up blocks for
                   more involved (potentially parallel) processing.  Get a single
                   block for encoding now */

                while ((result = Vorbis.AnalysisBlockout(this.dspState, this.block)) > 0)
                {
                    // analysis, assume we want to use bitrate management 
                    Vorbis.Analysis(this.block, null);
                    VorbisBitrate.Addblock(this.block);

                    while (VorbisBitrate.Flushpacket(this.dspState, oggPacket = new OggPacket()) > 0)
                    {
                        // weld the packet into the bitstream 
                        this.Packetin(oggPacket);

                        while (!endOfStream && (result = this.Pageout(oggPage = new OggPage())) > 0)
                        {
                            endOfStream |= OggPage.Eos(oggPage) > 0;
                        }
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            // VorbisInfo should be cleared last.
            base.Dispose(disposing);

            if (disposing)
            {
                if (this.block != null)
                {
                    this.block.Dispose();
                    this.block = null;
                }

                if (this.dspState != null)
                {
                    this.dspState.Dispose();
                    this.dspState = null;
                }

                if (this.comment != null)
                {
                    this.comment.Dispose();
                    this.comment = null;
                }

                if (this.info != null)
                {
                    this.info.Dispose();
                    this.info = null;
                }
            }
        }
    }
}
