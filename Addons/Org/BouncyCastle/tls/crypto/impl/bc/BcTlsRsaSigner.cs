using System;

using MultiServer.Addons.Org.BouncyCastle.Crypto;
using MultiServer.Addons.Org.BouncyCastle.Crypto.Digests;
using MultiServer.Addons.Org.BouncyCastle.Crypto.Encodings;
using MultiServer.Addons.Org.BouncyCastle.Crypto.Engines;
using MultiServer.Addons.Org.BouncyCastle.Crypto.Parameters;
using MultiServer.Addons.Org.BouncyCastle.Crypto.Signers;

namespace MultiServer.Addons.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    /// <summary>Operator supporting the generation of RSASSA-PKCS1-v1_5 signatures using the BC light-weight API.
    /// </summary>
    public class BcTlsRsaSigner
        : BcTlsSigner
    {
        private readonly RsaKeyParameters m_publicKey;

        public BcTlsRsaSigner(BcTlsCrypto crypto, RsaKeyParameters privateKey, RsaKeyParameters publicKey)
            : base(crypto, privateKey)
        {
            this.m_publicKey = publicKey;
        }

        public override byte[] GenerateRawSignature(SignatureAndHashAlgorithm algorithm, byte[] hash)
        {
            IDigest nullDigest = new NullDigest();

            ISigner signer;
            if (algorithm != null)
            {
                if (algorithm.Signature != SignatureAlgorithm.rsa)
                    throw new InvalidOperationException("Invalid algorithm: " + algorithm);

                /*
                 * RFC 5246 4.7. In RSA signing, the opaque vector contains the signature generated
                 * using the RSASSA-PKCS1-v1_5 signature scheme defined in [PKCS1].
                 */
                signer = new RsaDigestSigner(nullDigest, TlsUtilities.GetOidForHashAlgorithm(algorithm.Hash));
            }
            else
            {
                /*
                 * RFC 5246 4.7. Note that earlier versions of TLS used a different RSA signature scheme
                 * that did not include a DigestInfo encoding.
                 */
                signer = new GenericSigner(new Pkcs1Encoding(new RsaBlindedEngine()), nullDigest);
            }
            signer.Init(true, new ParametersWithRandom(m_privateKey, m_crypto.SecureRandom));
            signer.BlockUpdate(hash, 0, hash.Length);
            try
            {
                byte[] signature = signer.GenerateSignature();

                signer.Init(false, m_publicKey);
                signer.BlockUpdate(hash, 0, hash.Length);

                if (signer.VerifySignature(signature))
                {
                    return signature;
                }
            }
            catch (CryptoException e)
            {
                throw new TlsFatalAlert(AlertDescription.internal_error, e);
            }

            throw new TlsFatalAlert(AlertDescription.internal_error);
        }
    }
}
