using System.IO;

using MultiServer.Addons.Org.BouncyCastle.Crypto.IO;

namespace MultiServer.Addons.Org.BouncyCastle.Crypto.Operators
{
    public sealed class DefaultMacCalculator
        : IStreamCalculator<IBlockResult>
    {
        private readonly MacSink m_macSink;

        public DefaultMacCalculator(IMac mac)
        {
            m_macSink = new MacSink(mac);
        }

        public Stream Stream => m_macSink;

        public IBlockResult GetResult() => new DefaultMacResult(m_macSink.Mac);
    }
}
