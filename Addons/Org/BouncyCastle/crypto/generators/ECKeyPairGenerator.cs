using System;

using MultiServer.Addons.Org.BouncyCastle.Asn1;
using MultiServer.Addons.Org.BouncyCastle.Asn1.Sec;
using MultiServer.Addons.Org.BouncyCastle.Asn1.X9;
using MultiServer.Addons.Org.BouncyCastle.Crypto.EC;
using MultiServer.Addons.Org.BouncyCastle.Crypto.Parameters;
using MultiServer.Addons.Org.BouncyCastle.Math;
using MultiServer.Addons.Org.BouncyCastle.Math.EC;
using MultiServer.Addons.Org.BouncyCastle.Math.EC.Multiplier;
using MultiServer.Addons.Org.BouncyCastle.Security;

namespace MultiServer.Addons.Org.BouncyCastle.Crypto.Generators
{
    public class ECKeyPairGenerator
        : IAsymmetricCipherKeyPairGenerator
    {
        private readonly string algorithm;

        private ECDomainParameters parameters;
        private DerObjectIdentifier publicKeyParamSet;
        private SecureRandom random;

        public ECKeyPairGenerator()
            : this("EC")
        {
        }

        public ECKeyPairGenerator(
            string algorithm)
        {
            if (algorithm == null)
                throw new ArgumentNullException("algorithm");

            this.algorithm = ECKeyParameters.VerifyAlgorithmName(algorithm);
        }

        public void Init(
            KeyGenerationParameters parameters)
        {
            if (parameters is ECKeyGenerationParameters)
            {
                ECKeyGenerationParameters ecP = (ECKeyGenerationParameters) parameters;

                this.publicKeyParamSet = ecP.PublicKeyParamSet;
                this.parameters = ecP.DomainParameters;
            }
            else
            {
                DerObjectIdentifier oid;
                switch (parameters.Strength)
                {
                case 192:
                    oid = X9ObjectIdentifiers.Prime192v1;
                    break;
                case 224:
                    oid = SecObjectIdentifiers.SecP224r1;
                    break;
                case 239:
                    oid = X9ObjectIdentifiers.Prime239v1;
                    break;
                case 256:
                    oid = X9ObjectIdentifiers.Prime256v1;
                    break;
                case 384:
                    oid = SecObjectIdentifiers.SecP384r1;
                    break;
                case 521:
                    oid = SecObjectIdentifiers.SecP521r1;
                    break;
                default:
                    throw new InvalidParameterException("unknown key size.");
                }

                X9ECParameters ecps = FindECCurveByOid(oid);

                this.publicKeyParamSet = oid;
                this.parameters = new ECDomainParameters(
                    ecps.Curve, ecps.G, ecps.N, ecps.H, ecps.GetSeed());
            }

            this.random = parameters.Random;

            if (this.random == null)
            {
                this.random = CryptoServicesRegistrar.GetSecureRandom();
            }
        }

        /**
         * Given the domain parameters this routine generates an EC key
         * pair in accordance with X9.62 section 5.2.1 pages 26, 27.
         */
        public AsymmetricCipherKeyPair GenerateKeyPair()
        {
            BigInteger n = parameters.N;
            BigInteger d;
            int minWeight = n.BitLength >> 2;

            for (;;)
            {
                d = new BigInteger(n.BitLength, random);

                if (d.CompareTo(BigInteger.One) < 0 || d.CompareTo(n) >= 0)
                    continue;

                if (WNafUtilities.GetNafWeight(d) < minWeight)
                    continue;

                break;
            }

            ECPoint q = CreateBasePointMultiplier().Multiply(parameters.G, d);

            if (publicKeyParamSet != null)
            {
                return new AsymmetricCipherKeyPair(
                    new ECPublicKeyParameters(algorithm, q, publicKeyParamSet),
                    new ECPrivateKeyParameters(algorithm, d, publicKeyParamSet));
            }

            return new AsymmetricCipherKeyPair(
                new ECPublicKeyParameters(algorithm, q, parameters),
                new ECPrivateKeyParameters(algorithm, d, parameters));
        }

        protected virtual ECMultiplier CreateBasePointMultiplier()
        {
            return new FixedPointCombMultiplier();
        }

        internal static X9ECParameters FindECCurveByName(string name)
        {
            return CustomNamedCurves.GetByName(name) ?? ECNamedCurveTable.GetByName(name);
        }

        internal static X9ECParametersHolder FindECCurveByNameLazy(string name)
        {
            return CustomNamedCurves.GetByNameLazy(name) ?? ECNamedCurveTable.GetByNameLazy(name);
        }

        internal static X9ECParameters FindECCurveByOid(DerObjectIdentifier oid)
        {
            return CustomNamedCurves.GetByOid(oid) ?? ECNamedCurveTable.GetByOid(oid);
        }

        internal static X9ECParametersHolder FindECCurveByOidLazy(DerObjectIdentifier oid)
        {
            return CustomNamedCurves.GetByOidLazy(oid) ?? ECNamedCurveTable.GetByOidLazy(oid);
        }

        internal static ECPublicKeyParameters GetCorrespondingPublicKey(
            ECPrivateKeyParameters privKey)
        {
            ECDomainParameters ec = privKey.Parameters;
            ECPoint q = new FixedPointCombMultiplier().Multiply(ec.G, privKey.D);

            if (privKey.PublicKeyParamSet != null)
            {
                return new ECPublicKeyParameters(privKey.AlgorithmName, q, privKey.PublicKeyParamSet);
            }

            return new ECPublicKeyParameters(privKey.AlgorithmName, q, ec);
        }
    }
}
