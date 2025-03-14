using System;
using System.Diagnostics;
#if NETSTANDARD1_0_OR_GREATER || NETCOREAPP1_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

using MultiServer.Addons.Org.BouncyCastle.Crypto.Utilities;
using MultiServer.Addons.Org.BouncyCastle.Math.Raw;
using MultiServer.Addons.Org.BouncyCastle.Utilities;

namespace MultiServer.Addons.Org.BouncyCastle.Crypto.Modes.Gcm
{
    internal static class GcmUtilities
    {
        internal struct FieldElement
        {
            internal ulong n0, n1;
        }

        private const uint E1 = 0xe1000000;
        private const ulong E1UL = (ulong)E1 << 32;

        internal static void One(out FieldElement x)
        {
            x.n0 = 1UL << 63;
            x.n1 = 0UL;
        }

#if NETSTANDARD1_0_OR_GREATER || NETCOREAPP1_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static void AsBytes(ulong x0, ulong x1, byte[] z)
        {
            Pack.UInt64_To_BE(x0, z, 0);
            Pack.UInt64_To_BE(x1, z, 8);
        }

#if NETSTANDARD1_0_OR_GREATER || NETCOREAPP1_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static void AsBytes(ref FieldElement x, byte[] z)
        {
            AsBytes(x.n0, x.n1, z);
        }

#if NETSTANDARD1_0_OR_GREATER || NETCOREAPP1_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static void AsFieldElement(byte[] x, out FieldElement z)
        {
            z.n0 = Pack.BE_To_UInt64(x, 0);
            z.n1 = Pack.BE_To_UInt64(x, 8);
        }

        internal static void DivideP(ref FieldElement x, out FieldElement z)
        {
            ulong x0 = x.n0, x1 = x.n1;
            ulong m = (ulong)((long)x0 >> 63);
            x0 ^= (m & E1UL);
            z.n0 = (x0 << 1) | (x1 >> 63);
            z.n1 = (x1 << 1) | (ulong)(-(long)m);
        }

        internal static void Multiply(byte[] x, byte[] y)
        {
            AsFieldElement(x, out FieldElement X);
            AsFieldElement(y, out FieldElement Y);
            Multiply(ref X, ref Y);
            AsBytes(ref X, x);
        }

