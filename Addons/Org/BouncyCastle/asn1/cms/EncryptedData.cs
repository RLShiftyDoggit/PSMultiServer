using System;

namespace MultiServer.Addons.Org.BouncyCastle.Asn1.Cms
{
    public class EncryptedData
		: Asn1Encodable
	{
        public static EncryptedData GetInstance(object obj)
        {
            if (obj == null)
                return null;
            if (obj is EncryptedData encryptedData)
                return encryptedData;
            return new EncryptedData(Asn1Sequence.GetInstance(obj));
        }

		public static EncryptedData GetInstance(Asn1TaggedObject taggedObject, bool declaredExplicit)
		{
            return new EncryptedData(Asn1Sequence.GetInstance(taggedObject, declaredExplicit));
        }

        private readonly DerInteger				version;
		private readonly EncryptedContentInfo	encryptedContentInfo;
		private readonly Asn1Set				unprotectedAttrs;

		public EncryptedData(
			EncryptedContentInfo encInfo)
			: this(encInfo, null)
		{
		}

		public EncryptedData(
			EncryptedContentInfo	encInfo,
			Asn1Set					unprotectedAttrs)
		{
			if (encInfo == null)
				throw new ArgumentNullException("encInfo");

			this.version = new DerInteger((unprotectedAttrs == null) ? 0 : 2);
			this.encryptedContentInfo = encInfo;
			this.unprotectedAttrs = unprotectedAttrs;
		}

		private EncryptedData(
			Asn1Sequence seq)
		{
			if (seq == null)
				throw new ArgumentNullException("seq");
			if (seq.Count < 2 || seq.Count > 3)
				throw new ArgumentException("Bad sequence size: " + seq.Count, "seq");

			this.version = DerInteger.GetInstance(seq[0]);
			this.encryptedContentInfo = EncryptedContentInfo.GetInstance(seq[1]);

			if (seq.Count > 2)
			{
                this.unprotectedAttrs = Asn1Set.GetInstance((Asn1TaggedObject)seq[2], false);
            }
		}

		public virtual DerInteger Version
		{
			get { return version; }
		}

		public virtual EncryptedContentInfo EncryptedContentInfo
		{
			get { return encryptedContentInfo; }
		}

		public virtual Asn1Set UnprotectedAttrs
		{
			get { return unprotectedAttrs; }
		}

		/**
		* <pre>
		*       EncryptedData ::= SEQUENCE {
		*                     version CMSVersion,
		*                     encryptedContentInfo EncryptedContentInfo,
		*                     unprotectedAttrs [1] IMPLICIT UnprotectedAttributes OPTIONAL }
		* </pre>
		* @return a basic ASN.1 object representation.
		*/
		public override Asn1Object ToAsn1Object()
		{
			Asn1EncodableVector v = new Asn1EncodableVector(version, encryptedContentInfo);

			if (unprotectedAttrs != null)
			{
				v.Add(new BerTaggedObject(false, 1, unprotectedAttrs));
			}

			return new BerSequence(v);
		}
	}
}
