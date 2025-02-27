using System;

using MultiServer.Addons.Org.BouncyCastle.Utilities;

namespace MultiServer.Addons.Org.BouncyCastle.Asn1.Cms
{
    public class OriginatorInfo
        : Asn1Encodable
    {
        public static OriginatorInfo GetInstance(object obj)
        {
            if (obj == null)
                return null;
            if (obj is OriginatorInfo originatorInfo)
                return originatorInfo;
            return new OriginatorInfo(Asn1Sequence.GetInstance(obj));
        }

        public static OriginatorInfo GetInstance(Asn1TaggedObject obj, bool explicitly)
        {
            return new OriginatorInfo(Asn1Sequence.GetInstance(obj, explicitly));
        }

        private Asn1Set certs;
        private Asn1Set crls;

        public OriginatorInfo(
            Asn1Set certs,
            Asn1Set crls)
        {
            this.certs = certs;
            this.crls = crls;
        }

		public OriginatorInfo(
            Asn1Sequence seq)
        {
            switch (seq.Count)
            {
            case 0:     // empty
                break;
            case 1:
                Asn1TaggedObject o = (Asn1TaggedObject) seq[0];
                switch (o.TagNo)
                {
                case 0 :
                    certs = Asn1Set.GetInstance(o, false);
                    break;
                case 1 :
                    crls = Asn1Set.GetInstance(o, false);
                    break;
                default:
                    throw new ArgumentException("Bad tag in OriginatorInfo: " + o.TagNo);
                }
                break;
            case 2:
                certs = Asn1Set.GetInstance((Asn1TaggedObject) seq[0], false);
                crls  = Asn1Set.GetInstance((Asn1TaggedObject) seq[1], false);
                break;
            default:
                throw new ArgumentException("OriginatorInfo too big");
            }
        }

        public Asn1Set Certificates
		{
			get { return certs; }
		}

		public Asn1Set Crls
		{
			get { return crls; }
		}

		/**
         * Produce an object suitable for an Asn1OutputStream.
         * <pre>
         * OriginatorInfo ::= Sequence {
         *     certs [0] IMPLICIT CertificateSet OPTIONAL,
         *     crls [1] IMPLICIT CertificateRevocationLists OPTIONAL
         * }
         * </pre>
         */
        public override Asn1Object ToAsn1Object()
        {
            Asn1EncodableVector v = new Asn1EncodableVector(2);
            v.AddOptionalTagged(false, 0, certs);
            v.AddOptionalTagged(false, 1, crls);
			return new DerSequence(v);
        }
    }
}
