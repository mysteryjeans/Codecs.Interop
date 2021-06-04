using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecs.Interop.G711
{
    public class G711Encoder
    {
        public static byte LinearToALaw(short number)
        {
            const short ALAW_MAX = 0xFFF;
            short mask = 0x800;
            byte sign = 0;
            byte position = 11;
            byte lsb = 0;
            if (number < 0)
            {
                number *= -1;
                sign = 0x80;
            }
            if (number > ALAW_MAX)
            {
                number = ALAW_MAX;
            }
            for (; ((number & mask) != mask && position >= 5); mask >>= 1, position--) ;

            unchecked
            {
                lsb = (byte)((number >> ((position == 4) ? (1) : (position - 4))) & 0x0f);
                return (byte)((sign | ((position - 4) << 4) | lsb) ^ 0x55);
            }
        }

        public static byte LinearToULaw(short pcm_val)
        {
            const int TABLE_SIZE = 8;
            const int BIAS = 0x84;
            const int CLIP = 8159;

            short[] seg_uend = new short[]{
                0x3F, 0x7F, 0xFF, 0x1FF,
                0x3FF, 0x7FF, 0xFFF, 0x1FFF};

            short mask;
            short seg;
            byte uval;

            if (pcm_val < 0)
            {
                pcm_val = (short)(-pcm_val);
                mask = 0x7F;
            }
            else
            {
                mask = 0xFF;
            }

            if (pcm_val > CLIP) pcm_val = CLIP;
            pcm_val += (BIAS >> 2);

            seg = TABLE_SIZE;
            for (short i = 0; i < seg_uend.Length; i++)
                if (pcm_val <= seg_uend[i])
                {
                    seg = i;
                    break;
                };

            if (seg >= 8)
                return (byte)(0x7F ^ mask);
            else
            {
                uval = (byte)((seg << 4) | ((pcm_val >> (seg + 1)) & 0xF));
                return (byte)(uval ^ mask);
            }
        }
    }
}
