using System;
using System.IO;
using Flettu.Util;

namespace Codecs.Interop.Ogg
{
    internal delegate void DataInEventHandler(object sender, OggPacket e);

    public abstract class OggEncoder : IDisposable
    {
        internal event DataInEventHandler DataIn = null;

        private static readonly Random RandomSeriaNo = new Random();

        private int packetNo = 0;

        protected Stream OutputStream { get; private set; }

        public OggStreamState OggStream { get; private set; }

        public int NoHeaderPackets { get; protected set; }

        public int SerialNo { get; private set; }

        public OggEncoder(Stream outputStream)
        {
            Guard.CheckNull(outputStream, "OggEncoder(outputStream)");

            this.SerialNo = OggEncoder.RandomSeriaNo.Next();
            this.OggStream = new OggStreamState();
            this.OutputStream = outputStream;
        }

        ~OggEncoder()
        {
            this.Dispose(false);
        }

        public int Packetin(OggPacket oggPacket)
        {
            int result;
            result = OggStreamState.Packetin(this.OggStream, oggPacket);
            if (this.DataIn != null)
            {
                this.DataIn(this, oggPacket);
            }
            return result;
        }

        public virtual int Pageout(OggPage oggPage = null)
        {
            int result;

            oggPage = oggPage ?? new OggPage();
            if ((result = OggStreamState.Pageout(this.OggStream, oggPage)) > 0)
                this.Write(oggPage);

            return result;
        }

        public virtual int Flush(OggPage oggPage = null)
        {
            int result;

            oggPage = oggPage ?? new OggPage();
            if ((result = OggStreamState.Flush(this.OggStream, oggPage)) > 0)
                this.Write(oggPage);

            return result;
        }

        protected virtual void Write(OggPage oggPage)
        {
            Guard.CheckNull(oggPage, "Write(oggPage)");

            // ogg_page.header is fixed length array and not marshaled properly, so workaround
            // is using the fact that pageout/flush sets ogg_page.header to points to ogg_stream_state.header
            // after setting header data
            var header = this.OggStream.Header;
            var body = oggPage.Body;

            lock (this.OutputStream)
            {
                this.OutputStream.Write(header, 0, header.Length);
                this.OutputStream.Write(body, 0, body.Length);
            }
        }

        protected int GetPacketNo()
        {
            return this.packetNo++;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.OggStream != null)
                {
                    this.OggStream.Dispose();
                    this.OggStream = null;
                }
            }
        }

        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
