using System;
using System.Diagnostics;

using MultiServer.Addons.Org.BouncyCastle.Crypto.Utilities;

namespace MultiServer.Addons.Org.BouncyCastle.Math.Raw
{
    internal static class Nat224
    {
        private const ulong M = 0xFFFFFFFFUL;

        public static uint Add(uint[] x, uint[] y, uint[] z)
        {
            ulong c = 0;
            c += (ulong)x[0] + y[0];
            z[0] = (uint)c;
            c >>= 32;
            c += (ulong)x[1] + y[1];
            z[1] = (uint)c;
            c >>= 32;
            c += (ulong)x[2] + y[2];
            z[2] = (uint)c;
            c >>= 32;
            c += (ulong)x[3] + y[3];
            z[3] = (uint)c;
            c >>= 32;
            c += (ulong)x[4] + y[4];
            z[4] = (uint)c;
            c >>= 32;
            c += (ulong)x[5] + y[5];
            z[5] = (uint)c;
            c >>= 32;
            c += (ulong)x[6] + y[6];
            z[6] = (uint)c;
            c >>= 32;
            return (uint)c;
        }

        public static uint Add(uint[] x, int xOff, uint[] y, int yOff, uint[] z, int zOff)
        {
            ulong c = 0;
            c += (ulong)x[xOff + 0] + y[yOff + 0];
            z[zOff + 0] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 1] + y[yOff + 1];
            z[zOff + 1] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 2] + y[yOff + 2];
            z[zOff + 2] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 3] + y[yOff + 3];
            z[zOff + 3] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 4] + y[yOff + 4];
            z[zOff + 4] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 5] + y[yOff + 5];
            z[zOff + 5] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 6] + y[yOff + 6];
            z[zOff + 6] = (uint)c;
            c >>= 32;
            return (uint)c;
        }

        public static uint AddBothTo(uint[] x, uint[] y, uint[] z)
        {
            ulong c = 0;
            c += (ulong)x[0] + y[0] + z[0];
            z[0] = (uint)c;
            c >>= 32;
            c += (ulong)x[1] + y[1] + z[1];
            z[1] = (uint)c;
            c >>= 32;
            c += (ulong)x[2] + y[2] + z[2];
            z[2] = (uint)c;
            c >>= 32;
            c += (ulong)x[3] + y[3] + z[3];
            z[3] = (uint)c;
            c >>= 32;
            c += (ulong)x[4] + y[4] + z[4];
            z[4] = (uint)c;
            c >>= 32;
            c += (ulong)x[5] + y[5] + z[5];
            z[5] = (uint)c;
            c >>= 32;
            c += (ulong)x[6] + y[6] + z[6];
            z[6] = (uint)c;
            c >>= 32;
            return (uint)c;
        }

        public static uint AddBothTo(uint[] x, int xOff, uint[] y, int yOff, uint[] z, int zOff)
        {
            ulong c = 0;
            c += (ulong)x[xOff + 0] + y[yOff + 0] + z[zOff + 0];
            z[zOff + 0] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 1] + y[yOff + 1] + z[zOff + 1];
            z[zOff + 1] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 2] + y[yOff + 2] + z[zOff + 2];
            z[zOff + 2] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 3] + y[yOff + 3] + z[zOff + 3];
            z[zOff + 3] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 4] + y[yOff + 4] + z[zOff + 4];
            z[zOff + 4] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 5] + y[yOff + 5] + z[zOff + 5];
            z[zOff + 5] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 6] + y[yOff + 6] + z[zOff + 6];
            z[zOff + 6] = (uint)c;
            c >>= 32;
            return (uint)c;
        }

        public static uint AddTo(uint[] x, uint[] z, uint cIn)
        {
            ulong c = cIn;
            c += (ulong)x[0] + z[0];
            z[0] = (uint)c;
            c >>= 32;
            c += (ulong)x[1] + z[1];
            z[1] = (uint)c;
            c >>= 32;
            c += (ulong)x[2] + z[2];
            z[2] = (uint)c;
            c >>= 32;
            c += (ulong)x[3] + z[3];
            z[3] = (uint)c;
            c >>= 32;
            c += (ulong)x[4] + z[4];
            z[4] = (uint)c;
            c >>= 32;
            c += (ulong)x[5] + z[5];
            z[5] = (uint)c;
            c >>= 32;
            c += (ulong)x[6] + z[6];
            z[6] = (uint)c;
            c >>= 32;
            return (uint)c;
        }

        public static uint AddTo(uint[] x, int xOff, uint[] z, int zOff, uint cIn)
        {
            ulong c = cIn;
            c += (ulong)x[xOff + 0] + z[zOff + 0];
            z[zOff + 0] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 1] + z[zOff + 1];
            z[zOff + 1] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 2] + z[zOff + 2];
            z[zOff + 2] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 3] + z[zOff + 3];
            z[zOff + 3] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 4] + z[zOff + 4];
            z[zOff + 4] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 5] + z[zOff + 5];
            z[zOff + 5] = (uint)c;
            c >>= 32;
            c += (ulong)x[xOff + 6] + z[zOff + 6];
            z[zOff + 6] = (uint)c;
            c >>= 32;
            return (uint)c;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public static uint AddTo(ReadOnlySpan<uint> x, Span<uint> z, uint cIn)
        {
            ulong c = cIn;
            c += (ulong)x[0] + z[0];
            z[0] = (uint)c;
            c >>= 32;
            c += (ulong)x[1] + z[1];
            z[1] = (uint)c;
            c >>= 32;
            c += (ulong)x[2] + z[2];
            z[2] = (uint)c;
            c >>= 32;
            c += (ulong)x[3] + z[3];
            z[3] = (uint)c;
            c >>= 32;
            c += (ulong)x[4] + z[4];
            z[4] = (uint)c;
            c >>= 32;
            c += (ulong)x[5] + z[5];
            z[5] = (uint)c;
            c >>= 32;
            c += (ulong)x[6] + z[6];
            z[6] = (uint)c;
            c >>= 32;
            return (uint)c;
        }
