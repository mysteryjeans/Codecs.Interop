using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Codecs.Interop.Skeleton;
using Codecs.Interop.Ogg;

namespace Codecs.Interop.Ogg
{
    public class OggMultiplexer
    {
        public OggMultiplexer(OggEncoder[] encoders)
        {
            foreach (OggEncoder encoder in encoders)
            {
                encoder.DataIn += new DataInEventHandler(PacketIn);
            }
        }


        static void PacketIn(object sender, OggPacket e)
        {
            OggEncoder encoder = (OggEncoder)sender;
            if ((encoder.OggStream.ogg_stream_state.packetno % 4) == 0)
            {
                encoder.Flush();
            }
        }
    }
}
