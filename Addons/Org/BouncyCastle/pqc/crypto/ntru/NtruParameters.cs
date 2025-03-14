using MultiServer.Addons.Org.BouncyCastle.Crypto;
using MultiServer.Addons.Org.BouncyCastle.Pqc.Crypto.Ntru.ParameterSets;

namespace MultiServer.Addons.Org.BouncyCastle.Pqc.Crypto.Ntru
{
    public sealed class NtruParameters
        : IKemParameters
    {
        public static readonly NtruParameters NtruHps2048509 =
            new NtruParameters("ntruhps2048509", new NtruHps2048509());

        public static readonly NtruParameters NtruHps2048677 =
            new NtruParameters("ntruhps2048677", new NtruHps2048677());

        public static readonly NtruParameters NtruHps4096821 =
            new NtruParameters("ntruhps4096821", new NtruHps4096821());

        public static readonly NtruParameters NtruHrss701 = new NtruParameters("ntruhrss701", new NtruHrss701());

        internal readonly NtruParameterSet ParameterSet;

        private readonly string _name;

        private NtruParameters(string name, NtruParameterSet parameterSet)
        {
            _name = name;
            ParameterSet = parameterSet;
        }

        public string Name => _name;

        public int DefaultKeySize => ParameterSet.SharedKeyBytes * 8;
    }
}