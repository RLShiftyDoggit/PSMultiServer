using System.IO;

#if NET6_0_OR_GREATER
using System.IO.Compression;
#else
using MultiServer.Addons.Org.BouncyCastle.Utilities.Zlib;
#endif

namespace MultiServer.Addons.Org.BouncyCastle.Utilities.IO.Compression
{
    internal static class Zip
    {
        internal static Stream CompressOutput(Stream stream, int zlibCompressionLevel, bool leaveOpen = false)
        {
#if NET6_0_OR_GREATER
            return new DeflateStream(stream, ZLib.GetCompressionLevel(zlibCompressionLevel), leaveOpen);
#else
            return leaveOpen
                ?   new ZOutputStreamLeaveOpen(stream, zlibCompressionLevel, true)
                :   new ZOutputStream(stream, zlibCompressionLevel, true);
#endif
        }

        internal static Stream DecompressInput(Stream stream, bool leaveOpen = false)
        {
#if NET6_0_OR_GREATER
            return new DeflateStream(stream, CompressionMode.Decompress, leaveOpen);
#else
            return leaveOpen
                ?   new ZInputStreamLeaveOpen(stream, true)
                :   new ZInputStream(stream, true);
#endif
        }
    }
}
