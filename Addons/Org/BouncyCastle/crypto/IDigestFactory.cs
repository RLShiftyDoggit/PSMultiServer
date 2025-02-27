using System;

namespace MultiServer.Addons.Org.BouncyCastle.Crypto
{
    /// <summary>
    /// Base interface for operator factories that create stream-based digest calculators.
    /// </summary>
    // TODO[api] Add 'out A' type parameter for AlgorithmDetails return type
    public interface IDigestFactory
	{
		/// <summary>The algorithm details object for calculators made by this factory.</summary>
		object AlgorithmDetails { get ; }

		/// <summary>Return the size of the digest associated with this factory.</summary>
		/// <returns>The length of the digest produced by this calculators from this factory in bytes.</returns>
		int DigestLength { get; }

		/// <summary>
		/// Create a stream calculator for the digest associated with this factory. The stream
		/// calculator is used for the actual operation of entering the data to be digested
		/// and producing the digest block.
		/// </summary>
		/// <returns>A calculator producing an IBlockResult with the final digest in it.</returns>
		IStreamCalculator<IBlockResult> CreateCalculator();
	}
}
