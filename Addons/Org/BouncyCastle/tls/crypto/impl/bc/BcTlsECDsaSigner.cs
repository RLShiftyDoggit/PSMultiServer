using System;

using MultiServer.Addons.Org.BouncyCastle.Crypto;
using MultiServer.Addons.Org.BouncyCastle.Crypto.Parameters;
using MultiServer.Addons.Org.BouncyCastle.Crypto.Signers;

namespace MultiServer.Addons.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    /// <summary>Implementation class for generation of the raw ECDSA signature type using the BC light-weight API.
    /// </summary>
    public class BcTlsECDsaSigner
        : BcTlsDssSigner
    {
        public BcTlsECDsaSigner(BcTlsCrypto crypto, ECPrivateKeyParameters privateKey)
            : base(crypto, privateKey)
        {
        }

        protected override IDsa CreateDsaImpl(int cryptoHashAlgorithm)
        {
            return new ECDsaSigner(new HMacDsaKCalculator(m_crypto.CreateDigest(cryptoHashAlgorithm)));
        }

        protected override short SignatureAlgorithm
        {
            get { return Tls.SignatureAlgorithm.ecdsa; }
        }
    }
}
