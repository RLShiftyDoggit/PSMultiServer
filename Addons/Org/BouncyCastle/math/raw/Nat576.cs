using System;
using System.Diagnostics;

using MultiServer.Addons.Org.BouncyCastle.Crypto.Utilities;

namespace MultiServer.Addons.Org.BouncyCastle.Math.Raw
{
    internal static class Nat576
    {
        public static void Copy64(ulong[] x, ulong[] z)
        {
            z[0] = x[0];
            z[1] = x[1];
            z[2] = x[2];
            z[3] = x[3];
            z[4] = x[4];
            z[5] = x[5];
            z[6] = x[6];
            z[7] = x[7];
            z[8] = x[8];
        }

        public static void Copy64(ulong[] x, int xOff, ulong[] z, int zOff)
        {
            z[zOff + 0] = x[xOff + 0];
            z[zOff + 1] = x[xOff + 1];
            z[zOff + 2] = x[xOff + 2];
            z[zOff + 3] = x[xOff + 3];
            z[zOff + 4] = x[xOff + 4];
            z[zOff + 5] = x[xOff + 5];
            z[zOff + 6] = x[xOff + 6];
            z[zOff + 7] = x[xOff + 7];
            z[zOff + 8] = x[xOff + 8];
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public static void Copy64(ReadOnlySpan<ulong> x, Span<ulong> z)
        {
            z[0] = x[0];
            z[1] = x[1];
            z[2] = x[2];
            z[3] = x[3];
            z[4] = x[4];
            z[5] = x[5];
            z[6] = x[6];
            z[7] = x[7];
            z[8] = x[8];
        }
#endif

        public static ulong[] Create64()
        {
            return new ulong[9];
        }

        public static ulong[] CreateExt64()
        {
            return new ulong[18];
        }

        public static bool Eq64(ulong[] x, ulong[] y)
        {
            for (int i = 8; i >= 0; --i)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsOne64(ulong[] x)
        {
            if (x[0] != 1UL)
            {
                return false;
            }
            for (int i = 1; i < 9; ++i)
            {
                if (x[i] != 0UL)
                {
                    return false;
                }
            }
            return true;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public static bool IsZero64(ReadOnlySpan<ulong> x)
#else
        public static bool IsZero64(ulong[] x)
#endif
        {
            for (int i = 0; i < 9; ++i)
            {
                if (x[i] != 0UL)
                {
                    return false;
                }
            }
            return true;
        }

        public static BigInteger ToBigInteger64(ulong[] x)
        {
            byte[] bs = new byte[72];
            for (int i = 0; i < 9; ++i)
            {
                ulong x_i = x[i];
                if (x_i != 0L)
                {
                    Pack.UInt64_To_BE(x_i, bs, (8 - i) << 3);
                }
            }
            return new BigInteger(1, bs);
        }
    }
}
