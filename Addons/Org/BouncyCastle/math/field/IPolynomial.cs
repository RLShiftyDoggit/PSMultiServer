using System;

namespace MultiServer.Addons.Org.BouncyCastle.Math.Field
{
    public interface IPolynomial
    {
        int Degree { get; }

        //BigInteger[] GetCoefficients();

        int[] GetExponentsPresent();

        //Term[] GetNonZeroTerms();
    }
}
