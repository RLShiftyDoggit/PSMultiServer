using System;

using MultiServer.Addons.Org.BouncyCastle.Asn1.X509;
using MultiServer.Addons.Org.BouncyCastle.Utilities;

namespace MultiServer.Addons.Org.BouncyCastle.Asn1.Ocsp
{
    public class RevokedInfo
        : Asn1Encodable
    {
        private readonly Asn1GeneralizedTime revocationTime;
        private readonly CrlReason revocationReason;

		public static RevokedInfo GetInstance(
			Asn1TaggedObject	obj,
			bool				explicitly)
		{
			return GetInstance(Asn1Sequence.GetInstance(obj, explicitly));
		}

		public static RevokedInfo GetInstance(
			object obj)
		{
			if (obj == null || obj is RevokedInfo)
			{
				return (RevokedInfo) obj;
			}

			if (obj is Asn1Sequence)
			{
				return new RevokedInfo((Asn1Sequence) obj);
			}

            throw new ArgumentException("unknown object in factory: " + Platform.GetTypeName(obj), "obj");
		}

		public RevokedInfo(
            Asn1GeneralizedTime revocationTime)
			: this(revocationTime, null)
		{
		}

		public RevokedInfo(
            Asn1GeneralizedTime revocationTime,
            CrlReason           revocationReason)
        {
			if (revocationTime == null)
				throw new ArgumentNullException("revocationTime");

			this.revocationTime = revocationTime;
            this.revocationReason = revocationReason;
        }

		private RevokedInfo(
            Asn1Sequence seq)
        {
            this.revocationTime = (Asn1GeneralizedTime)seq[0];

			if (seq.Count > 1)
            {
                this.revocationReason = new CrlReason(
					DerEnumerated.GetInstance((Asn1TaggedObject) seq[1], true));
            }
        }

		public Asn1GeneralizedTime RevocationTime
		{
			get { return revocationTime; }
		}

		public CrlReason RevocationReason
		{
			get { return revocationReason; }
		}

		/**
         * Produce an object suitable for an Asn1OutputStream.
         * <pre>
         * RevokedInfo ::= Sequence {
         *      revocationTime              GeneralizedTime,
         *      revocationReason    [0]     EXPLICIT CRLReason OPTIONAL }
         * </pre>
         */
        public override Asn1Object ToAsn1Object()
        {
            Asn1EncodableVector v = new Asn1EncodableVector(revocationTime);
            v.AddOptionalTagged(true, 0, revocationReason);
            return new DerSequence(v);
        }
    }
}
