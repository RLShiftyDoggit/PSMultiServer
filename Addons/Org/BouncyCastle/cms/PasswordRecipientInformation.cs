using System;
using System.IO;

using MultiServer.Addons.Org.BouncyCastle.Asn1;
using MultiServer.Addons.Org.BouncyCastle.Asn1.Cms;
using MultiServer.Addons.Org.BouncyCastle.Asn1.X509;
using MultiServer.Addons.Org.BouncyCastle.Crypto;
using MultiServer.Addons.Org.BouncyCastle.Crypto.Parameters;
using MultiServer.Addons.Org.BouncyCastle.Security;

namespace MultiServer.Addons.Org.BouncyCastle.Cms
{
	/**
	 * the RecipientInfo class for a recipient who has been sent a message
	 * encrypted using a password.
	 */
	public class PasswordRecipientInformation
		: RecipientInformation
	{
		private readonly PasswordRecipientInfo	info;

		internal PasswordRecipientInformation(
			PasswordRecipientInfo	info,
			CmsSecureReadable		secureReadable)
			: base(info.KeyEncryptionAlgorithm, secureReadable)
		{
			this.info = info;
			this.rid = new RecipientID();
		}

		/**
		 * return the object identifier for the key derivation algorithm, or null
		 * if there is none present.
		 *
		 * @return OID for key derivation algorithm, if present.
		 */
		public virtual AlgorithmIdentifier KeyDerivationAlgorithm
		{
			get { return info.KeyDerivationAlgorithm; }
		}

		/**
		 * decrypt the content and return an input stream.
		 */
		public override CmsTypedStream GetContentStream(
			ICipherParameters key)
		{
			try
			{
				AlgorithmIdentifier kekAlg = AlgorithmIdentifier.GetInstance(info.KeyEncryptionAlgorithm);
				Asn1Sequence        kekAlgParams = (Asn1Sequence)kekAlg.Parameters;
				byte[]              encryptedKey = info.EncryptedKey.GetOctets();
				string              kekAlgName = DerObjectIdentifier.GetInstance(kekAlgParams[0]).Id;
				string				cName = CmsEnvelopedHelper.GetRfc3211WrapperName(kekAlgName);
				IWrapper			keyWrapper = WrapperUtilities.GetWrapper(cName);

                var iv = Asn1OctetString.GetInstance(kekAlgParams[1]);

                ICipherParameters parameters = ((CmsPbeKey)key).GetEncoded(kekAlgName);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                parameters = new ParametersWithIV(parameters, iv.GetOctetsSpan());
#else
				parameters = new ParametersWithIV(parameters, iv.GetOctets());
#endif

                keyWrapper.Init(false, parameters);

				KeyParameter sKey = ParameterUtilities.CreateKeyParameter(
					GetContentAlgorithmName(), keyWrapper.Unwrap(encryptedKey, 0, encryptedKey.Length));

				return GetContentFromSessionKey(sKey);
			}
			catch (SecurityUtilityException e)
			{
				throw new CmsException("couldn't create cipher.", e);
			}
			catch (InvalidKeyException e)
			{
				throw new CmsException("key invalid in message.", e);
			}
		}
	}
}
