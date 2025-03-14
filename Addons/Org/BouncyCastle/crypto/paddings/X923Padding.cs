using System;

using MultiServer.Addons.Org.BouncyCastle.Security;
using MultiServer.Addons.Org.BouncyCastle.Utilities;

namespace MultiServer.Addons.Org.BouncyCastle.Crypto.Paddings
{
    /// <summary>
    /// A padder that adds X9.23 padding to a block - if a SecureRandom is passed in random padding is assumed,
    /// otherwise padding with zeros is used.
    /// </summary>
    public class X923Padding
		: IBlockCipherPadding
    {
        private SecureRandom m_random = null;

        public void Init(SecureRandom random)
        {
            // NOTE: If random is null, zero padding is used
            m_random = random;
        }

        public string PaddingName => "X9.23";

        public int AddPadding(byte[] input, int inOff)
        {
            int count = input.Length - inOff;
            if (count > 1)
            {
                if (m_random == null)
                {
                    Arrays.Fill(input, inOff, input.Length - 1, 0x00);
                }
                else
                {
                    m_random.NextBytes(input, inOff, count - 1);
                }
            }
            input[input.Length - 1] = (byte)count;
            return count;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public int AddPadding(Span<byte> block, int position)
        {
            int count = block.Length - position;
            if (count > 1)
            {
                var body = block[position..(block.Length - 1)];
                if (m_random == null)
                {
                    body.Fill(0x00);
                }
                else
                {
                    m_random.NextBytes(body);
                }
            }
            block[block.Length - 1] = (byte)count;
            return count;
        }
#endif

        public int PadCount(byte[] input)
        {
            int count = input[input.Length - 1];
            int position = input.Length - count;

            int failed = (position | (count - 1)) >> 31;
            if (failed != 0)
                throw new InvalidCipherTextException("pad block corrupted");

            return count;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public int PadCount(ReadOnlySpan<byte> block)
        {
            int count = block[block.Length - 1];
            int position = block.Length - count;

            int failed = (position | (count - 1)) >> 31;
            if (failed != 0)
                throw new InvalidCipherTextException("pad block corrupted");

            return count;
        }
#endif
    }
}
