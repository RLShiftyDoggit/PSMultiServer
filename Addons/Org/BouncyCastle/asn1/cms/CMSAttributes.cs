using MultiServer.Addons.Org.BouncyCastle.Asn1;
using MultiServer.Addons.Org.BouncyCastle.Asn1.Pkcs;

namespace MultiServer.Addons.Org.BouncyCastle.Asn1.Cms
{
    public abstract class CmsAttributes
    {
        public static readonly DerObjectIdentifier ContentType		= PkcsObjectIdentifiers.Pkcs9AtContentType;
        public static readonly DerObjectIdentifier MessageDigest	= PkcsObjectIdentifiers.Pkcs9AtMessageDigest;
        public static readonly DerObjectIdentifier SigningTime		= PkcsObjectIdentifiers.Pkcs9AtSigningTime;
		public static readonly DerObjectIdentifier CounterSignature = PkcsObjectIdentifiers.Pkcs9AtCounterSignature;
		public static readonly DerObjectIdentifier ContentHint		= PkcsObjectIdentifiers.IdAAContentHint;
        public static readonly DerObjectIdentifier CmsAlgorithmProtect = PkcsObjectIdentifiers.id_aa_cmsAlgorithmProtect;

	}
}
