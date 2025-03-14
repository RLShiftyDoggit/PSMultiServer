using System;

using MultiServer.Addons.Org.BouncyCastle.Asn1.Crmf;

namespace MultiServer.Addons.Org.BouncyCastle.Asn1.Cmp
{
	public class CertifiedKeyPair
		: Asn1Encodable
	{
        public static CertifiedKeyPair GetInstance(object obj)
        {
            if (obj == null)
                return null;
            if (obj is CertifiedKeyPair certifiedKeyPair)
                return certifiedKeyPair;
            return new CertifiedKeyPair(Asn1Sequence.GetInstance(obj));
        }

        public static CertifiedKeyPair GetInstance(Asn1TaggedObject taggedObject, bool declaredExplicit)
        {
            return new CertifiedKeyPair(Asn1Sequence.GetInstance(taggedObject, declaredExplicit));
        }

        private readonly CertOrEncCert m_certOrEncCert;
		private readonly EncryptedKey m_privateKey;
		private readonly PkiPublicationInfo m_publicationInfo;

        private CertifiedKeyPair(Asn1Sequence seq)
		{
			m_certOrEncCert = CertOrEncCert.GetInstance(seq[0]);

			if (seq.Count >= 2)
			{
				if (seq.Count == 2)
				{
					Asn1TaggedObject tagged = Asn1TaggedObject.GetInstance(seq[1], Asn1Tags.ContextSpecific);
					if (tagged.TagNo == 0)
					{
						m_privateKey = EncryptedKey.GetInstance(tagged.GetExplicitBaseObject());
					}
					else
					{
						m_publicationInfo = PkiPublicationInfo.GetInstance(tagged.GetExplicitBaseObject());
					}
				}
				else
				{
                    m_privateKey = EncryptedKey.GetInstance(
						Asn1TaggedObject.GetInstance(seq[1], Asn1Tags.ContextSpecific).GetExplicitBaseObject());
                    m_publicationInfo = PkiPublicationInfo.GetInstance(
						Asn1TaggedObject.GetInstance(seq[2], Asn1Tags.ContextSpecific).GetExplicitBaseObject());
				}
			}
		}

		public CertifiedKeyPair(CertOrEncCert certOrEncCert)
			: this(certOrEncCert, (EncryptedKey)null, null)
		{
		}

        public CertifiedKeyPair(CertOrEncCert certOrEncCert, EncryptedValue privateKey,
            PkiPublicationInfo publicationInfo)
            : this(certOrEncCert, privateKey == null ? null : new EncryptedKey(privateKey), publicationInfo)
        {
        }

        public CertifiedKeyPair(CertOrEncCert certOrEncCert, EncryptedKey privateKey,
			PkiPublicationInfo publicationInfo)
        {
			if (certOrEncCert == null)
				throw new ArgumentNullException(nameof(certOrEncCert));

            m_certOrEncCert = certOrEncCert;
            m_privateKey = privateKey;
            m_publicationInfo = publicationInfo;
        }

		public virtual CertOrEncCert CertOrEncCert => m_certOrEncCert;

		public virtual EncryptedKey PrivateKey => m_privateKey;

		public virtual PkiPublicationInfo PublicationInfo => m_publicationInfo;

		/**
		 * <pre>
		 * CertifiedKeyPair ::= SEQUENCE {
		 *                                  certOrEncCert       CertOrEncCert,
		 *                                  privateKey      [0] EncryptedValue      OPTIONAL,
		 *                                  -- see [CRMF] for comment on encoding
		 *                                  publicationInfo [1] PKIPublicationInfo  OPTIONAL
		 *       }
		 * </pre>
		 * @return a basic ASN.1 object representation.
		 */
		public override Asn1Object ToAsn1Object()
		{
			Asn1EncodableVector v = new Asn1EncodableVector(3);
			v.Add(m_certOrEncCert);
            v.AddOptionalTagged(true, 0, m_privateKey);
            v.AddOptionalTagged(true, 1, m_publicationInfo);
			return new DerSequence(v);
		}
	}
}
