using System.Linq;
using System.Text;
using System.IO;
using Flettu.Extensions;
using Codecs.Interop.Ogg;

namespace Codecs.Interop.Skeleton
{
    public class SkeletonEncoder : OggEncoder
    {
        private const int FISHEAD_HEADER_SIZE = 54;
        private const int FISBONE_HEADER_SIZE = 52;

        private static readonly byte[] FISHEAD = Encoding.UTF8.GetBytes("fishead\0");
        private static readonly byte[] FISBONE = Encoding.UTF8.GetBytes("fisbone\0");

        public SkeletonEncoder(Stream outputStream)
            : base(outputStream)
        {
            byte[] buffer = new byte[FISHEAD_HEADER_SIZE];

            // First header identifier
            SkeletonEncoder.FISHEAD.CopyTo(buffer, 0);

            // Ogg Skeleton version 3.0
            ((short)3).CopyToBuffer(buffer, 8);
            ((short)0).CopyToBuffer(buffer, 10);
            
            // Persentation
            0L.CopyToBuffer(buffer, 12);
            1000L.CopyToBuffer(buffer, 20);

            // Basetime
            0L.CopyToBuffer(buffer, 28);
            1000L.CopyToBuffer(buffer, 36);

            // UTC - #No-Ndeed
            //byte[] utc = new byte[20];
            //utc.CopyTo(buffer, 44);
            
            OggStreamState.Init(this.OggStream, this.SerialNo);
            var oggPacket = new OggPacket
            {
                B_o_s = 256,
                E_o_s = 0,
                Granulepos = 0,
                Packetno = this.GetPacketNo(),
                Bytes = buffer.Length,
                Packet = buffer
            };

            this.Packetin(oggPacket);
        }

        public void AddFishboneHeader(int serialNo, int granulePos, int headerPackets, long granuleNumerator, long granulerateDenominator, int preroll, byte granuleshift, string[] messageHeaders)
        {
            byte[] msgHeaders = Encoding.UTF8.GetBytes(string.Join("\r\n", messageHeaders) + "\r\n");
            byte[] buffer = new byte[FISBONE_HEADER_SIZE + msgHeaders.Length];


            // Sub-headers identifiers
            SkeletonEncoder.FISBONE.CopyTo(buffer, 0);

            // Message Headers Offset
            44.CopyToBuffer(buffer, 8);
            
            // Ogg bitstream serialno
            serialNo.CopyToBuffer(buffer, 12);

            // Ogg bitstream headers packets
            headerPackets.CopyToBuffer(buffer, 16);

            // Granule
            granuleNumerator.CopyToBuffer(buffer, 20);
            granulerateDenominator.CopyToBuffer(buffer, 28);

            // Base Granule
            // 0L.CopyToBuffer(buffer, 36);

            // Preroll
            preroll.CopyToBuffer(buffer, 44);

            // Granuleshift
            buffer[48] = granuleshift;

            // Padding #No-Need
            //byte[] padding = new byte[3];
            //padding.CopyTo(buffer, 49);

            msgHeaders.CopyTo(buffer, 52);

            var oggPacket = new OggPacket
            {
                B_o_s = 0,
                E_o_s = 0,
                Granulepos = granulePos,
                Packetno = this.GetPacketNo(),
                Bytes = buffer.Length,
                Packet = buffer.ToArray()
            };

            this.Packetin(oggPacket);
            this.Pageout();
        }

        public void EndOfStream()
        {
            OggPacket oggPacket = new OggPacket
            {
                B_o_s = 0,
                E_o_s = 512,
                Granulepos = 0,
                Bytes = 0,
                Packetno = this.GetPacketNo(),
                Packet = new byte[0]
            };

            this.Packetin(oggPacket);
            this.Flush();
        }
    }
}
