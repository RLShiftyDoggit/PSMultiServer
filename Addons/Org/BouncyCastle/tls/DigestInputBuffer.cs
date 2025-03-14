using System;
using System.IO;

using MultiServer.Addons.Org.BouncyCastle.Tls.Crypto;

namespace MultiServer.Addons.Org.BouncyCastle.Tls
{
    internal class DigestInputBuffer
        : MemoryStream
    {
        internal void UpdateDigest(TlsHash hash)
        {
            WriteTo(new TlsHashSink(hash));
        }

        /// <exception cref="IOException"/>
        internal void CopyInputTo(Stream output)
        {
            // TODO[tls] Consider defensive copy if 'output' might be external code
            WriteTo(output);
        }
    }
}
