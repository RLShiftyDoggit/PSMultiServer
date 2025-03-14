using System.Collections.Generic;

using MultiServer.Addons.Org.BouncyCastle.Asn1;
using MultiServer.Addons.Org.BouncyCastle.Asn1.Nist;
using MultiServer.Addons.Org.BouncyCastle.Asn1.Ntt;
using MultiServer.Addons.Org.BouncyCastle.Crypto;
using MultiServer.Addons.Org.BouncyCastle.Crypto.Operators;

namespace MultiServer.Addons.Org.BouncyCastle.Operators
{
    public class CmsContentEncryptorBuilder
    {
        private static readonly IDictionary<DerObjectIdentifier, int> KeySizes =
            new Dictionary<DerObjectIdentifier, int>();

        static CmsContentEncryptorBuilder()
        {
            KeySizes[NistObjectIdentifiers.IdAes128Cbc] = 128;
            KeySizes[NistObjectIdentifiers.IdAes192Cbc] = 192;
            KeySizes[NistObjectIdentifiers.IdAes256Cbc] = 256;

            KeySizes[NttObjectIdentifiers.IdCamellia128Cbc] = 128;
            KeySizes[NttObjectIdentifiers.IdCamellia192Cbc] = 192;
            KeySizes[NttObjectIdentifiers.IdCamellia256Cbc] = 256;
        }

        private static int GetKeySize(DerObjectIdentifier oid)
        {
            return KeySizes.TryGetValue(oid, out var keySize) ? keySize : -1;
        }

        private readonly DerObjectIdentifier encryptionOID;
        private readonly int keySize;

        //private SecureRandom random;

        public CmsContentEncryptorBuilder(DerObjectIdentifier encryptionOID)
            : this(encryptionOID, GetKeySize(encryptionOID))
        {
        }

        public CmsContentEncryptorBuilder(DerObjectIdentifier encryptionOID, int keySize)
        {
            this.encryptionOID = encryptionOID;
            this.keySize = keySize;
        }

        public ICipherBuilderWithKey Build()
        {
            //return new Asn1CipherBuilderWithKey(encryptionOID, keySize, random);
            return new Asn1CipherBuilderWithKey(encryptionOID, keySize, null);
        }
    }
}
