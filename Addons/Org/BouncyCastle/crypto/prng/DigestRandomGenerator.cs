using System;

using MultiServer.Addons.Org.BouncyCastle.Crypto.Utilities;
using MultiServer.Addons.Org.BouncyCastle.Utilities;

namespace MultiServer.Addons.Org.BouncyCastle.Crypto.Prng
{
	/**
	 * Random generation based on the digest with counter. Calling AddSeedMaterial will
	 * always increase the entropy of the hash.
	 * <p>
	 * Internal access to the digest is synchronized so a single one of these can be shared.
	 * </p>
	 */
	public sealed class DigestRandomGenerator
		: IRandomGenerator
	{
		private const long CYCLE_COUNT = 10;

		private long	stateCounter;
		private long	seedCounter;
		private IDigest	digest;
		private byte[]	state;
		private byte[]	seed;

		public DigestRandomGenerator(IDigest digest)
		{
			this.digest = digest;

			this.seed = new byte[digest.GetDigestSize()];
			this.seedCounter = 1;

			this.state = new byte[digest.GetDigestSize()];
			this.stateCounter = 1;
		}

		public void AddSeedMaterial(byte[] inSeed)
		{
			lock (this)
			{
                if (!Arrays.IsNullOrEmpty(inSeed))
                {
                    DigestUpdate(inSeed);
                }
                DigestUpdate(seed);
				DigestDoFinal(seed);
			}
		}

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public void AddSeedMaterial(ReadOnlySpan<byte> inSeed)
        {
            lock (this)
            {
                if (!inSeed.IsEmpty)
                {
                    DigestUpdate(inSeed);
                }
                DigestUpdate(seed);
                DigestDoFinal(seed);
            }
        }
#endif

        public void AddSeedMaterial(long rSeed)
		{
			lock (this)
			{
				DigestAddCounter(rSeed);
				DigestUpdate(seed);
				DigestDoFinal(seed);
			}
		}

		public void NextBytes(byte[] bytes)
		{
			NextBytes(bytes, 0, bytes.Length);
		}

		public void NextBytes(byte[] bytes, int start, int len)
		{
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
			NextBytes(bytes.AsSpan(start, len));
#else
			lock (this)
			{
				int stateOff = 0;

				GenerateState();

				int end = start + len;
				for (int i = start; i < end; ++i)
				{
					if (stateOff == state.Length)
					{
						GenerateState();
						stateOff = 0;
					}
					bytes[i] = state[stateOff++];
				}
			}
#endif
		}

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		public void NextBytes(Span<byte> bytes)
		{
			lock (this)
			{
				int stateOff = 0;

				GenerateState();

				for (int i = 0; i < bytes.Length; ++i)
				{
					if (stateOff == state.Length)
					{
						GenerateState();
						stateOff = 0;
					}
					bytes[i] = state[stateOff++];
				}
			}
		}
#endif

		private void CycleSeed()
		{
			DigestUpdate(seed);
			DigestAddCounter(seedCounter++);
			DigestDoFinal(seed);
		}

		private void GenerateState()
		{
			DigestAddCounter(stateCounter++);
			DigestUpdate(state);
			DigestUpdate(seed);
			DigestDoFinal(state);

			if ((stateCounter % CYCLE_COUNT) == 0)
			{
				CycleSeed();
			}
		}

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private void DigestAddCounter(long seedVal)
        {
            Span<byte> bytes = stackalloc byte[8];
            Pack.UInt64_To_LE((ulong)seedVal, bytes);
            digest.BlockUpdate(bytes);
        }

        private void DigestUpdate(ReadOnlySpan<byte> inSeed)
        {
            digest.BlockUpdate(inSeed);
        }

        private void DigestDoFinal(Span<byte> result)
        {
            digest.DoFinal(result);
        }
#else
        private void DigestAddCounter(long seedVal)
        {
            byte[] bytes = new byte[8];
            Pack.UInt64_To_LE((ulong)seedVal, bytes);
            digest.BlockUpdate(bytes, 0, bytes.Length);
        }

		private void DigestUpdate(byte[] inSeed)
		{
			digest.BlockUpdate(inSeed, 0, inSeed.Length);
		}

        private void DigestDoFinal(byte[] result)
		{
			digest.DoFinal(result, 0);
		}
#endif
    }
}
