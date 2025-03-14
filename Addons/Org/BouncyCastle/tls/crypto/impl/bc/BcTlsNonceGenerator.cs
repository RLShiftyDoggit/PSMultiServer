using System;

using MultiServer.Addons.Org.BouncyCastle.Crypto.Prng;

namespace MultiServer.Addons.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    internal sealed class BcTlsNonceGenerator
        : TlsNonceGenerator
    {
        private readonly IRandomGenerator m_randomGenerator;

        internal BcTlsNonceGenerator(IRandomGenerator randomGenerator)
        {
            this.m_randomGenerator = randomGenerator;
        }

        public byte[] GenerateNonce(int size)
        {
            byte[] nonce = new byte[size];
            m_randomGenerator.NextBytes(nonce);
            return nonce;
        }
    }
}
