using System;

using MultiServer.Addons.Org.BouncyCastle.Utilities;

namespace MultiServer.Addons.Org.BouncyCastle.Asn1.Esf
{
	/// <remarks>
	/// RFC 3126: 4.2.2 Complete Revocation Refs Attribute Definition
	/// <code>
	/// CrlOcspRef ::= SEQUENCE {
	///		crlids		[0] CRLListID		OPTIONAL,
	/// 	ocspids		[1] OcspListID		OPTIONAL,
	/// 	otherRev	[2] OtherRevRefs	OPTIONAL
	/// }
	/// </code>
	/// </remarks>
	public class CrlOcspRef
		: Asn1Encodable
	{
		private readonly CrlListID		crlids;
		private readonly OcspListID		ocspids;
		private readonly OtherRevRefs	otherRev;

		public static CrlOcspRef GetInstance(
			object obj)
		{
			if (obj == null || obj is CrlOcspRef)
				return (CrlOcspRef) obj;

			if (obj is Asn1Sequence)
				return new CrlOcspRef((Asn1Sequence) obj);

			throw new ArgumentException(
				"Unknown object in 'CrlOcspRef' factory: "
                    + Platform.GetTypeName(obj),
				"obj");
		}

		private CrlOcspRef(Asn1Sequence seq)
		{
			if (seq == null)
				throw new ArgumentNullException("seq");

			foreach (var element in seq)
			{
				var o = Asn1TaggedObject.GetInstance(element, Asn1Tags.ContextSpecific);
				switch (o.TagNo)
				{
				case 0:
					this.crlids = CrlListID.GetInstance(o.GetExplicitBaseObject());
					break;
				case 1:
					this.ocspids = OcspListID.GetInstance(o.GetExplicitBaseObject());
					break;
				case 2:
					this.otherRev = OtherRevRefs.GetInstance(o.GetExplicitBaseObject());
					break;
				default:
					throw new ArgumentException("Illegal tag in CrlOcspRef", "seq");
				}
			}
		}

		public CrlOcspRef(
			CrlListID		crlids,
			OcspListID		ocspids,
			OtherRevRefs	otherRev)
		{
			this.crlids = crlids;
			this.ocspids = ocspids;
			this.otherRev = otherRev;
		}

		public CrlListID CrlIDs
		{
			get { return crlids; }
		}

		public OcspListID OcspIDs
		{
			get { return ocspids; }
		}

		public OtherRevRefs OtherRev
		{
			get { return otherRev; }
		}

		public override Asn1Object ToAsn1Object()
		{
			Asn1EncodableVector v = new Asn1EncodableVector(3);

			if (crlids != null)
			{
				v.Add(new DerTaggedObject(true, 0, crlids.ToAsn1Object()));
			}

			if (ocspids != null)
			{
				v.Add(new DerTaggedObject(true, 1, ocspids.ToAsn1Object()));
			}

			if (otherRev != null)
			{
				v.Add(new DerTaggedObject(true, 2, otherRev.ToAsn1Object()));
			}

			return new DerSequence(v);
		}
	}
}
