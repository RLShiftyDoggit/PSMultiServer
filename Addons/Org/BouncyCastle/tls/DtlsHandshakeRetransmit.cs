using System;
using System.IO;

namespace MultiServer.Addons.Org.BouncyCastle.Tls
{
    internal interface DtlsHandshakeRetransmit
    {
        /// <exception cref="IOException"/>
        void ReceivedHandshakeRecord(int epoch, byte[] buf, int off, int len);
    }
}
