using System;

using MultiServer.Addons.Org.BouncyCastle.Utilities;

namespace MultiServer.Addons.Org.BouncyCastle.Pqc.Crypto.Sike
{
    [Obsolete("Will be removed")]
    public sealed class SikePrivateKeyParameters
        : SikeKeyParameters
    {
        private readonly byte[] privateKey;

        public SikePrivateKeyParameters(SikeParameters param, byte[] privateKey)
            : base(true, param)
        {
            this.privateKey = Arrays.Clone(privateKey);
        }

        public byte[] GetEncoded()
        {
            return Arrays.Clone(privateKey);
        }

        public byte[] GetPrivateKey()
        {
            return Arrays.Clone(privateKey);
        }
    }
}
