using System;
using System.IO;

using MultiServer.Addons.Org.BouncyCastle.Asn1;
using MultiServer.Addons.Org.BouncyCastle.Asn1.Cms;
using MultiServer.Addons.Org.BouncyCastle.Asn1.X509;
using MultiServer.Addons.Org.BouncyCastle.Crypto;
using MultiServer.Addons.Org.BouncyCastle.Crypto.IO;
using MultiServer.Addons.Org.BouncyCastle.Crypto.Parameters;
using MultiServer.Addons.Org.BouncyCastle.Security;
using MultiServer.Addons.Org.BouncyCastle.Utilities;
using MultiServer.Addons.Org.BouncyCastle.Utilities.IO;

namespace MultiServer.Addons.Org.BouncyCastle.Cms
{
	/**
	 * General class for generating a CMS authenticated-data message.
	 *
	 * A simple example of usage.
	 *
	 * <pre>
	 *      CMSAuthenticatedDataGenerator  fact = new CMSAuthenticatedDataGenerator();
	 *
	 *      fact.addKeyTransRecipient(cert);
	 *
	 *      CMSAuthenticatedData         data = fact.generate(content, algorithm, "BC");
	 * </pre>
	 */
	public class CmsAuthenticatedDataGenerator
	    : CmsAuthenticatedGenerator
	{
	    public CmsAuthenticatedDataGenerator()
	    {
	    }

        /// <summary>Constructor allowing specific source of randomness</summary>
        /// <param name="random">Instance of <c>SecureRandom</c> to use.</param>
	    public CmsAuthenticatedDataGenerator(SecureRandom random)
	        : base(random)
	    {
	    }

	    /**
	     * generate an enveloped object that contains an CMS Enveloped Data
	     * object using the given provider and the passed in key generator.
	     */
		private CmsAuthenticatedData Generate(
			CmsProcessable		content,
			string				macOid,
			CipherKeyGenerator	keyGen)
		{
			AlgorithmIdentifier macAlgId;
			KeyParameter encKey;
			Asn1OctetString encContent;
			Asn1OctetString macResult;

			try
			{
				// FIXME Will this work for macs?
				byte[] encKeyBytes = keyGen.GenerateKey();
				encKey = ParameterUtilities.CreateKeyParameter(macOid, encKeyBytes);

				Asn1Encodable asn1Params = GenerateAsn1Parameters(macOid, encKeyBytes);

				macAlgId = GetAlgorithmIdentifier(macOid, encKey, asn1Params, out var cipherParameters);

				IMac mac = MacUtilities.GetMac(macOid);
				// TODO Confirm no ParametersWithRandom needed
				// FIXME Only passing key at the moment
//	            mac.Init(cipherParameters);
				mac.Init(encKey);

				var bOut = new MemoryStream();
				using (var mOut = new TeeOutputStream(bOut, new MacSink(mac)))
				{
					content.Write(mOut);
				}

                encContent = new BerOctetString(bOut.ToArray());

				byte[] macOctets = MacUtilities.DoFinal(mac);
				macResult = new DerOctetString(macOctets);
			}
			catch (SecurityUtilityException e)
			{
				throw new CmsException("couldn't create cipher.", e);
			}
			catch (InvalidKeyException e)
			{
				throw new CmsException("key invalid in message.", e);
			}
			catch (IOException e)
			{
				throw new CmsException("exception decoding algorithm parameters.", e);
			}

			var recipientInfos = new Asn1EncodableVector(recipientInfoGenerators.Count);

			foreach (RecipientInfoGenerator rig in recipientInfoGenerators) 
			{
				try
				{
					recipientInfos.Add(rig.Generate(encKey, m_random));
				}
				catch (InvalidKeyException e)
				{
					throw new CmsException("key inappropriate for algorithm.", e);
				}
				catch (GeneralSecurityException e)
				{
					throw new CmsException("error making encrypted content.", e);
				}
			}

			var eci = new ContentInfo(CmsObjectIdentifiers.Data, encContent);

			var contentInfo = new ContentInfo(
				CmsObjectIdentifiers.AuthenticatedData,
				new AuthenticatedData(null, DerSet.FromVector(recipientInfos), macAlgId, null, eci, null, macResult, null));

			return new CmsAuthenticatedData(contentInfo);
		}

	    /**
	     * generate an authenticated object that contains an CMS Authenticated Data object
	     */
	    public CmsAuthenticatedData Generate(
	        CmsProcessable	content,
	        string			encryptionOid)
	    {
            try
            {
				// FIXME Will this work for macs?
				CipherKeyGenerator keyGen = GeneratorUtilities.GetKeyGenerator(encryptionOid);

				keyGen.Init(new KeyGenerationParameters(m_random, keyGen.DefaultStrength));

				return Generate(content, encryptionOid, keyGen);
            }
            catch (SecurityUtilityException e)
            {
                throw new CmsException("can't find key generation algorithm.", e);
            }
	    }
	}
}
