using System;

using MultiServer.Addons.Org.BouncyCastle.Crypto;
using MultiServer.Addons.Org.BouncyCastle.Crypto.Parameters;
using MultiServer.Addons.Org.BouncyCastle.Crypto.Signers;

namespace MultiServer.Addons.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    /// <summary>Implementation class for the verification of the raw DSA signature type using the BC light-weight API.
    /// </summary>
    public class BcTlsDsaVerifier
        : BcTlsDssVerifier
    {
        public BcTlsDsaVerifier(BcTlsCrypto crypto, DsaPublicKeyParameters publicKey)
            : base(crypto, publicKey)
        {
        }

        protected override IDsa CreateDsaImpl()
        {
            return new DsaSigner();
        }

        protected override short SignatureAlgorithm
        {
            get { return Tls.SignatureAlgorithm.dsa; }
        }
    }
}
