using System;

namespace MultiServer.Addons.Org.BouncyCastle.Crypto
{
    /// <summary>
    /// Operators that reduce their input to a single block return an object
    /// of this type.
    /// </summary>
    public interface IBlockResult
    {
        /// <summary>
        /// Return the final result of the operation.
        /// </summary>
        /// <returns>A block of bytes, representing the result of an operation.</returns>
        byte[] Collect();

        /// <summary>
        /// Store the final result of the operation by copying it into the destination array.
        /// </summary>
        /// <returns>The number of bytes copied into destination.</returns>
        /// <param name="buf">The byte array to copy the result into.</param>
        /// <param name="off">The offset into destination to start copying the result at.</param>
        int Collect(byte[] buf, int off);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Store the final result of the operation by copying it into the destination span.
        /// </summary>
        /// <returns>The number of bytes copied into destination.</returns>
        /// <param name="output">The span to copy the result into.</param>
        int Collect(Span<byte> output);
#endif

        /// <summary>Return an upper limit for the size of the result.</summary>
        int GetMaxResultLength();
    }
}
