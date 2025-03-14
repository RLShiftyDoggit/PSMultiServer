using System;

using MultiServer.Addons.Org.BouncyCastle.Security;

namespace MultiServer.Addons.Org.BouncyCastle.Crypto.Paddings
{
    /// <summary>A padder that adds PKCS7/PKCS5 padding to a block.</summary>
    public class Pkcs7Padding
        : IBlockCipherPadding
    {
        public void Init(SecureRandom random)
        {
            // nothing to do.
        }

        public string PaddingName => "PKCS7";

        public int AddPadding(byte[] input, int inOff)
        {
            int count = input.Length - inOff;
            byte padValue = (byte)count;

            while (inOff < input.Length)
            {
                input[inOff++] = padValue;
            }

            return count;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public int AddPadding(Span<byte> block, int position)
        {
            int count = block.Length - position;
            byte padValue = (byte)count;
            block[position..].Fill(padValue);
            return count;
        }
#endif

        public int PadCount(byte[] input)
        {
            byte padValue = input[input.Length - 1];
            int count = padValue;
            int position = input.Length - count;

            int failed = (position | (count - 1)) >> 31;
            for (int i = 0; i < input.Length; ++i)
            {
                failed |= (input[i] ^ padValue) & ~((i - position) >> 31);
            }
            if (failed != 0)
                throw new InvalidCipherTextException("pad block corrupted");

            return count;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public int PadCount(ReadOnlySpan<byte> block)
        {
            byte padValue = block[block.Length - 1];
            int count = padValue;
            int position = block.Length - count;

            int failed = (position | (count - 1)) >> 31;
            for (int i = 0; i < block.Length; ++i)
            {
                failed |= (block[i] ^ padValue) & ~((i - position) >> 31);
            }
            if (failed != 0)
                throw new InvalidCipherTextException("pad block corrupted");

            return count;
        }
#endif
    }
}
