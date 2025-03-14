using System;

using MultiServer.Addons.Org.BouncyCastle.Asn1.X509;
using MultiServer.Addons.Org.BouncyCastle.Utilities;

namespace MultiServer.Addons.Org.BouncyCastle.Asn1.Esf
{
	public class SignerAttribute
		: Asn1Encodable
	{
		private Asn1Sequence			claimedAttributes;
		private AttributeCertificate	certifiedAttributes;

		public static SignerAttribute GetInstance(
			object obj)
		{
			if (obj == null || obj is SignerAttribute)
				return (SignerAttribute) obj;

			if (obj is Asn1Sequence)
				return new SignerAttribute(obj);

			throw new ArgumentException(
				"Unknown object in 'SignerAttribute' factory: "
                + Platform.GetTypeName(obj),
				"obj");
		}

		private SignerAttribute(object obj)
		{
			Asn1Sequence seq = (Asn1Sequence)obj;
			Asn1TaggedObject taggedObject = (Asn1TaggedObject)seq[0];
			if (taggedObject.TagNo == 0)
			{
				claimedAttributes = Asn1Sequence.GetInstance(taggedObject, true);
			}
			else if (taggedObject.TagNo == 1)
			{
				certifiedAttributes = AttributeCertificate.GetInstance(taggedObject);
			}
			else
			{
				throw new ArgumentException("illegal tag.", "obj");
			}
		}

		public SignerAttribute(
			Asn1Sequence claimedAttributes)
		{
			this.claimedAttributes = claimedAttributes;
		}

		public SignerAttribute(
			AttributeCertificate certifiedAttributes)
		{
			this.certifiedAttributes = certifiedAttributes;
		}

		public virtual Asn1Sequence ClaimedAttributes
		{
			get { return claimedAttributes; }
		}

		public virtual AttributeCertificate CertifiedAttributes
		{
			get { return certifiedAttributes; }
		}

		/**
		*
		* <pre>
		*  SignerAttribute ::= SEQUENCE OF CHOICE {
		*      claimedAttributes   [0] ClaimedAttributes,
		*      certifiedAttributes [1] CertifiedAttributes }
		*
		*  ClaimedAttributes ::= SEQUENCE OF Attribute
		*  CertifiedAttributes ::= AttributeCertificate -- as defined in RFC 3281: see clause 4.1.
		* </pre>
		*/
		public override Asn1Object ToAsn1Object()
		{
			return claimedAttributes != null
				?	new DerSequence(new DerTaggedObject(0, claimedAttributes))
				:	new DerSequence(new DerTaggedObject(1, certifiedAttributes));
		}
	}
}
