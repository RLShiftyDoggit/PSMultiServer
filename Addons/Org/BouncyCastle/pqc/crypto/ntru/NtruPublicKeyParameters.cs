namespace MultiServer.Addons.Org.BouncyCastle.Pqc.Crypto.Ntru
{
    public sealed class NtruPublicKeyParameters
        : NtruKeyParameters
    {
        private byte[] _publicKey;

        public byte[] PublicKey
        {
            get => (byte[])_publicKey.Clone();
            set => _publicKey = (byte[])value.Clone();
        }

        public NtruPublicKeyParameters(NtruParameters parameters, byte[] key) : base(false, parameters)
        {
            PublicKey = key;
        }

        public override byte[] GetEncoded()
        {
            return PublicKey;
        }
    }
}