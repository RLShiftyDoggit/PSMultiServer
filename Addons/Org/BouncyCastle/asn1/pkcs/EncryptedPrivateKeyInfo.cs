using System;

using MultiServer.Addons.Org.BouncyCastle.Asn1.X509;

namespace MultiServer.Addons.Org.BouncyCastle.Asn1.Pkcs
{
    public class EncryptedPrivateKeyInfo
        : Asn1Encodable
    {
        private readonly AlgorithmIdentifier algId;
        private readonly Asn1OctetString data;

		private EncryptedPrivateKeyInfo(Asn1Sequence seq)
        {
			if (seq.Count != 2)
				throw new ArgumentException("Wrong number of elements in sequence", "seq");

            algId = AlgorithmIdentifier.GetInstance(seq[0]);
            data = Asn1OctetString.GetInstance(seq[1]);
        }

		public EncryptedPrivateKeyInfo(
            AlgorithmIdentifier	algId,
            byte[]				encoding)
        {
            this.algId = algId;
            this.data = new DerOctetString(encoding);
        }

        public static EncryptedPrivateKeyInfo GetInstance(object obj)
        {
            if (obj == null)
                return null;
            if (obj is EncryptedPrivateKeyInfo encryptedPrivateKeyInfo)
                return encryptedPrivateKeyInfo;
            return new EncryptedPrivateKeyInfo(Asn1Sequence.GetInstance(obj));
        }

		public AlgorithmIdentifier EncryptionAlgorithm
		{
			get { return algId; }
		}

		public byte[] GetEncryptedData()
        {
            return data.GetOctets();
        }

		/**
         * Produce an object suitable for an Asn1OutputStream.
         * <pre>
         * EncryptedPrivateKeyInfo ::= Sequence {
         *      encryptionAlgorithm AlgorithmIdentifier {{KeyEncryptionAlgorithms}},
         *      encryptedData EncryptedData
         * }
         *
         * EncryptedData ::= OCTET STRING
         *
         * KeyEncryptionAlgorithms ALGORITHM-IDENTIFIER ::= {
         *          ... -- For local profiles
         * }
         * </pre>
         */
        public override Asn1Object ToAsn1Object()
        {
			return new DerSequence(algId, data);
        }
    }
}
