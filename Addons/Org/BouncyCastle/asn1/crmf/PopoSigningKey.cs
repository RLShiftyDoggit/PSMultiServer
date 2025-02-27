using MultiServer.Addons.Org.BouncyCastle.Asn1.X509;

namespace MultiServer.Addons.Org.BouncyCastle.Asn1.Crmf
{
    public class PopoSigningKey
        : Asn1Encodable
    {
        public static PopoSigningKey GetInstance(object obj)
        {
            if (obj == null)
                return null;
            if (obj is PopoSigningKey popoSigningKey)
                return popoSigningKey;
            return new PopoSigningKey(Asn1Sequence.GetInstance(obj));
        }

        public static PopoSigningKey GetInstance(Asn1TaggedObject obj, bool isExplicit)
        {
            return new PopoSigningKey(Asn1Sequence.GetInstance(obj, isExplicit));
        }

        private readonly PopoSigningKeyInput m_poposkInput;
        private readonly AlgorithmIdentifier m_algorithmIdentifier;
        private readonly DerBitString m_signature;

        private PopoSigningKey(Asn1Sequence seq)
        {
            int index = 0;

            if (seq[index] is Asn1TaggedObject tagObj)
            {
                index++;

                m_poposkInput = PopoSigningKeyInput.GetInstance(
                    Asn1Utilities.GetContextBaseUniversal(tagObj, 0, false, Asn1Tags.Sequence));
            }
            m_algorithmIdentifier = AlgorithmIdentifier.GetInstance(seq[index++]);
            m_signature = DerBitString.GetInstance(seq[index]);
        }

        /**
         * Creates a new Proof of Possession object for a signing key.
         * @param poposkIn the PopoSigningKeyInput structure, or null if the
         *     CertTemplate includes both subject and publicKey values.
         * @param aid the AlgorithmIdentifier used to sign the proof of possession.
         * @param signature a signature over the DER-encoded value of poposkIn,
         *     or the DER-encoded value of certReq if poposkIn is null.
         */
        public PopoSigningKey(PopoSigningKeyInput poposkIn, AlgorithmIdentifier aid, DerBitString signature)
        {
            m_poposkInput = poposkIn;
            m_algorithmIdentifier = aid;
            m_signature = signature;
        }

        public virtual PopoSigningKeyInput PoposkInput => m_poposkInput;

        public virtual AlgorithmIdentifier AlgorithmIdentifier => m_algorithmIdentifier;

        public virtual DerBitString Signature => m_signature;

        /**
         * <pre>
         * PopoSigningKey ::= SEQUENCE {
         *                      poposkInput           [0] PopoSigningKeyInput OPTIONAL,
         *                      algorithmIdentifier   AlgorithmIdentifier,
         *                      signature             BIT STRING }
         *  -- The signature (using "algorithmIdentifier") is on the
         *  -- DER-encoded value of poposkInput.  NOTE: If the CertReqMsg
         *  -- certReq CertTemplate contains the subject and publicKey values,
         *  -- then poposkInput MUST be omitted and the signature MUST be
         *  -- computed on the DER-encoded value of CertReqMsg certReq.  If
         *  -- the CertReqMsg certReq CertTemplate does not contain the public
         *  -- key and subject values, then poposkInput MUST be present and
         *  -- MUST be signed.  This strategy ensures that the public key is
         *  -- not present in both the poposkInput and CertReqMsg certReq
         *  -- CertTemplate fields.
         * </pre>
         * @return a basic ASN.1 object representation.
         */
        public override Asn1Object ToAsn1Object()
        {
            Asn1EncodableVector v = new Asn1EncodableVector(3);
            v.AddOptionalTagged(false, 0, m_poposkInput);
            v.Add(m_algorithmIdentifier);
            v.Add(m_signature);
            return new DerSequence(v);
        }
    }
}
