using MultiServer.Addons.Org.BouncyCastle.Utilities;
using System;

namespace MultiServer.Addons.Org.BouncyCastle.Pqc.Crypto.Crystals.Dilithium
{
    internal class Packing
    {

        public static byte[] PackPublicKey(PolyVecK t1, DilithiumEngine Engine)
        {
            byte[] output = new byte[Engine.CryptoPublicKeyBytes - DilithiumEngine.SeedBytes];

            for (int i = 0; i < Engine.K; i++)
            {
                Array.Copy(t1.Vec[i].PolyT1Pack(), 0, output, i * DilithiumEngine.PolyT1PackedBytes, DilithiumEngine.PolyT1PackedBytes );
            }
            return output;
        }

        public static PolyVecK UnpackPublicKey(PolyVecK t1, byte[] pk, DilithiumEngine Engine)
        {
            int i;
            for (i = 0; i < Engine.K; ++i)
            {
                t1.Vec[i].PolyT1Unpack(Arrays.CopyOfRange(pk, i * DilithiumEngine.PolyT1PackedBytes, DilithiumEngine.SeedBytes + (i + 1) * DilithiumEngine.PolyT1PackedBytes));
            }

            return t1;
        }

        public static void PackSecretKey(byte[] t0_, byte[] s1_, byte[] s2_, PolyVecK t0, PolyVecL s1, PolyVecK s2, DilithiumEngine Engine)
        {
            int i;
            

            for (i = 0; i < Engine.L; ++i)
            {
                s1.Vec[i].PolyEtaPack(s1_, i * Engine.PolyEtaPackedBytes);
            }

            for (i = 0; i < Engine.K; ++i)
            {
                s2.Vec[i].PolyEtaPack(s2_, i * Engine.PolyEtaPackedBytes);
            }

            for (i = 0; i < Engine.K; ++i)
            {
                t0.Vec[i].PolyT0Pack(t0_,i * DilithiumEngine.PolyT0PackedBytes);
            }
        }

        public static void UnpackSecretKey(PolyVecK t0, PolyVecL s1, PolyVecK s2, byte[] t0Enc, byte[] s1Enc, byte[] s2Enc, DilithiumEngine Engine)
        {
            int i;
            for (i = 0; i < Engine.L; ++i)
            {
                s1.Vec[i].PolyEtaUnpack(s1Enc,i * Engine.PolyEtaPackedBytes);
            }
            for (i = 0; i < Engine.K; ++i)
            {
                s2.Vec[i].PolyEtaUnpack(s2Enc,i * Engine.PolyEtaPackedBytes);
            }
            for (i = 0; i < Engine.K; ++i)
            {
                t0.Vec[i].PolyT0Unpack(t0Enc,i * DilithiumEngine.PolyT0PackedBytes);
            }
        }

        public static void PackSignature(byte[] sig, byte[] c, PolyVecL z, PolyVecK h, DilithiumEngine engine)
        {
            int i, j, k, end = 0;

            Array.Copy(c, 0, sig, 0, engine.CTilde);
            end += engine.CTilde;

            for (i = 0; i < engine.L; ++i)
            {
                z.Vec[i].PackZ(sig, end + i * engine.PolyZPackedBytes);
            }
            end += engine.L * engine.PolyZPackedBytes;

            for (i = 0; i < engine.Omega + engine.K; ++i)
            {
                sig[end + i] = 0;
            }


            k = 0;
            for (i = 0; i < engine.K; ++i)
            {
                for (j = 0; j < DilithiumEngine.N; ++j)
                {
                    if (h.Vec[i].Coeffs[j] != 0)
                    {
                        sig[end + k++] = (byte)j;
                    }
                }
                sig[end + engine.Omega + i] = (byte)k;
            }
            //Console.WriteLine("sig = " + Convert.ToHexString(sig));

        }

        public static bool UnpackSignature(PolyVecL z, PolyVecK h, byte[] sig, DilithiumEngine engine)
        {
            int i, j, k;
            
            int end = engine.CTilde;
            for (i = 0; i < engine.L; ++i)
            {
                z.Vec[i].UnpackZ(Arrays.CopyOfRange(sig, end + i * engine.PolyZPackedBytes, end + (i + 1) * engine.PolyZPackedBytes));
            }
            end += engine.L * engine.PolyZPackedBytes;

            k = 0;
            for (i = 0; i < engine.K; ++i)
            {
                for (j = 0; j < DilithiumEngine.N; ++j)
                {
                    h.Vec[i].Coeffs[j] = 0;
                }

                if ((sig[end + engine.Omega + i] & 0xFF) < k || (sig[end + engine.Omega + i] & 0xFF) > engine.Omega)
                {
                    return false;
                }

                for (j = k; j < (sig[end + engine.Omega + i] & 0xFF); ++j)
                {
                    if (j > k && (sig[end + j] & 0xFF) <= (sig[end + j - 1] & 0xFF))
                    {
                        return false;
                    }
                    h.Vec[i].Coeffs[sig[end + j] & 0xFF] = 1;
                }

                k = (int)(sig[end + engine.Omega + i]);
            }
            for (j = k; j < engine.Omega; ++j)
            {
                if ((sig[end + j] & 0xFF) != 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
