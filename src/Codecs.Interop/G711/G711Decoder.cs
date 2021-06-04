using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codecs.Interop.G711
{
    public class G711Decoder
    {
        private const byte SIGN_BIT = 0x80;
        private const byte SEG_MASK = 0x70;
        private const byte QUANT_MASK = 0xf;
        private const byte SEG_SHIFT = 4;
        private const byte BIAS = 0x84;

        public static int ULawToLinear(int val)
        {
            int t;

            val = ~val;

            t = ((val & QUANT_MASK) << 3) + BIAS;
            t <<= (val & SEG_MASK) >> SEG_SHIFT;

            return (((val & SIGN_BIT) != 0) ? (BIAS - t) : (t - BIAS));
        }

        public static int ALawToLinear(int val)
        {
            int t;
            int seg;

            val ^= 0x55;

            t = (val & QUANT_MASK) << 4;
            seg = (int)((uint)val & SEG_MASK) >> SEG_SHIFT;
            switch (seg)
            {
                case 0:
                    t += 8;
                    break;
                case 1:
                    t += 0x108;
                    break;
                default:
                    t += 0x108;
                    t <<= seg - 1;
                    break;
            }
            return (((val & SIGN_BIT) != 0) ? t : -t);
        }
    }
}
