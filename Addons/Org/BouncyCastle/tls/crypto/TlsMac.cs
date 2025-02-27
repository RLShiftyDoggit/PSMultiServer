using System;

namespace MultiServer.Addons.Org.BouncyCastle.Tls.Crypto
{
    /// <summary>Interface for MAC services.</summary>
    public interface TlsMac
    {
        /// <summary>Set the key to be used by the MAC implementation supporting this service.</summary>
        /// <param name="key">array holding the MAC key.</param>
        /// <param name="keyOff">offset into the array the key starts at.</param>
        /// <param name="keyLen">length of the key in the array.</param>
        void SetKey(byte[] key, int keyOff, int keyLen);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        void SetKey(ReadOnlySpan<byte> key);
#endif

        /// <summary>Update the MAC with the passed in input.</summary>
        /// <param name="input">input array containing the data.</param>
        /// <param name="inOff">offset into the input array the input starts at.</param>
        /// <param name="length">the length of the input data.</param>
        void Update(byte[] input, int inOff, int length);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        void Update(ReadOnlySpan<byte> input);
#endif

        /// <summary>Return calculated MAC for any input passed in.</summary>
        /// <returns>the MAC value.</returns>
        byte[] CalculateMac();

        /// <summary>Write the calculated MAC to an output buffer.</summary>
        /// <param name="output">output array to write the MAC to.</param>
        /// <param name="outOff">offset into the output array to write the MAC to.</param>
        void CalculateMac(byte[] output, int outOff);

        /// <summary>Return the length of the MAC generated by this service.</summary>
        /// <returns>the MAC length.</returns>
        int MacLength { get; }

        /// <summary>Reset the MAC underlying this service.</summary>
        void Reset();
    }
}
