using System;

using MultiServer.Addons.Org.BouncyCastle.Crypto.Parameters;
using MultiServer.Addons.Org.BouncyCastle.Utilities;

namespace MultiServer.Addons.Org.BouncyCastle.Crypto.Macs
{
    /**
    * HMAC implementation based on RFC2104
    *
    * H(K XOR opad, H(K XOR ipad, text))
    */
    public class HMac
		: IMac
    {
        private const byte IPAD = (byte)0x36;
        private const byte OPAD = (byte)0x5C;

        private readonly IDigest digest;
        private readonly int digestSize;
        private readonly int blockLength;
		private IMemoable ipadState;
		private IMemoable opadState;

		private readonly byte[] inputPad;
        private readonly byte[] outputBuf;

        public HMac(IDigest digest)
            : this(digest, digest.GetByteLength())
        {
        }

        public HMac(IDigest digest, int blockLength)
        {
            if (blockLength < 16)
                throw new ArgumentException("must be at least 16 bytes", nameof(blockLength));

            this.digest = digest;
            this.digestSize = digest.GetDigestSize();
            this.blockLength = blockLength;
            this.inputPad = new byte[blockLength];
            this.outputBuf = new byte[blockLength + digestSize];
        }

        public virtual string AlgorithmName
        {
            get { return digest.AlgorithmName + "/HMAC"; }
        }

		public virtual IDigest GetUnderlyingDigest()
        {
            return digest;
        }

        public virtual void Init(ICipherParameters parameters)
        {
            digest.Reset();

            KeyParameter keyParameter = (KeyParameter)parameters;

            int keyLength = keyParameter.KeyLength;
            if (keyLength > blockLength)
            {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                digest.BlockUpdate(keyParameter.Key);
#else
                byte[] key = keyParameter.GetKey();
                digest.BlockUpdate(key, 0, keyLength);
#endif

                digest.DoFinal(inputPad, 0);

				keyLength = digestSize;
            }
            else
            {
                keyParameter.CopyTo(inputPad, 0, keyLength);
            }

			Array.Clear(inputPad, keyLength, blockLength - keyLength);
            Array.Copy(inputPad, 0, outputBuf, 0, blockLength);

			XorPad(inputPad, blockLength, IPAD);
            XorPad(outputBuf, blockLength, OPAD);

			if (digest is IMemoable memoable)
			{
				opadState = memoable.Copy();

				((IDigest)opadState).BlockUpdate(outputBuf, 0, blockLength);

			    digest.BlockUpdate(inputPad, 0, inputPad.Length);

				ipadState = memoable.Copy();
            }
            else
            {
                digest.BlockUpdate(inputPad, 0, inputPad.Length);
            }
        }

        public virtual int GetMacSize()
        {
            return digestSize;
        }

        public virtual void Update(byte input)
        {
            digest.Update(input);
        }

        public virtual void BlockUpdate(byte[] input, int inOff, int len)
        {
            digest.BlockUpdate(input, inOff, len);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public virtual void BlockUpdate(ReadOnlySpan<byte> input)
        {
            digest.BlockUpdate(input);
        }
#endif

        public virtual int DoFinal(byte[] output, int outOff)
        {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            return DoFinal(output.AsSpan(outOff));
#else
            digest.DoFinal(outputBuf, blockLength);

			if (opadState != null)
			{
				((IMemoable)digest).Reset(opadState);
				digest.BlockUpdate(outputBuf, blockLength, digestSize);
			}
			else
			{
				digest.BlockUpdate(outputBuf, 0, outputBuf.Length);
			}

			int len = digest.DoFinal(output, outOff);

			Array.Clear(outputBuf, blockLength, digestSize);

			if (ipadState != null)
			{
				((IMemoable)digest).Reset(ipadState);
			}
			else
			{
				digest.BlockUpdate(inputPad, 0, inputPad.Length);
			}

            return len;
#endif
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public virtual int DoFinal(Span<byte> output)
        {
            digest.DoFinal(outputBuf.AsSpan(blockLength));

            if (opadState != null)
            {
                ((IMemoable)digest).Reset(opadState);
                digest.BlockUpdate(outputBuf.AsSpan(blockLength, digestSize));
            }
            else
            {
                digest.BlockUpdate(outputBuf);
            }

            int len = digest.DoFinal(output);

            Array.Clear(outputBuf, blockLength, digestSize);

            if (ipadState != null)
            {
                ((IMemoable)digest).Reset(ipadState);
            }
            else
            {
                digest.BlockUpdate(inputPad);
            }

            return len;
        }
#endif

        /**
        * Reset the mac generator.
        */
        public virtual void Reset()
        {
            if (ipadState != null)
            {
                ((IMemoable)digest).Reset(ipadState);
            }
            else
            {
                digest.Reset();
                digest.BlockUpdate(inputPad, 0, inputPad.Length);
            }
        }

        private static void XorPad(byte[] pad, int len, byte n)
		{
			for (int i = 0; i < len; ++i)
            {
                pad[i] ^= n;
            }
		}
    }
}
