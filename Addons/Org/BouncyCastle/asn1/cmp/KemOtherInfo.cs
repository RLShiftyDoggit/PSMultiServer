using System;

using MultiServer.Addons.Org.BouncyCastle.Asn1.X509;

namespace MultiServer.Addons.Org.BouncyCastle.Asn1.Cmp
{
    /*
     * <pre>
     * KemOtherInfo ::= SEQUENCE {
     *   staticString      PKIFreeText,  -- MUST be "CMP-KEM"
     *   transactionID [0] OCTET STRING     OPTIONAL,
     *   senderNonce   [1] OCTET STRING     OPTIONAL,
     *   recipNonce    [2] OCTET STRING     OPTIONAL,
     *   len               INTEGER (1..MAX),
     *   mac               AlgorithmIdentifier{MAC-ALGORITHM, {...}}
     *   ct                OCTET STRING
     * }
     * </pre>
     */
    public class KemOtherInfo
        : Asn1Encodable
    {
        public static KemOtherInfo GetInstance(object obj)
        {
            if (obj == null)
                return null;
            if (obj is KemOtherInfo kemOtherInfo)
                return kemOtherInfo;
            return new KemOtherInfo(Asn1Sequence.GetInstance(obj));
        }

        public static KemOtherInfo GetInstance(Asn1TaggedObject taggedObject, bool declaredExplicit) =>
            new KemOtherInfo(Asn1Sequence.GetInstance(taggedObject, declaredExplicit));

        private static readonly PkiFreeText DEFAULT_staticString = new PkiFreeText("CMP-KEM");

        private readonly PkiFreeText m_staticString;
        private readonly Asn1OctetString m_transactionID;
        private readonly Asn1OctetString m_senderNonce;
        private readonly Asn1OctetString m_recipNonce;
        private readonly DerInteger m_len;
        private readonly AlgorithmIdentifier m_mac;
        private readonly Asn1OctetString m_ct;

        public KemOtherInfo(Asn1OctetString transactionID, Asn1OctetString senderNonce, Asn1OctetString recipNonce,
            DerInteger len, AlgorithmIdentifier mac, Asn1OctetString ct)
        {
            m_staticString = DEFAULT_staticString;
            m_transactionID = transactionID;
            m_senderNonce = senderNonce;
            m_recipNonce = recipNonce;
            m_len = len;
            m_mac = mac;
            m_ct = ct;
        }

        public KemOtherInfo(Asn1OctetString transactionID, Asn1OctetString senderNonce, Asn1OctetString recipNonce,
            long len, AlgorithmIdentifier mac, Asn1OctetString ct)
            : this(transactionID, senderNonce, recipNonce, new DerInteger(len), mac, ct)
        {
        }

        private KemOtherInfo(Asn1Sequence seq)
        {
            if (seq.Count < 4 || seq.Count > 7)
                throw new ArgumentException("sequence size should be between 4 and 7 inclusive", nameof(seq));

            int seqPos = 0;

            m_staticString = PkiFreeText.GetInstance(seq[seqPos]);
            if (!DEFAULT_staticString.Equals(m_staticString))
                throw new ArgumentException("staticString field should be " + DEFAULT_staticString);

            Asn1TaggedObject tagged = seq[++seqPos] as Asn1TaggedObject;

            if (tagged != null &&
                Asn1Utilities.TryGetContextBaseUniversal(tagged, 0, true, Asn1Tags.OctetString, out var transactionID))
            {
                m_transactionID = (Asn1OctetString)transactionID;
                tagged = seq[++seqPos] as Asn1TaggedObject;
            }

            if (tagged != null &&
                Asn1Utilities.TryGetContextBaseUniversal(tagged, 1, true, Asn1Tags.OctetString, out var senderNonce))
            {
                m_senderNonce = (Asn1OctetString)senderNonce;
                tagged = seq[++seqPos] as Asn1TaggedObject;
            }

            if (tagged != null &&
                Asn1Utilities.TryGetContextBaseUniversal(tagged, 2, true, Asn1Tags.OctetString, out var recipNonce))
            {
                m_recipNonce = (Asn1OctetString)recipNonce;
                tagged = seq[++seqPos] as Asn1TaggedObject;
            }

            if (tagged != null)
                throw new ArgumentException("unknown tag: " + Asn1Utilities.GetTagText(tagged));

            m_len = DerInteger.GetInstance(seq[seqPos]);
            m_mac = AlgorithmIdentifier.GetInstance(seq[++seqPos]);
            m_ct = Asn1OctetString.GetInstance(seq[++seqPos]);

            if (++seqPos != seq.Count)
                throw new ArgumentException("unexpected data at end of sequence", nameof(seq));
        }

        public virtual Asn1OctetString TransactionID => m_transactionID;

        public virtual Asn1OctetString SenderNonce => m_senderNonce;

        public virtual Asn1OctetString RecipNonce => m_recipNonce;

        public virtual DerInteger Len => m_len;

        public virtual AlgorithmIdentifier Mac => m_mac;

        public virtual Asn1OctetString Ct => m_ct;

        /**
         * <pre>
         * KemOtherInfo ::= SEQUENCE {
         *   staticString      PKIFreeText,   -- MUST be "CMP-KEM"
         *   transactionID [0] OCTET STRING     OPTIONAL,
         *   senderNonce   [1] OCTET STRING     OPTIONAL,
         *   recipNonce    [2] OCTET STRING     OPTIONAL,
         *   len               INTEGER (1..MAX),
         *   mac               AlgorithmIdentifier{MAC-ALGORITHM, {...}}
         *   ct                OCTET STRING
         * }
         * </pre>
         *
         * @return a basic ASN.1 object representation.
         */
        public override Asn1Object ToAsn1Object()
        {
            Asn1EncodableVector v = new Asn1EncodableVector(7);

            v.Add(m_staticString);
            v.AddOptionalTagged(true, 0, m_transactionID);
            v.AddOptionalTagged(true, 1, m_senderNonce);
            v.AddOptionalTagged(true, 2, m_recipNonce);
            v.Add(m_len);
            v.Add(m_mac);
            v.Add(m_ct);

            return new DerSequence(v);
        }
    }
}
