using System;

using MultiServer.Addons.Org.BouncyCastle.Math;

namespace MultiServer.Addons.Org.BouncyCastle.Crypto
{
    public interface IRsa
    {
        void Init(bool forEncryption, ICipherParameters parameters);
        int GetInputBlockSize();
        int GetOutputBlockSize();
        BigInteger ConvertInput(byte[] buf, int off, int len);
        BigInteger ProcessBlock(BigInteger input);
        byte[] ConvertOutput(BigInteger result);
    }
}
