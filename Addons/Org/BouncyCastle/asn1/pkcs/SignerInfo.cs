using MultiServer.Addons.Org.BouncyCastle.Asn1.X509;

namespace MultiServer.Addons.Org.BouncyCastle.Asn1.Pkcs
{
    /**
     * a Pkcs#7 signer info object.
     */
    public class SignerInfo
        : Asn1Encodable
    {
        private DerInteger              version;
        private IssuerAndSerialNumber   issuerAndSerialNumber;
        private AlgorithmIdentifier     digAlgorithm;
        private Asn1Set                 authenticatedAttributes;
        private AlgorithmIdentifier     digEncryptionAlgorithm;
        private Asn1OctetString         encryptedDigest;
        private Asn1Set                 unauthenticatedAttributes;

        public static SignerInfo GetInstance(object obj)
        {
            if (obj == null)
                return null;
            if (obj is SignerInfo signerInfo)
                return signerInfo;
            return new SignerInfo(Asn1Sequence.GetInstance(obj));
        }

		public SignerInfo(
            DerInteger              version,
            IssuerAndSerialNumber   issuerAndSerialNumber,
            AlgorithmIdentifier     digAlgorithm,
            Asn1Set                 authenticatedAttributes,
            AlgorithmIdentifier     digEncryptionAlgorithm,
            Asn1OctetString         encryptedDigest,
            Asn1Set                 unauthenticatedAttributes)
        {
            this.version = version;
            this.issuerAndSerialNumber = issuerAndSerialNumber;
            this.digAlgorithm = digAlgorithm;
            this.authenticatedAttributes = authenticatedAttributes;
            this.digEncryptionAlgorithm = digEncryptionAlgorithm;
            this.encryptedDigest = encryptedDigest;
            this.unauthenticatedAttributes = unauthenticatedAttributes;
        }

		public SignerInfo(
            Asn1Sequence seq)
        {
            var e = seq.GetEnumerator();

			e.MoveNext();
            version = (DerInteger) e.Current;

			e.MoveNext();
            issuerAndSerialNumber = IssuerAndSerialNumber.GetInstance(e.Current);

			e.MoveNext();
            digAlgorithm = AlgorithmIdentifier.GetInstance(e.Current);

			e.MoveNext();
            var obj = e.Current;

			if (obj is Asn1TaggedObject tagged)
            {
                authenticatedAttributes = Asn1Set.GetInstance(tagged, false);

				e.MoveNext();
                digEncryptionAlgorithm = AlgorithmIdentifier.GetInstance(e.Current);
            }
            else
            {
                authenticatedAttributes = null;
                digEncryptionAlgorithm = AlgorithmIdentifier.GetInstance(obj);
            }

			e.MoveNext();
            encryptedDigest = Asn1OctetString.GetInstance(e.Current);

			if (e.MoveNext())
            {
                unauthenticatedAttributes = Asn1Set.GetInstance((Asn1TaggedObject)e.Current, false);
            }
            else
            {
                unauthenticatedAttributes = null;
            }
        }

		public DerInteger Version { get { return version; } }

		public IssuerAndSerialNumber IssuerAndSerialNumber { get { return issuerAndSerialNumber; } }

		public Asn1Set AuthenticatedAttributes { get { return authenticatedAttributes; } }

		public AlgorithmIdentifier DigestAlgorithm { get { return digAlgorithm; } }

		public Asn1OctetString EncryptedDigest { get { return encryptedDigest; } }

		public AlgorithmIdentifier DigestEncryptionAlgorithm { get { return digEncryptionAlgorithm; } }

		public Asn1Set UnauthenticatedAttributes { get { return unauthenticatedAttributes; } }

		/**
         * Produce an object suitable for an Asn1OutputStream.
         * <pre>
         *  SignerInfo ::= Sequence {
         *      version Version,
         *      issuerAndSerialNumber IssuerAndSerialNumber,
         *      digestAlgorithm DigestAlgorithmIdentifier,
         *      authenticatedAttributes [0] IMPLICIT Attributes OPTIONAL,
         *      digestEncryptionAlgorithm DigestEncryptionAlgorithmIdentifier,
         *      encryptedDigest EncryptedDigest,
         *      unauthenticatedAttributes [1] IMPLICIT Attributes OPTIONAL
         *  }
         *
         *  EncryptedDigest ::= OCTET STRING
         *
         *  DigestAlgorithmIdentifier ::= AlgorithmIdentifier
         *
         *  DigestEncryptionAlgorithmIdentifier ::= AlgorithmIdentifier
         * </pre>
         */
        public override Asn1Object ToAsn1Object()
        {
            Asn1EncodableVector v = new Asn1EncodableVector(version, issuerAndSerialNumber, digAlgorithm);
            v.AddOptionalTagged(false, 0, authenticatedAttributes);
            v.Add(digEncryptionAlgorithm, encryptedDigest);
            v.AddOptionalTagged(false, 1, unauthenticatedAttributes);
            return new DerSequence(v);
        }
    }
}
