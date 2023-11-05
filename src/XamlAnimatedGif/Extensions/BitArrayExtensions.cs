using System.Collections;

namespace XamlAnimatedGif.Extensions
{
    static class BitArrayExtensions
    {
        public static short ToInt16(this BitArray bitArray)
        {
            short n = 0;
            for (int i = bitArray.Length - 1; i >= 0; i--)
            {
                n = (short) ((n << 1) + (bitArray[i] ? 1 : 0));
            }
            return n;
        }
    }
}