        internal static void Multiply(ref FieldElement x, ref FieldElement y)
        {
            ulong z0, z1, z2;

#if NETCOREAPP3_0_OR_GREATER
            if (Pclmulqdq.IsSupported)
            {
                var X = Vector128.Create(x.n1, x.n0);
                var Y = Vector128.Create(y.n1, y.n0);

                var Z0 = Pclmulqdq.CarrylessMultiply(X, Y, 0x00);
                var Z1 = Sse2.Xor(
                    Pclmulqdq.CarrylessMultiply(X, Y, 0x01),
                    Pclmulqdq.CarrylessMultiply(X, Y, 0x10));
                var Z2 = Pclmulqdq.CarrylessMultiply(X, Y, 0x11);

                ulong t3 = Z0.GetElement(0);
                ulong t2 = Z0.GetElement(1) ^ Z1.GetElement(0);
                ulong t1 = Z2.GetElement(0) ^ Z1.GetElement(1);
                ulong t0 = Z2.GetElement(1);

                Debug.Assert(t0 >> 63 == 0);

                t1 ^= t3 ^ (t3 >>  1) ^ (t3 >>  2) ^ (t3 >>  7);
                t2 ^=      (t3 << 63) ^ (t3 << 62) ^ (t3 << 57);

                z0 = (t0 << 1) | (t1 >> 63);
                z1 = (t1 << 1) | (t2 >> 63);
                z2 = (t2 << 1);

                z0 ^= z2 ^ (z2 >>  1) ^ (z2 >>  2) ^ (z2 >>  7);
//              z1 ^=      (z2 << 63) ^ (z2 << 62) ^ (z2 << 57);
                z1 ^=                   (t2 << 63) ^ (t2 << 58);
            }
            else
#endif
            {
                /*
                 * "Three-way recursion" as described in "Batch binary Edwards", Daniel J. Bernstein.
                 *
                 * Without access to the high part of a 64x64 product x * y, we use a bit reversal to calculate it:
                 *     rev(x) * rev(y) == rev((x * y) << 1) 
                 */

                ulong x0 = x.n0, x1 = x.n1;
                ulong y0 = y.n0, y1 = y.n1;
                ulong x0r = Longs.Reverse(x0), x1r = Longs.Reverse(x1);
                ulong y0r = Longs.Reverse(y0), y1r = Longs.Reverse(y1);
                ulong z3;

                ulong h0 = Longs.Reverse(ImplMul64(x0r, y0r));
                ulong h1 = ImplMul64(x0, y0) << 1;
                ulong h2 = Longs.Reverse(ImplMul64(x1r, y1r));
                ulong h3 = ImplMul64(x1, y1) << 1;
                ulong h4 = Longs.Reverse(ImplMul64(x0r ^ x1r, y0r ^ y1r));
                ulong h5 = ImplMul64(x0 ^ x1, y0 ^ y1) << 1;

                z0 = h0;
                z1 = h1 ^ h0 ^ h2 ^ h4;
                z2 = h2 ^ h1 ^ h3 ^ h5;
                z3 = h3;

                Debug.Assert(z3 << 63 == 0);

                z1 ^= z3 ^ (z3 >>  1) ^ (z3 >>  2) ^ (z3 >>  7);
//              z2 ^=      (z3 << 63) ^ (z3 << 62) ^ (z3 << 57);
                z2 ^=                   (z3 << 62) ^ (z3 << 57);

                z0 ^= z2 ^ (z2 >>  1) ^ (z2 >>  2) ^ (z2 >>  7);
                z1 ^=      (z2 << 63) ^ (z2 << 62) ^ (z2 << 57);
            }

            x.n0 = z0;
            x.n1 = z1;
        }

#if NETCOREAPP3_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector128<ulong> Load(byte[] x)
        {
            AsFieldElement(x, out FieldElement X);
            return Vector128.Create(X.n1, X.n0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector128<ulong> Multiply(Vector128<ulong> X, Vector128<ulong> Y)
        {
            MultiplyExt(X, Y, out var Z0, out var Z1, out var Z2);
            return Reduce3(Z0, Z1, Z2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void MultiplyExt(Vector128<ulong> X, Vector128<ulong> Y, out Vector128<ulong> Z0,
            out Vector128<ulong> Z1, out Vector128<ulong> Z2)
        {
            if (!Pclmulqdq.IsSupported)
                throw new PlatformNotSupportedException(nameof(GcmUtilities.MultiplyExt));

            Z0 =          Pclmulqdq.CarrylessMultiply(X, Y, 0x00);
            Z1 = Sse2.Xor(Pclmulqdq.CarrylessMultiply(X, Y, 0x01),
                          Pclmulqdq.CarrylessMultiply(X, Y, 0x10));
            Z2 =          Pclmulqdq.CarrylessMultiply(X, Y, 0x11);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector128<ulong> Reduce2(Vector128<ulong> Z0, Vector128<ulong> Z2)
        {
            ulong t3 = Z0.GetElement(0);
            ulong t2 = Z0.GetElement(1);
            ulong t1 = Z2.GetElement(0);
            ulong t0 = Z2.GetElement(1);

            Debug.Assert(t0 >> 63 == 0);

            t1 ^= t3 ^ (t3 >>  1) ^ (t3 >>  2) ^ (t3 >>  7);
            t2 ^=      (t3 << 63) ^ (t3 << 62) ^ (t3 << 57);

            ulong z0 = (t0 << 1) | (t1 >> 63);
            ulong z1 = (t1 << 1) | (t2 >> 63);
            ulong z2 = (t2 << 1);

            z0 ^= z2 ^ (z2 >>  1) ^ (z2 >>  2) ^ (z2 >>  7);
//          z1 ^=      (z2 << 63) ^ (z2 << 62) ^ (z2 << 57);
            z1 ^=                   (t2 << 63) ^ (t2 << 58);

            return Vector128.Create(z1, z0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector128<ulong> Reduce3(Vector128<ulong> Z0, Vector128<ulong> Z1, Vector128<ulong> Z2)
        {
            ulong t3 = Z0.GetElement(0);
            ulong t2 = Z0.GetElement(1) ^ Z1.GetElement(0);
            ulong t1 = Z2.GetElement(0) ^ Z1.GetElement(1);
            ulong t0 = Z2.GetElement(1);

            Debug.Assert(t0 >> 63 == 0);

            t1 ^= t3 ^ (t3 >>  1) ^ (t3 >>  2) ^ (t3 >>  7);
            t2 ^=      (t3 << 63) ^ (t3 << 62) ^ (t3 << 57);

            ulong z0 = (t0 << 1) | (t1 >> 63);
            ulong z1 = (t1 << 1) | (t2 >> 63);
            ulong z2 = (t2 << 1);

            z0 ^= z2 ^ (z2 >>  1) ^ (z2 >>  2) ^ (z2 >>  7);
//          z1 ^=      (z2 << 63) ^ (z2 << 62) ^ (z2 << 57);
            z1 ^=                   (t2 << 63) ^ (t2 << 58);

            return Vector128.Create(z1, z0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Store(Vector128<ulong> X, byte[] z)
        {
            AsBytes(X.GetElement(1), X.GetElement(0), z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector128<ulong> Square(Vector128<ulong> X)
        {
            SquareExt(X, out var Z0, out var Z2);
            return Reduce2(Z0, Z2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SquareExt(Vector128<ulong> X, out Vector128<ulong> Z0, out Vector128<ulong> Z2)
        {
            if (!Pclmulqdq.IsSupported)
                throw new PlatformNotSupportedException(nameof(GcmUtilities.SquareExt));

            Z0 = Pclmulqdq.CarrylessMultiply(X, X, 0x00);
            Z2 = Pclmulqdq.CarrylessMultiply(X, X, 0x11);
        }
#endif

        internal static void MultiplyP7(ref FieldElement x)
        {
            ulong x0 = x.n0, x1 = x.n1;
            ulong c = x1 << 57;
            x.n0 = (x0 >> 7) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            x.n1 = (x1 >> 7) | (x0 << 57);
        }

        internal static void MultiplyP8(ref FieldElement x)
        {
            ulong x0 = x.n0, x1 = x.n1;
            ulong c = x1 << 56;
            x.n0 = (x0 >> 8) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            x.n1 = (x1 >> 8) | (x0 << 56);
        }

        internal static void MultiplyP8(ref FieldElement x, out FieldElement y)
        {
            ulong x0 = x.n0, x1 = x.n1;
            ulong c = x1 << 56;
            y.n0 = (x0 >> 8) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            y.n1 = (x1 >> 8) | (x0 << 56);
        }

        internal static void MultiplyP16(ref FieldElement x)
        {
            ulong x0 = x.n0, x1 = x.n1;
            ulong c = x1 << 48;
            x.n0 = (x0 >> 16) ^ c ^ (c >> 1) ^ (c >> 2) ^ (c >> 7);
            x.n1 = (x1 >> 16) | (x0 << 48);
        }

        internal static void Square(ref FieldElement x)
        {
            ulong t1 = Interleave.Expand64To128Rev(x.n0, out ulong t0);
            ulong t3 = Interleave.Expand64To128Rev(x.n1, out ulong t2);

            Debug.Assert((t0 | t1 | t2 | t3) << 63 == 0UL);

            var z1 = t1 ^ t3 ^ (t3 >>  1) ^ (t3 >>  2) ^ (t3 >>  7);
            var z2 = t2 ^                   (t3 << 62) ^ (t3 << 57);

            x.n0   = t0 ^ z2 ^ (z2 >>  1) ^ (z2 >>  2) ^ (z2 >>  7);
            x.n1   = z1 ^                   (t2 << 62) ^ (t2 << 57);
        }

        internal static void Xor(byte[] x, byte[] y)
        {
            int i = 0;
            do
            {
                x[i] ^= y[i]; ++i;
                x[i] ^= y[i]; ++i;
                x[i] ^= y[i]; ++i;
                x[i] ^= y[i]; ++i;
            }
            while (i < 16);
        }

        internal static void Xor(byte[] x, byte[] y, int yOff)
        {
            int i = 0;
            do
            {
                x[i] ^= y[yOff + i]; ++i;
                x[i] ^= y[yOff + i]; ++i;
                x[i] ^= y[yOff + i]; ++i;
                x[i] ^= y[yOff + i]; ++i;
            }
            while (i < 16);
        }

        internal static void Xor(byte[] x, byte[] y, int yOff, int yLen)
        {
            while (--yLen >= 0)
            {
                x[yLen] ^= y[yOff + yLen];
            }
        }

        internal static void Xor(byte[] x, int xOff, byte[] y, int yOff, int len)
        {
            while (--len >= 0)
            {
                x[xOff + len] ^= y[yOff + len];
            }
        }

        internal static void Xor(ref FieldElement x, ref FieldElement y)
        {
            x.n0 ^= y.n0;
            x.n1 ^= y.n1;
        }

        internal static void Xor(ref FieldElement x, ref FieldElement y, out FieldElement z)
        {
            z.n0 = x.n0 ^ y.n0;
            z.n1 = x.n1 ^ y.n1;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        internal static void Xor(Span<byte> x, ReadOnlySpan<byte> y)
        {
            int i = 0;
            do
            {
                x[i] ^= y[i]; ++i;
                x[i] ^= y[i]; ++i;
                x[i] ^= y[i]; ++i;
                x[i] ^= y[i]; ++i;
            }
            while (i < 16);
        }

        internal static void Xor(Span<byte> x, ReadOnlySpan<byte> y, int len)
        {
            for (int i = 0; i < len; ++i)
            {
                x[i] ^= y[i];
            }
        }
#endif

        private static ulong ImplMul64(ulong x, ulong y)
        {
            ulong x0 = x & 0x1111111111111111UL;
            ulong x1 = x & 0x2222222222222222UL;
            ulong x2 = x & 0x4444444444444444UL;
            ulong x3 = x & 0x8888888888888888UL;

            ulong y0 = y & 0x1111111111111111UL;
            ulong y1 = y & 0x2222222222222222UL;
            ulong y2 = y & 0x4444444444444444UL;
            ulong y3 = y & 0x8888888888888888UL;

            ulong z0 = (x0 * y0) ^ (x1 * y3) ^ (x2 * y2) ^ (x3 * y1);
            ulong z1 = (x0 * y1) ^ (x1 * y0) ^ (x2 * y3) ^ (x3 * y2);
            ulong z2 = (x0 * y2) ^ (x1 * y1) ^ (x2 * y0) ^ (x3 * y3);
            ulong z3 = (x0 * y3) ^ (x1 * y2) ^ (x2 * y1) ^ (x3 * y0);

            z0 &= 0x1111111111111111UL;
            z1 &= 0x2222222222222222UL;
            z2 &= 0x4444444444444444UL;
            z3 &= 0x8888888888888888UL;

            return z0 | z1 | z2 | z3;
        }
    }
}
