using System;

using MultiServer.Addons.Org.BouncyCastle.Asn1.X509;
using MultiServer.Addons.Org.BouncyCastle.Utilities;

namespace MultiServer.Addons.Org.BouncyCastle.Asn1.Ess
{
	public class SigningCertificate
		: Asn1Encodable
	{
		private Asn1Sequence certs, policies;

		public static SigningCertificate GetInstance(
			object o)
		{
			if (o == null || o is SigningCertificate)
			{
				return (SigningCertificate) o;
			}

			if (o is Asn1Sequence)
			{
				return new SigningCertificate((Asn1Sequence) o);
			}

			throw new ArgumentException(
				"unknown object in 'SigningCertificate' factory : "
                + Platform.GetTypeName(o) + ".");
		}

		/**
		 * constructors
		 */
		public SigningCertificate(
			Asn1Sequence seq)
		{
			if (seq.Count < 1 || seq.Count > 2)
			{
				throw new ArgumentException("Bad sequence size: " + seq.Count);
			}

			this.certs = Asn1Sequence.GetInstance(seq[0]);

			if (seq.Count > 1)
			{
				this.policies = Asn1Sequence.GetInstance(seq[1]);
			}
		}

		public SigningCertificate(
			EssCertID essCertID)
		{
			certs = new DerSequence(essCertID);
		}

        public EssCertID[] GetCerts()
        {
            return certs.MapElements(EssCertID.GetInstance);
        }

        public PolicyInformation[] GetPolicies()
        {
            return policies?.MapElements(PolicyInformation.GetInstance);
        }

        /**
		 * The definition of SigningCertificate is
		 * <pre>
		 * SigningCertificate ::=  SEQUENCE {
		 *      certs        SEQUENCE OF EssCertID,
		 *      policies     SEQUENCE OF PolicyInformation OPTIONAL
		 * }
		 * </pre>
		 * id-aa-signingCertificate OBJECT IDENTIFIER ::= { iso(1)
		 *  member-body(2) us(840) rsadsi(113549) pkcs(1) pkcs9(9)
		 *  smime(16) id-aa(2) 12 }
		 */
        public override Asn1Object ToAsn1Object()
		{
			Asn1EncodableVector v = new Asn1EncodableVector(certs);
            v.AddOptional(policies);
			return new DerSequence(v);
		}
	}
}
