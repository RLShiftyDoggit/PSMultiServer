using System;

namespace MultiServer.Addons.Org.BouncyCastle.Asn1
{
	/**
	 * DER TaggedObject - in ASN.1 notation this is any object preceded by
	 * a [n] where n is some number - these are assumed to follow the construction
	 * rules (as with sequences).
	 */
	public class DerTaggedObject
		: Asn1TaggedObject
	{
        public DerTaggedObject(int tagNo, Asn1Encodable obj)
			: base(true, tagNo, obj)
		{
		}

        public DerTaggedObject(int tagClass, int tagNo, Asn1Encodable obj)
            : base(true, tagClass, tagNo, obj)
        {
        }

        /**
		 * @param isExplicit true if an explicitly tagged object.
		 * @param tagNo the tag number for this object.
		 * @param obj the tagged object.
		 */
        public DerTaggedObject(bool isExplicit, int tagNo, Asn1Encodable obj)
			: base(isExplicit, tagNo, obj)
		{
		}

        public DerTaggedObject(bool isExplicit, int tagClass, int tagNo, Asn1Encodable obj)
            : base(isExplicit, tagClass, tagNo, obj)
        {
        }

        internal DerTaggedObject(int explicitness, int tagClass, int tagNo, Asn1Encodable obj)
            : base(explicitness, tagClass, tagNo, obj)
        {
        }

        internal override IAsn1Encoding GetEncoding(int encoding)
        {
            encoding = Asn1OutputStream.EncodingDer;

            Asn1Object baseObject = GetBaseObject().ToAsn1Object();

            if (!IsExplicit())
                return baseObject.GetEncodingImplicit(encoding, TagClass, TagNo);

            return new TaggedDLEncoding(TagClass, TagNo, baseObject.GetEncoding(encoding));
        }

        internal override IAsn1Encoding GetEncodingImplicit(int encoding, int tagClass, int tagNo)
        {
            encoding = Asn1OutputStream.EncodingDer;

            Asn1Object baseObject = GetBaseObject().ToAsn1Object();

            if (!IsExplicit())
                return baseObject.GetEncodingImplicit(encoding, tagClass, tagNo);

            return new TaggedDLEncoding(tagClass, tagNo, baseObject.GetEncoding(encoding));
        }

        internal sealed override DerEncoding GetEncodingDer()
        {
            Asn1Object baseObject = GetBaseObject().ToAsn1Object();

            if (!IsExplicit())
                return baseObject.GetEncodingDerImplicit(TagClass, TagNo);

            return new TaggedDerEncoding(TagClass, TagNo, baseObject.GetEncodingDer());
        }

        internal sealed override DerEncoding GetEncodingDerImplicit(int tagClass, int tagNo)
        {
            Asn1Object baseObject = GetBaseObject().ToAsn1Object();

            if (!IsExplicit())
                return baseObject.GetEncodingDerImplicit(tagClass, tagNo);

            return new TaggedDerEncoding(tagClass, tagNo, baseObject.GetEncodingDer());
        }

        internal override Asn1Sequence RebuildConstructed(Asn1Object asn1Object)
        {
            return new DerSequence(asn1Object);
        }

        internal override Asn1TaggedObject ReplaceTag(int tagClass, int tagNo)
        {
            return new DerTaggedObject(m_explicitness, tagClass, tagNo, m_object);
        }
    }
}
