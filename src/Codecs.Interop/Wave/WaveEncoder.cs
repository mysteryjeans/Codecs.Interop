using System;
using System.Text;
using System.IO;
using Flettu.Util;

namespace Codecs.Interop.Wave
{
    public class WaveEncoder : IDisposable
    {
        private const int HEADER_SIZE = 36;
        private static readonly byte[] RIFF_BYTES = Encoding.ASCII.GetBytes("RIFF");
        private static readonly byte[] WAVE_BYTES = Encoding.ASCII.GetBytes("WAVE");
        private static readonly byte[] FMT_BYTES = Encoding.ASCII.GetBytes("fmt ");
        private static readonly byte[] DATA_BYTES = Encoding.ASCII.GetBytes("data");


        private int? dataLength;
        private long startPosition;

        private Stream wavOutput;

        public WaveEncoder(int bits, short channels, int sampleRate, Stream outputStream, bool isFloatingPoint = false, int? dataLength = null)
        {
            Guard.CheckNull(outputStream, "WaveEncoder(outputStream)");

            if (dataLength == null && !outputStream.CanSeek)
                throw new ArgumentException("Please specify data length, cannot updated wave header later because output stream doesn't support seek");

            this.dataLength = dataLength;
            this.wavOutput = outputStream;
            this.startPosition = outputStream.CanSeek ? outputStream.Position : 0;

            lock (this.wavOutput)
            {
                this.wavOutput.Write(WaveEncoder.RIFF_BYTES, 0, WaveEncoder.RIFF_BYTES.Length);
                this.wavOutput.Write(BitConverter.GetBytes(this.dataLength.GetValueOrDefault() + HEADER_SIZE), 0, 4);
                this.wavOutput.Write(WaveEncoder.WAVE_BYTES, 0, WaveEncoder.WAVE_BYTES.Length);
                this.wavOutput.Write(WaveEncoder.FMT_BYTES, 0, WaveEncoder.FMT_BYTES.Length);
                this.wavOutput.Write(BitConverter.GetBytes(16), 0, 4);
                this.wavOutput.Write(BitConverter.GetBytes((ushort)(isFloatingPoint ? 3 : 1)), 0, 2);
                this.wavOutput.Write(BitConverter.GetBytes(channels), 0, 2);
                this.wavOutput.Write(BitConverter.GetBytes(sampleRate), 0, 4);
                this.wavOutput.Write(BitConverter.GetBytes(sampleRate * channels * (bits / 8)), 0, 4);
                this.wavOutput.Write(BitConverter.GetBytes((ushort)channels * (bits / 8)), 0, 2);
                this.wavOutput.Write(BitConverter.GetBytes(bits), 0, 2);
                this.wavOutput.Write(WaveEncoder.DATA_BYTES, 0, DATA_BYTES.Length);
                this.wavOutput.Write(BitConverter.GetBytes(this.dataLength.GetValueOrDefault()), 0, 4);
            }
        }

        public void Encode(byte[] pcmData)
        {
            lock (this.wavOutput)
            {
                this.wavOutput.Write(pcmData, 0, pcmData.Length);
            }
        }

        public void EndOfStream()
        {
            if (this.dataLength == null)
            {
                lock (this.wavOutput)
                {
                    var currentPosition = this.wavOutput.Position;
                    this.dataLength = (int)(this.wavOutput.Position - this.startPosition) - WaveEncoder.HEADER_SIZE;

                    // Writing total data length including header
                    this.wavOutput.Position = this.startPosition + RIFF_BYTES.Length;
                    this.wavOutput.Write(BitConverter.GetBytes(dataLength.Value + WaveEncoder.HEADER_SIZE), 0, 4);

                    // Writing total data length without header
                    this.wavOutput.Position = this.startPosition + 40;
                    this.wavOutput.Write(BitConverter.GetBytes(dataLength.Value), 0, 4);

                    this.wavOutput.Position = currentPosition;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.EndOfStream();
            }
        }

        void IDisposable.Dispose()
        {
            this.Dispose(true);
        }
    }
}
