using System;

using MultiServer.Addons.Org.BouncyCastle.Asn1;
using MultiServer.Addons.Org.BouncyCastle.Math;
using MultiServer.Addons.Org.BouncyCastle.Math.EC;

namespace MultiServer.Addons.Org.BouncyCastle.Bcpg
{
    /// <remarks>Base class for an ECDH Public Key.</remarks>
    public class ECDHPublicBcpgKey
        : ECPublicBcpgKey
    {
        private byte reserved;
        private HashAlgorithmTag hashFunctionId;
        private SymmetricKeyAlgorithmTag symAlgorithmId;

        /// <param name="bcpgIn">The stream to read the packet from.</param>
        public ECDHPublicBcpgKey(BcpgInputStream bcpgIn)
            : base(bcpgIn)
        {
            int length = bcpgIn.ReadByte();
            if (length != 3)
                throw new InvalidOperationException("KDF parameters size of 3 expected.");

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            Span<byte> kdfParameters = stackalloc byte[3];
#else
            byte[] kdfParameters = new byte[3];
#endif
            bcpgIn.ReadFully(kdfParameters);

            reserved = kdfParameters[0];
            hashFunctionId = (HashAlgorithmTag)kdfParameters[1];
            symAlgorithmId = (SymmetricKeyAlgorithmTag)kdfParameters[2];

            VerifyHashAlgorithm();
            VerifySymmetricKeyAlgorithm();
        }

        public ECDHPublicBcpgKey(
            DerObjectIdentifier oid,
            ECPoint point,
            HashAlgorithmTag hashAlgorithm,
            SymmetricKeyAlgorithmTag symmetricKeyAlgorithm)
            : base(oid, point)
        {
            reserved = 1;
            hashFunctionId = hashAlgorithm;
            symAlgorithmId = symmetricKeyAlgorithm;

            VerifyHashAlgorithm();
            VerifySymmetricKeyAlgorithm();
        }

        public ECDHPublicBcpgKey(
            DerObjectIdentifier oid,
            BigInteger point,
            HashAlgorithmTag hashAlgorithm,
            SymmetricKeyAlgorithmTag symmetricKeyAlgorithm)
            : base(oid, point)
        {
            reserved = 1;
            hashFunctionId = hashAlgorithm;
            symAlgorithmId = symmetricKeyAlgorithm;

            VerifyHashAlgorithm();
            VerifySymmetricKeyAlgorithm();
        }

        public virtual byte Reserved
        {
            get { return reserved; }
        }

        public virtual HashAlgorithmTag HashAlgorithm
        {
            get { return hashFunctionId; }
        }

        public virtual SymmetricKeyAlgorithmTag SymmetricKeyAlgorithm
        {
            get { return symAlgorithmId; }
        }

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            base.Encode(bcpgOut);
            bcpgOut.WriteByte(0x3);
            bcpgOut.WriteByte(reserved);
            bcpgOut.WriteByte((byte)hashFunctionId);
            bcpgOut.WriteByte((byte)symAlgorithmId);
        }

        private void VerifyHashAlgorithm()
        {
            switch (hashFunctionId)
            {
            case HashAlgorithmTag.Sha256:
            case HashAlgorithmTag.Sha384:
            case HashAlgorithmTag.Sha512:
                break;
            default:
                throw new InvalidOperationException("Hash algorithm must be SHA-256 or stronger.");
            }
        }

        private void VerifySymmetricKeyAlgorithm()
        {
            switch (symAlgorithmId)
            {
            case SymmetricKeyAlgorithmTag.Aes128:
            case SymmetricKeyAlgorithmTag.Aes192:
            case SymmetricKeyAlgorithmTag.Aes256:
                break;
            default:
                throw new InvalidOperationException("Symmetric key algorithm must be AES-128 or stronger.");
            }
        }
    }
}