#endif

        public static uint AddToEachOther(uint[] u, int uOff, uint[] v, int vOff)
        {
            ulong c = 0;
            c += (ulong)u[uOff + 0] + v[vOff + 0];
            u[uOff + 0] = (uint)c;
            v[vOff + 0] = (uint)c;
            c >>= 32;
            c += (ulong)u[uOff + 1] + v[vOff + 1];
            u[uOff + 1] = (uint)c;
            v[vOff + 1] = (uint)c;
            c >>= 32;
            c += (ulong)u[uOff + 2] + v[vOff + 2];
            u[uOff + 2] = (uint)c;
            v[vOff + 2] = (uint)c;
            c >>= 32;
            c += (ulong)u[uOff + 3] + v[vOff + 3];
            u[uOff + 3] = (uint)c;
            v[vOff + 3] = (uint)c;
            c >>= 32;
            c += (ulong)u[uOff + 4] + v[vOff + 4];
            u[uOff + 4] = (uint)c;
            v[vOff + 4] = (uint)c;
            c >>= 32;
            c += (ulong)u[uOff + 5] + v[vOff + 5];
            u[uOff + 5] = (uint)c;
            v[vOff + 5] = (uint)c;
            c >>= 32;
            c += (ulong)u[uOff + 6] + v[vOff + 6];
            u[uOff + 6] = (uint)c;
            v[vOff + 6] = (uint)c;
            c >>= 32;
            return (uint)c;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public static uint AddToEachOther(Span<uint> u, Span<uint> v)
        {
            ulong c = 0;
            c += (ulong)u[0] + v[0];
            u[0] = (uint)c;
            v[0] = (uint)c;
            c >>= 32;
            c += (ulong)u[1] + v[1];
            u[1] = (uint)c;
            v[1] = (uint)c;
            c >>= 32;
            c += (ulong)u[2] + v[2];
            u[2] = (uint)c;
            v[2] = (uint)c;
            c >>= 32;
            c += (ulong)u[3] + v[3];
            u[3] = (uint)c;
            v[3] = (uint)c;
            c >>= 32;
            c += (ulong)u[4] + v[4];
            u[4] = (uint)c;
            v[4] = (uint)c;
            c >>= 32;
            c += (ulong)u[5] + v[5];
            u[5] = (uint)c;
            v[5] = (uint)c;
            c >>= 32;
            c += (ulong)u[6] + v[6];
            u[6] = (uint)c;
            v[6] = (uint)c;
            c >>= 32;
            return (uint)c;
        }
#endif

        public static void Copy(uint[] x, uint[] z)
        {
            z[0] = x[0];
            z[1] = x[1];
            z[2] = x[2];
            z[3] = x[3];
            z[4] = x[4];
            z[5] = x[5];
            z[6] = x[6];
        }

        public static void Copy(uint[] x, int xOff, uint[] z, int zOff)
        {
            z[zOff + 0] = x[xOff + 0];
            z[zOff + 1] = x[xOff + 1];
            z[zOff + 2] = x[xOff + 2];
            z[zOff + 3] = x[xOff + 3];
            z[zOff + 4] = x[xOff + 4];
            z[zOff + 5] = x[xOff + 5];
            z[zOff + 6] = x[xOff + 6];
        }

        public static uint[] Create()
        {
            return new uint[7];
        }

        public static uint[] CreateExt()
        {
            return new uint[14];
        }

        public static bool Diff(uint[] x, int xOff, uint[] y, int yOff, uint[] z, int zOff)
        {
            bool pos = Gte(x, xOff, y, yOff);
            if (pos)
            {
                Sub(x, xOff, y, yOff, z, zOff);
            }
            else
            {
                Sub(y, yOff, x, xOff, z, zOff);
            }
            return pos;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public static bool Diff(ReadOnlySpan<uint> x, ReadOnlySpan<uint> y, Span<uint> z)
        {
            bool pos = Gte(x, y);
            if (pos)
            {
                Sub(x, y, z);
            }
            else
            {
                Sub(y, x, z);
            }
            return pos;
        }
#endif

        public static bool Eq(uint[] x, uint[] y)
        {
            for (int i = 6; i >= 0; --i)
            {
                if (x[i] != y[i])
                    return false;
            }
            return true;
        }

        public static uint GetBit(uint[] x, int bit)
        {
            if (bit == 0)
            {
                return x[0] & 1;
            }
            int w = bit >> 5;
            if (w < 0 || w >= 7)
            {
                return 0;
            }
            int b = bit & 31;
            return (x[w] >> b) & 1;
        }

        public static bool Gte(uint[] x, uint[] y)
        {
            for (int i = 6; i >= 0; --i)
            {
                uint x_i = x[i], y_i = y[i];
                if (x_i < y_i)
                    return false;
                if (x_i > y_i)
                    return true;
            }
            return true;
        }

        public static bool Gte(uint[] x, int xOff, uint[] y, int yOff)
        {
            for (int i = 6; i >= 0; --i)
            {
                uint x_i = x[xOff + i], y_i = y[yOff + i];
                if (x_i < y_i)
                    return false;
                if (x_i > y_i)
                    return true;
            }
            return true;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public static bool Gte(ReadOnlySpan<uint> x, ReadOnlySpan<uint> y)
        {
            for (int i = 6; i >= 0; --i)
            {
                uint x_i = x[i], y_i = y[i];
                if (x_i < y_i)
                    return false;
                if (x_i > y_i)
                    return true;
            }
            return true;
        }
#endif

        public static bool IsOne(uint[] x)
        {
            if (x[0] != 1)
            {
                return false;
            }
            for (int i = 1; i < 7; ++i)
            {
                if (x[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsZero(uint[] x)
        {
            for (int i = 0; i < 7; ++i)
            {
                if (x[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }

        public static void Mul(uint[] x, uint[] y, uint[] zz)
        {
            ulong y_0 = y[0];
            ulong y_1 = y[1];
            ulong y_2 = y[2];
            ulong y_3 = y[3];
            ulong y_4 = y[4];
            ulong y_5 = y[5];
            ulong y_6 = y[6];

            {
                ulong c = 0, x_0 = x[0];
                c += x_0 * y_0;
                zz[0] = (uint)c;
                c >>= 32;
                c += x_0 * y_1;
                zz[1] = (uint)c;
                c >>= 32;
                c += x_0 * y_2;
                zz[2] = (uint)c;
                c >>= 32;
                c += x_0 * y_3;
                zz[3] = (uint)c;
                c >>= 32;
                c += x_0 * y_4;
                zz[4] = (uint)c;
                c >>= 32;
                c += x_0 * y_5;
                zz[5] = (uint)c;
                c >>= 32;
                c += x_0 * y_6;
                zz[6] = (uint)c;
                c >>= 32;
                zz[7] = (uint)c;
            }

            for (int i = 1; i < 7; ++i)
            {
                ulong c = 0, x_i = x[i];
                c += x_i * y_0 + zz[i + 0];
                zz[i + 0] = (uint)c;
                c >>= 32;
                c += x_i * y_1 + zz[i + 1];
                zz[i + 1] = (uint)c;
                c >>= 32;
                c += x_i * y_2 + zz[i + 2];
                zz[i + 2] = (uint)c;
                c >>= 32;
                c += x_i * y_3 + zz[i + 3];
                zz[i + 3] = (uint)c;
                c >>= 32;
                c += x_i * y_4 + zz[i + 4];
                zz[i + 4] = (uint)c;
                c >>= 32;
                c += x_i * y_5 + zz[i + 5];
                zz[i + 5] = (uint)c;
                c >>= 32;
                c += x_i * y_6 + zz[i + 6];
                zz[i + 6] = (uint)c;
                c >>= 32;
                zz[i + 7] = (uint)c;
            }
        }

        public static void Mul(uint[] x, int xOff, uint[] y, int yOff, uint[] zz, int zzOff)
        {
            ulong y_0 = y[yOff + 0];
            ulong y_1 = y[yOff + 1];
            ulong y_2 = y[yOff + 2];
            ulong y_3 = y[yOff + 3];
            ulong y_4 = y[yOff + 4];
            ulong y_5 = y[yOff + 5];
            ulong y_6 = y[yOff + 6];

            {
                ulong c = 0, x_0 = x[xOff + 0];
                c += x_0 * y_0;
                zz[zzOff + 0] = (uint)c;
                c >>= 32;
                c += x_0 * y_1;
                zz[zzOff + 1] = (uint)c;
                c >>= 32;
                c += x_0 * y_2;
                zz[zzOff + 2] = (uint)c;
                c >>= 32;
                c += x_0 * y_3;
                zz[zzOff + 3] = (uint)c;
                c >>= 32;
                c += x_0 * y_4;
                zz[zzOff + 4] = (uint)c;
                c >>= 32;
                c += x_0 * y_5;
                zz[zzOff + 5] = (uint)c;
                c >>= 32;
                c += x_0 * y_6;
                zz[zzOff + 6] = (uint)c;
                c >>= 32;
                zz[zzOff + 7] = (uint)c;
            }

            for (int i = 1; i < 7; ++i)
            {
                ++zzOff;
                ulong c = 0, x_i = x[xOff + i];
                c += x_i * y_0 + zz[zzOff + 0];
                zz[zzOff + 0] = (uint)c;
                c >>= 32;
                c += x_i * y_1 + zz[zzOff + 1];
                zz[zzOff + 1] = (uint)c;
                c >>= 32;
                c += x_i * y_2 + zz[zzOff + 2];
                zz[zzOff + 2] = (uint)c;
                c >>= 32;
                c += x_i * y_3 + zz[zzOff + 3];
                zz[zzOff + 3] = (uint)c;
                c >>= 32;
                c += x_i * y_4 + zz[zzOff + 4];
                zz[zzOff + 4] = (uint)c;
                c >>= 32;
                c += x_i * y_5 + zz[zzOff + 5];
                zz[zzOff + 5] = (uint)c;
                c >>= 32;
                c += x_i * y_6 + zz[zzOff + 6];
                zz[zzOff + 6] = (uint)c;
                c >>= 32;
                zz[zzOff + 7] = (uint)c;
            }
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public static void Mul(ReadOnlySpan<uint> x, ReadOnlySpan<uint> y, Span<uint> zz)
        {
            ulong y_0 = y[0];
            ulong y_1 = y[1];
            ulong y_2 = y[2];
            ulong y_3 = y[3];
            ulong y_4 = y[4];
            ulong y_5 = y[5];
            ulong y_6 = y[6];

            {
                ulong c = 0, x_0 = x[0];
                c += x_0 * y_0;
                zz[0] = (uint)c;
                c >>= 32;
                c += x_0 * y_1;
                zz[1] = (uint)c;
                c >>= 32;
                c += x_0 * y_2;
                zz[2] = (uint)c;
                c >>= 32;
                c += x_0 * y_3;
                zz[3] = (uint)c;
                c >>= 32;
                c += x_0 * y_4;
                zz[4] = (uint)c;
                c >>= 32;
                c += x_0 * y_5;
                zz[5] = (uint)c;
                c >>= 32;
                c += x_0 * y_6;
                zz[6] = (uint)c;
                c >>= 32;
                zz[7] = (uint)c;
            }

            for (int i = 1; i < 7; ++i)
            {
                ulong c = 0, x_i = x[i];
                c += x_i * y_0 + zz[i + 0];
                zz[i + 0] = (uint)c;
                c >>= 32;
                c += x_i * y_1 + zz[i + 1];
                zz[i + 1] = (uint)c;
                c >>= 32;
                c += x_i * y_2 + zz[i + 2];
                zz[i + 2] = (uint)c;
                c >>= 32;
                c += x_i * y_3 + zz[i + 3];
                zz[i + 3] = (uint)c;
                c >>= 32;
                c += x_i * y_4 + zz[i + 4];
                zz[i + 4] = (uint)c;
                c >>= 32;
                c += x_i * y_5 + zz[i + 5];
                zz[i + 5] = (uint)c;
                c >>= 32;
                c += x_i * y_6 + zz[i + 6];
                zz[i + 6] = (uint)c;
                c >>= 32;
                zz[i + 7] = (uint)c;
            }
        }
#endif

        public static uint MulAddTo(uint[] x, uint[] y, uint[] zz)
        {
            ulong y_0 = y[0];
            ulong y_1 = y[1];
            ulong y_2 = y[2];
            ulong y_3 = y[3];
            ulong y_4 = y[4];
            ulong y_5 = y[5];
            ulong y_6 = y[6];

            ulong zc = 0;
            for (int i = 0; i < 7; ++i)
            {
                ulong c = 0, x_i = x[i];
                c += x_i * y_0 + zz[i + 0];
                zz[i + 0] = (uint)c;
                c >>= 32;
                c += x_i * y_1 + zz[i + 1];
                zz[i + 1] = (uint)c;
                c >>= 32;
                c += x_i * y_2 + zz[i + 2];
                zz[i + 2] = (uint)c;
                c >>= 32;
                c += x_i * y_3 + zz[i + 3];
                zz[i + 3] = (uint)c;
                c >>= 32;
                c += x_i * y_4 + zz[i + 4];
                zz[i + 4] = (uint)c;
                c >>= 32;
                c += x_i * y_5 + zz[i + 5];
                zz[i + 5] = (uint)c;
                c >>= 32;
                c += x_i * y_6 + zz[i + 6];
                zz[i + 6] = (uint)c;
                c >>= 32;

                zc += c + zz[i + 7];
                zz[i + 7] = (uint)zc;
                zc >>= 32;
            }
            return (uint)zc;
        }

        public static uint MulAddTo(uint[] x, int xOff, uint[] y, int yOff, uint[] zz, int zzOff)
        {
            ulong y_0 = y[yOff + 0];
            ulong y_1 = y[yOff + 1];
            ulong y_2 = y[yOff + 2];
            ulong y_3 = y[yOff + 3];
            ulong y_4 = y[yOff + 4];
            ulong y_5 = y[yOff + 5];
            ulong y_6 = y[yOff + 6];

            ulong zc = 0;
            for (int i = 0; i < 7; ++i)
            {
                ulong c = 0, x_i = x[xOff + i];
                c += x_i * y_0 + zz[zzOff + 0];
                zz[zzOff + 0] = (uint)c;
                c >>= 32;
                c += x_i * y_1 + zz[zzOff + 1];
                zz[zzOff + 1] = (uint)c;
                c >>= 32;
                c += x_i * y_2 + zz[zzOff + 2];
                zz[zzOff + 2] = (uint)c;
                c >>= 32;
                c += x_i * y_3 + zz[zzOff + 3];
                zz[zzOff + 3] = (uint)c;
                c >>= 32;
                c += x_i * y_4 + zz[zzOff + 4];
                zz[zzOff + 4] = (uint)c;
                c >>= 32;
                c += x_i * y_5 + zz[zzOff + 5];
                zz[zzOff + 5] = (uint)c;
                c >>= 32;
                c += x_i * y_6 + zz[zzOff + 6];
                zz[zzOff + 6] = (uint)c;
                c >>= 32;

                zc += c + zz[zzOff + 7];
                zz[zzOff + 7] = (uint)zc;
                zc >>= 32;
                ++zzOff;
            }
            return (uint)zc;
        }

        public static ulong Mul33Add(uint w, uint[] x, int xOff, uint[] y, int yOff, uint[] z, int zOff)
        {
            Debug.Assert(w >> 31 == 0);

            ulong c = 0, wVal = w;
            ulong x0 = x[xOff + 0];
            c += wVal * x0 + y[yOff + 0];
            z[zOff + 0] = (uint)c;
            c >>= 32;
            ulong x1 = x[xOff + 1];
            c += wVal * x1 + x0 + y[yOff + 1];
            z[zOff + 1] = (uint)c;
            c >>= 32;
            ulong x2 = x[xOff + 2];
            c += wVal * x2 + x1 + y[yOff + 2];
            z[zOff + 2] = (uint)c;
            c >>= 32;
            ulong x3 = x[xOff + 3];
            c += wVal * x3 + x2 + y[yOff + 3];
            z[zOff + 3] = (uint)c;
            c >>= 32;
            ulong x4 = x[xOff + 4];
            c += wVal * x4 + x3 + y[yOff + 4];
            z[zOff + 4] = (uint)c;
            c >>= 32;
            ulong x5 = x[xOff + 5];
            c += wVal * x5 + x4 + y[yOff + 5];
            z[zOff + 5] = (uint)c;
            c >>= 32;
            ulong x6 = x[xOff + 6];
            c += wVal * x6 + x5 + y[yOff + 6];
            z[zOff + 6] = (uint)c;
            c >>= 32;
            c += x6;
            return c;
        }

        public static uint MulByWord(uint x, uint[] z)
        {
            ulong c = 0, xVal = x;
            c += xVal * (ulong)z[0];
            z[0] = (uint)c;
            c >>= 32;
            c += xVal * (ulong)z[1];
            z[1] = (uint)c;
            c >>= 32;
            c += xVal * (ulong)z[2];
            z[2] = (uint)c;
            c >>= 32;
            c += xVal * (ulong)z[3];
            z[3] = (uint)c;
            c >>= 32;
            c += xVal * (ulong)z[4];
            z[4] = (uint)c;
            c >>= 32;
            c += xVal * (ulong)z[5];
            z[5] = (uint)c;
            c >>= 32;
            c += xVal * (ulong)z[6];
            z[6] = (uint)c;
            c >>= 32;
            return (uint)c;
        }

        public static uint MulByWordAddTo(uint x, uint[] y, uint[] z)
        {
            ulong c = 0, xVal = x;
            c += xVal * (ulong)z[0] + y[0];
            z[0] = (uint)c;
            c >>= 32;
            c += xVal * (ulong)z[1] + y[1];
            z[1] = (uint)c;
            c >>= 32;
            c += xVal * (ulong)z[2] + y[2];
            z[2] = (uint)c;
            c >>= 32;
            c += xVal * (ulong)z[3] + y[3];
            z[3] = (uint)c;
            c >>= 32;
            c += xVal * (ulong)z[4] + y[4];
            z[4] = (uint)c;
            c >>= 32;
            c += xVal * (ulong)z[5] + y[5];
            z[5] = (uint)c;
            c >>= 32;
            c += xVal * (ulong)z[6] + y[6];
            z[6] = (uint)c;
            c >>= 32;
            return (uint)c;
        }

        public static uint MulWordAddTo(uint x, uint[] y, int yOff, uint[] z, int zOff)
        {
            ulong c = 0, xVal = x;
            c += xVal * y[yOff + 0] + z[zOff + 0];
            z[zOff + 0] = (uint)c;
            c >>= 32;
            c += xVal * y[yOff + 1] + z[zOff + 1];
            z[zOff + 1] = (uint)c;
            c >>= 32;
            c += xVal * y[yOff + 2] + z[zOff + 2];
            z[zOff + 2] = (uint)c;
            c >>= 32;
            c += xVal * y[yOff + 3] + z[zOff + 3];
            z[zOff + 3] = (uint)c;
            c >>= 32;
            c += xVal * y[yOff + 4] + z[zOff + 4];
            z[zOff + 4] = (uint)c;
            c >>= 32;
            c += xVal * y[yOff + 5] + z[zOff + 5];
            z[zOff + 5] = (uint)c;
            c >>= 32;
            c += xVal * y[yOff + 6] + z[zOff + 6];
            z[zOff + 6] = (uint)c;
            c >>= 32;
            return (uint)c;
        }

        public static uint Mul33DWordAdd(uint x, ulong y, uint[] z, int zOff)
        {
            Debug.Assert(x >> 31 == 0);
            Debug.Assert(zOff <= 3);
            ulong c = 0, xVal = x;
            ulong y00 = y & M;
            c += xVal * y00 + z[zOff + 0];
            z[zOff + 0] = (uint)c;
            c >>= 32;
            ulong y01 = y >> 32;
            c += xVal * y01 + y00 + z[zOff + 1];
            z[zOff + 1] = (uint)c;
            c >>= 32;
            c += y01 + z[zOff + 2];
            z[zOff + 2] = (uint)c;
            c >>= 32;
            c += z[zOff + 3];
            z[zOff + 3] = (uint)c;
            c >>= 32;
            return c == 0 ? 0 : Nat.IncAt(7, z, zOff, 4);
        }

        public static uint Mul33WordAdd(uint x, uint y, uint[] z, int zOff)
        {
            Debug.Assert(x >> 31 == 0);
            Debug.Assert(zOff <= 4);
            ulong c = 0, yVal = y;
            c += yVal * x + z[zOff + 0];
            z[zOff + 0] = (uint)c;
            c >>= 32;
            c += yVal + z[zOff + 1];
            z[zOff + 1] = (uint)c;
            c >>= 32;
            c += z[zOff + 2];
            z[zOff + 2] = (uint)c;
            c >>= 32;
            return c == 0 ? 0 : Nat.IncAt(7, z, zOff, 3);
        }

        public static uint MulWordDwordAdd(uint x, ulong y, uint[] z, int zOff)
        {
            Debug.Assert(zOff <= 4);
            ulong c = 0, xVal = x;
            c += xVal * y + z[zOff + 0];
            z[zOff + 0] = (uint)c;
            c >>= 32;
            c += xVal * (y >> 32) + z[zOff + 1];
            z[zOff + 1] = (uint)c;
            c >>= 32;
            c += z[zOff + 2];
            z[zOff + 2] = (uint)c;
            c >>= 32;
            return c == 0 ? 0 : Nat.IncAt(7, z, zOff, 3);
        }

        public static uint MulWord(uint x, uint[] y, uint[] z, int zOff)
        {
            ulong c = 0, xVal = x;
            int i = 0;
            do
            {
                c += xVal * y[i];
                z[zOff + i] = (uint)c;
                c >>= 32;
            }
            while (++i < 7);
            return (uint)c;
        }

        public static void Square(uint[] x, uint[] zz)
        {
            ulong x_0 = x[0];
            ulong zz_1;

            uint c = 0, w;
            {
                int i = 6, j = 14;
                do
                {
                    ulong xVal = x[i--];
                    ulong p = xVal * xVal;
                    zz[--j] = (c << 31) | (uint)(p >> 33);
                    zz[--j] = (uint)(p >> 1);
                    c = (uint)p;
                }
                while (i > 0);

                {
                    ulong p = x_0 * x_0;
                    zz_1 = (ulong)(c << 31) | (p >> 33);
                    zz[0] = (uint)p;
                    c = (uint)(p >> 32) & 1;
                }
            }

            ulong x_1 = x[1];
            ulong zz_2 = zz[2];

            {
                zz_1 += x_1 * x_0;
                w = (uint)zz_1;
                zz[1] = (w << 1) | c;
                c = w >> 31;
                zz_2 += zz_1 >> 32;
            }

            ulong x_2 = x[2];
            ulong zz_3 = zz[3];
            ulong zz_4 = zz[4];
            {
                zz_2 += x_2 * x_0;
                w = (uint)zz_2;
                zz[2] = (w << 1) | c;
                c = w >> 31;
                zz_3 += (zz_2 >> 32) + x_2 * x_1;
                zz_4 += zz_3 >> 32;
                zz_3 &= M;
            }

            ulong x_3 = x[3];
            ulong zz_5 = zz[5] + (zz_4 >> 32); zz_4 &= M;
            ulong zz_6 = zz[6] + (zz_5 >> 32); zz_5 &= M;
            {
                zz_3 += x_3 * x_0;
                w = (uint)zz_3;
                zz[3] = (w << 1) | c;
                c = w >> 31;
                zz_4 += (zz_3 >> 32) + x_3 * x_1;
                zz_5 += (zz_4 >> 32) + x_3 * x_2;
                zz_4 &= M;
                zz_6 += zz_5 >> 32;
                zz_5 &= M;
            }

            ulong x_4 = x[4];
            ulong zz_7 = zz[7] + (zz_6 >> 32); zz_6 &= M;
            ulong zz_8 = zz[8] + (zz_7 >> 32); zz_7 &= M;
            {
                zz_4 += x_4 * x_0;
                w = (uint)zz_4;
                zz[4] = (w << 1) | c;
                c = w >> 31;
                zz_5 += (zz_4 >> 32) + x_4 * x_1;
                zz_6 += (zz_5 >> 32) + x_4 * x_2;
                zz_5 &= M;
                zz_7 += (zz_6 >> 32) + x_4 * x_3;
                zz_6 &= M;
                zz_8 += zz_7 >> 32;
                zz_7 &= M;
            }

            ulong x_5 = x[5];
            ulong zz_9 = zz[9] + (zz_8 >> 32); zz_8 &= M;
            ulong zz_10 = zz[10] + (zz_9 >> 32); zz_9 &= M;
            {
                zz_5 += x_5 * x_0;
                w = (uint)zz_5;
                zz[5] = (w << 1) | c;
                c = w >> 31;
                zz_6 += (zz_5 >> 32) + x_5 * x_1;
                zz_7 += (zz_6 >> 32) + x_5 * x_2;
                zz_6 &= M;
                zz_8 += (zz_7 >> 32) + x_5 * x_3;
                zz_7 &= M;
                zz_9 += (zz_8 >> 32) + x_5 * x_4;
                zz_8 &= M;
                zz_10 += zz_9 >> 32;
                zz_9 &= M;
            }

            ulong x_6 = x[6];
            ulong zz_11 = zz[11] + (zz_10 >> 32); zz_10 &= M;
            ulong zz_12 = zz[12] + (zz_11 >> 32); zz_11 &= M;
            {
                zz_6 += x_6 * x_0;
                w = (uint)zz_6;
                zz[6] = (w << 1) | c;
                c = w >> 31;
                zz_7 += (zz_6 >> 32) + x_6 * x_1;
                zz_8 += (zz_7 >> 32) + x_6 * x_2;
                zz_9 += (zz_8 >> 32) + x_6 * x_3;
                zz_10 += (zz_9 >> 32) + x_6 * x_4;
                zz_11 += (zz_10 >> 32) + x_6 * x_5;
                zz_12 += zz_11 >> 32;
            }

            w = (uint)zz_7;
            zz[7] = (w << 1) | c;
            c = w >> 31;
            w = (uint)zz_8;
            zz[8] = (w << 1) | c;
            c = w >> 31;
            w = (uint)zz_9;
            zz[9] = (w << 1) | c;
            c = w >> 31;
            w = (uint)zz_10;
            zz[10] = (w << 1) | c;
            c = w >> 31;
            w = (uint)zz_11;
            zz[11] = (w << 1) | c;
            c = w >> 31;
            w = (uint)zz_12;
            zz[12] = (w << 1) | c;
            c = w >> 31;
            w = zz[13] + (uint)(zz_12 >> 32);
            zz[13] = (w << 1) | c;
        }

        public static void Square(uint[] x, int xOff, uint[] zz, int zzOff)
        {
            ulong x_0 = x[xOff + 0];
            ulong zz_1;

            uint c = 0, w;
            {
                int i = 6, j = 14;
                do
                {
                    ulong xVal = x[xOff + i--];
                    ulong p = xVal * xVal;
                    zz[zzOff + --j] = (c << 31) | (uint)(p >> 33);
                    zz[zzOff + --j] = (uint)(p >> 1);
                    c = (uint)p;
                }
                while (i > 0);

                {
                    ulong p = x_0 * x_0;
                    zz_1 = (ulong)(c << 31) | (p >> 33);
                    zz[zzOff + 0] = (uint)p;
                    c = (uint)(p >> 32) & 1;
                }
            }

            ulong x_1 = x[xOff + 1];
            ulong zz_2 = zz[zzOff + 2];

            {
                zz_1 += x_1 * x_0;
                w = (uint)zz_1;
                zz[zzOff + 1] = (w << 1) | c;
                c = w >> 31;
                zz_2 += zz_1 >> 32;
            }

            ulong x_2 = x[xOff + 2];
            ulong zz_3 = zz[zzOff + 3];
            ulong zz_4 = zz[zzOff + 4];
            {
                zz_2 += x_2 * x_0;
                w = (uint)zz_2;
                zz[zzOff + 2] = (w << 1) | c;
                c = w >> 31;
                zz_3 += (zz_2 >> 32) + x_2 * x_1;
                zz_4 += zz_3 >> 32;
                zz_3 &= M;
            }

            ulong x_3 = x[xOff + 3];
            ulong zz_5 = zz[zzOff + 5] + (zz_4 >> 32); zz_4 &= M;
            ulong zz_6 = zz[zzOff + 6] + (zz_5 >> 32); zz_5 &= M;
            {
                zz_3 += x_3 * x_0;
                w = (uint)zz_3;
                zz[zzOff + 3] = (w << 1) | c;
                c = w >> 31;
                zz_4 += (zz_3 >> 32) + x_3 * x_1;
                zz_5 += (zz_4 >> 32) + x_3 * x_2;
                zz_4 &= M;
                zz_6 += zz_5 >> 32;
                zz_5 &= M;
            }

            ulong x_4 = x[xOff + 4];
            ulong zz_7 = zz[zzOff + 7] + (zz_6 >> 32); zz_6 &= M;
            ulong zz_8 = zz[zzOff + 8] + (zz_7 >> 32); zz_7 &= M;
            {
                zz_4 += x_4 * x_0;
                w = (uint)zz_4;
                zz[zzOff + 4] = (w << 1) | c;
                c = w >> 31;
                zz_5 += (zz_4 >> 32) + x_4 * x_1;
                zz_6 += (zz_5 >> 32) + x_4 * x_2;
                zz_5 &= M;
                zz_7 += (zz_6 >> 32) + x_4 * x_3;
                zz_6 &= M;
                zz_8 += zz_7 >> 32;
                zz_7 &= M;
            }

            ulong x_5 = x[xOff + 5];
            ulong zz_9 = zz[zzOff + 9] + (zz_8 >> 32); zz_8 &= M;
            ulong zz_10 = zz[zzOff + 10] + (zz_9 >> 32); zz_9 &= M;
            {
                zz_5 += x_5 * x_0;
                w = (uint)zz_5;
                zz[zzOff + 5] = (w << 1) | c;
                c = w >> 31;
                zz_6 += (zz_5 >> 32) + x_5 * x_1;
                zz_7 += (zz_6 >> 32) + x_5 * x_2;
                zz_6 &= M;
                zz_8 += (zz_7 >> 32) + x_5 * x_3;
                zz_7 &= M;
                zz_9 += (zz_8 >> 32) + x_5 * x_4;
                zz_8 &= M;
                zz_10 += zz_9 >> 32;
                zz_9 &= M;
            }

            ulong x_6 = x[xOff + 6];
            ulong zz_11 = zz[zzOff + 11] + (zz_10 >> 32); zz_10 &= M;
            ulong zz_12 = zz[zzOff + 12] + (zz_11 >> 32); zz_11 &= M;
            {
                zz_6 += x_6 * x_0;
                w = (uint)zz_6;
                zz[zzOff + 6] = (w << 1) | c;
                c = w >> 31;
                zz_7 += (zz_6 >> 32) + x_6 * x_1;
                zz_8 += (zz_7 >> 32) + x_6 * x_2;
                zz_9 += (zz_8 >> 32) + x_6 * x_3;
                zz_10 += (zz_9 >> 32) + x_6 * x_4;
                zz_11 += (zz_10 >> 32) + x_6 * x_5;
                zz_12 += zz_11 >> 32;
            }

            w = (uint)zz_7;
            zz[zzOff + 7] = (w << 1) | c;
            c = w >> 31;
            w = (uint)zz_8;
            zz[zzOff + 8] = (w << 1) | c;
            c = w >> 31;
            w = (uint)zz_9;
            zz[zzOff + 9] = (w << 1) | c;
            c = w >> 31;
            w = (uint)zz_10;
            zz[zzOff + 10] = (w << 1) | c;
            c = w >> 31;
            w = (uint)zz_11;
            zz[zzOff + 11] = (w << 1) | c;
            c = w >> 31;
            w = (uint)zz_12;
            zz[zzOff + 12] = (w << 1) | c;
            c = w >> 31;
            w = zz[zzOff + 13] + (uint)(zz_12 >> 32);
            zz[zzOff + 13] = (w << 1) | c;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public static void Square(ReadOnlySpan<uint> x, Span<uint> zz)
        {
            ulong x_0 = x[0];
            ulong zz_1;

            uint c = 0, w;
            {
                int i = 6, j = 14;
                do
                {
                    ulong xVal = x[i--];
                    ulong p = xVal * xVal;
                    zz[--j] = (c << 31) | (uint)(p >> 33);
                    zz[--j] = (uint)(p >> 1);
                    c = (uint)p;
                }
                while (i > 0);

                {
                    ulong p = x_0 * x_0;
                    zz_1 = (ulong)(c << 31) | (p >> 33);
                    zz[0] = (uint)p;
                    c = (uint)(p >> 32) & 1;
                }
            }

            ulong x_1 = x[1];
            ulong zz_2 = zz[2];

            {
                zz_1 += x_1 * x_0;
                w = (uint)zz_1;
                zz[1] = (w << 1) | c;
                c = w >> 31;
                zz_2 += zz_1 >> 32;
            }

            ulong x_2 = x[2];
            ulong zz_3 = zz[3];
            ulong zz_4 = zz[4];
            {
                zz_2 += x_2 * x_0;
                w = (uint)zz_2;
                zz[2] = (w << 1) | c;
                c = w >> 31;
                zz_3 += (zz_2 >> 32) + x_2 * x_1;
                zz_4 += zz_3 >> 32;
                zz_3 &= M;
            }

            ulong x_3 = x[3];
            ulong zz_5 = zz[5] + (zz_4 >> 32); zz_4 &= M;
            ulong zz_6 = zz[6] + (zz_5 >> 32); zz_5 &= M;
            {
                zz_3 += x_3 * x_0;
                w = (uint)zz_3;
                zz[3] = (w << 1) | c;
                c = w >> 31;
                zz_4 += (zz_3 >> 32) + x_3 * x_1;
                zz_5 += (zz_4 >> 32) + x_3 * x_2;
                zz_4 &= M;
                zz_6 += zz_5 >> 32;
                zz_5 &= M;
            }

            ulong x_4 = x[4];
            ulong zz_7 = zz[7] + (zz_6 >> 32); zz_6 &= M;
            ulong zz_8 = zz[8] + (zz_7 >> 32); zz_7 &= M;
            {
                zz_4 += x_4 * x_0;
                w = (uint)zz_4;
                zz[4] = (w << 1) | c;
                c = w >> 31;
                zz_5 += (zz_4 >> 32) + x_4 * x_1;
                zz_6 += (zz_5 >> 32) + x_4 * x_2;
                zz_5 &= M;
                zz_7 += (zz_6 >> 32) + x_4 * x_3;
                zz_6 &= M;
                zz_8 += zz_7 >> 32;
                zz_7 &= M;
            }

            ulong x_5 = x[5];
            ulong zz_9 = zz[9] + (zz_8 >> 32); zz_8 &= M;
            ulong zz_10 = zz[10] + (zz_9 >> 32); zz_9 &= M;
            {
                zz_5 += x_5 * x_0;
                w = (uint)zz_5;
                zz[5] = (w << 1) | c;
                c = w >> 31;
                zz_6 += (zz_5 >> 32) + x_5 * x_1;
                zz_7 += (zz_6 >> 32) + x_5 * x_2;
                zz_6 &= M;
                zz_8 += (zz_7 >> 32) + x_5 * x_3;
                zz_7 &= M;
                zz_9 += (zz_8 >> 32) + x_5 * x_4;
                zz_8 &= M;
                zz_10 += zz_9 >> 32;
                zz_9 &= M;
            }

            ulong x_6 = x[6];
            ulong zz_11 = zz[11] + (zz_10 >> 32); zz_10 &= M;
            ulong zz_12 = zz[12] + (zz_11 >> 32); zz_11 &= M;
            {
                zz_6 += x_6 * x_0;
                w = (uint)zz_6;
                zz[6] = (w << 1) | c;
                c = w >> 31;
                zz_7 += (zz_6 >> 32) + x_6 * x_1;
                zz_8 += (zz_7 >> 32) + x_6 * x_2;
                zz_9 += (zz_8 >> 32) + x_6 * x_3;
                zz_10 += (zz_9 >> 32) + x_6 * x_4;
                zz_11 += (zz_10 >> 32) + x_6 * x_5;
                zz_12 += zz_11 >> 32;
            }

            w = (uint)zz_7;
            zz[7] = (w << 1) | c;
            c = w >> 31;
            w = (uint)zz_8;
            zz[8] = (w << 1) | c;
            c = w >> 31;
            w = (uint)zz_9;
            zz[9] = (w << 1) | c;
            c = w >> 31;
            w = (uint)zz_10;
            zz[10] = (w << 1) | c;
            c = w >> 31;
            w = (uint)zz_11;
            zz[11] = (w << 1) | c;
            c = w >> 31;
            w = (uint)zz_12;
            zz[12] = (w << 1) | c;
            c = w >> 31;
            w = zz[13] + (uint)(zz_12 >> 32);
            zz[13] = (w << 1) | c;
        }
#endif

        public static int Sub(uint[] x, uint[] y, uint[] z)
        {
            long c = 0;
            c += (long)x[0] - y[0];
            z[0] = (uint)c;
            c >>= 32;
            c += (long)x[1] - y[1];
            z[1] = (uint)c;
            c >>= 32;
            c += (long)x[2] - y[2];
            z[2] = (uint)c;
            c >>= 32;
            c += (long)x[3] - y[3];
            z[3] = (uint)c;
            c >>= 32;
            c += (long)x[4] - y[4];
            z[4] = (uint)c;
            c >>= 32;
            c += (long)x[5] - y[5];
            z[5] = (uint)c;
            c >>= 32;
            c += (long)x[6] - y[6];
            z[6] = (uint)c;
            c >>= 32;
            return (int)c;
        }

        public static int Sub(uint[] x, int xOff, uint[] y, int yOff, uint[] z, int zOff)
        {
            long c = 0;
            c += (long)x[xOff + 0] - y[yOff + 0];
            z[zOff + 0] = (uint)c;
            c >>= 32;
            c += (long)x[xOff + 1] - y[yOff + 1];
            z[zOff + 1] = (uint)c;
            c >>= 32;
            c += (long)x[xOff + 2] - y[yOff + 2];
            z[zOff + 2] = (uint)c;
            c >>= 32;
            c += (long)x[xOff + 3] - y[yOff + 3];
            z[zOff + 3] = (uint)c;
            c >>= 32;
            c += (long)x[xOff + 4] - y[yOff + 4];
            z[zOff + 4] = (uint)c;
            c >>= 32;
            c += (long)x[xOff + 5] - y[yOff + 5];
            z[zOff + 5] = (uint)c;
            c >>= 32;
            c += (long)x[xOff + 6] - y[yOff + 6];
            z[zOff + 6] = (uint)c;
            c >>= 32;
            return (int)c;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public static int Sub(ReadOnlySpan<uint> x, ReadOnlySpan<uint> y, Span<uint> z)
        {
            long c = 0;
            c += (long)x[0] - y[0];
            z[0] = (uint)c;
            c >>= 32;
            c += (long)x[1] - y[1];
            z[1] = (uint)c;
            c >>= 32;
            c += (long)x[2] - y[2];
            z[2] = (uint)c;
            c >>= 32;
            c += (long)x[3] - y[3];
            z[3] = (uint)c;
            c >>= 32;
            c += (long)x[4] - y[4];
            z[4] = (uint)c;
            c >>= 32;
            c += (long)x[5] - y[5];
            z[5] = (uint)c;
            c >>= 32;
            c += (long)x[6] - y[6];
            z[6] = (uint)c;
            c >>= 32;
            return (int)c;
        }
#endif

        public static int SubBothFrom(uint[] x, uint[] y, uint[] z)
        {
            long c = 0;
            c += (long)z[0] - x[0] - y[0];
            z[0] = (uint)c;
            c >>= 32;
            c += (long)z[1] - x[1] - y[1];
            z[1] = (uint)c;
            c >>= 32;
            c += (long)z[2] - x[2] - y[2];
            z[2] = (uint)c;
            c >>= 32;
            c += (long)z[3] - x[3] - y[3];
            z[3] = (uint)c;
            c >>= 32;
            c += (long)z[4] - x[4] - y[4];
            z[4] = (uint)c;
            c >>= 32;
            c += (long)z[5] - x[5] - y[5];
            z[5] = (uint)c;
            c >>= 32;
            c += (long)z[6] - x[6] - y[6];
            z[6] = (uint)c;
            c >>= 32;
            return (int)c;
        }

        public static int SubFrom(uint[] x, uint[] z)
        {
            long c = 0;
            c += (long)z[0] - x[0];
            z[0] = (uint)c;
            c >>= 32;
            c += (long)z[1] - x[1];
            z[1] = (uint)c;
            c >>= 32;
            c += (long)z[2] - x[2];
            z[2] = (uint)c;
            c >>= 32;
            c += (long)z[3] - x[3];
            z[3] = (uint)c;
            c >>= 32;
            c += (long)z[4] - x[4];
            z[4] = (uint)c;
            c >>= 32;
            c += (long)z[5] - x[5];
            z[5] = (uint)c;
            c >>= 32;
            c += (long)z[6] - x[6];
            z[6] = (uint)c;
            c >>= 32;
            return (int)c;
        }

        public static int SubFrom(uint[] x, int xOff, uint[] z, int zOff)
        {
            long c = 0;
            c += (long)z[zOff + 0] - x[xOff + 0];
            z[zOff + 0] = (uint)c;
            c >>= 32;
            c += (long)z[zOff + 1] - x[xOff + 1];
            z[zOff + 1] = (uint)c;
            c >>= 32;
            c += (long)z[zOff + 2] - x[xOff + 2];
            z[zOff + 2] = (uint)c;
            c >>= 32;
            c += (long)z[zOff + 3] - x[xOff + 3];
            z[zOff + 3] = (uint)c;
            c >>= 32;
            c += (long)z[zOff + 4] - x[xOff + 4];
            z[zOff + 4] = (uint)c;
            c >>= 32;
            c += (long)z[zOff + 5] - x[xOff + 5];
            z[zOff + 5] = (uint)c;
            c >>= 32;
            c += (long)z[zOff + 6] - x[xOff + 6];
            z[zOff + 6] = (uint)c;
            c >>= 32;
            return (int)c;
        }

        public static BigInteger ToBigInteger(uint[] x)
        {
            byte[] bs = new byte[28];
            for (int i = 0; i < 7; ++i)
            {
                uint x_i = x[i];
                if (x_i != 0)
                {
                    Pack.UInt32_To_BE(x_i, bs, (6 - i) << 2);
                }
            }
            return new BigInteger(1, bs);
        }

        public static void Zero(uint[] z)
        {
            z[0] = 0;
            z[1] = 0;
            z[2] = 0;
            z[3] = 0;
            z[4] = 0;
            z[5] = 0;
            z[6] = 0;
        }
    }
}
