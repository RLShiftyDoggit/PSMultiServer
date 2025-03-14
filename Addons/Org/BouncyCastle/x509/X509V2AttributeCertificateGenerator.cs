using System;
using System.Collections.Generic;
using System.IO;

using MultiServer.Addons.Org.BouncyCastle.Asn1;
using MultiServer.Addons.Org.BouncyCastle.Asn1.X509;
using MultiServer.Addons.Org.BouncyCastle.Crypto;
using MultiServer.Addons.Org.BouncyCastle.Math;
using MultiServer.Addons.Org.BouncyCastle.Security.Certificates;
using MultiServer.Addons.Org.BouncyCastle.Utilities;

namespace MultiServer.Addons.Org.BouncyCastle.X509
{
	/// <remarks>Class to produce an X.509 Version 2 AttributeCertificate.</remarks>
	public class X509V2AttributeCertificateGenerator
	{
		private readonly X509ExtensionsGenerator extGenerator = new X509ExtensionsGenerator();

		private V2AttributeCertificateInfoGenerator acInfoGen;

		public X509V2AttributeCertificateGenerator()
		{
			acInfoGen = new V2AttributeCertificateInfoGenerator();
		}

		/// <summary>Reset the generator</summary>
		public void Reset()
		{
			acInfoGen = new V2AttributeCertificateInfoGenerator();
			extGenerator.Reset();
		}

		/// <summary>Set the Holder of this Attribute Certificate.</summary>
		public void SetHolder(
			AttributeCertificateHolder holder)
		{
			acInfoGen.SetHolder(holder.m_holder);
		}

		/// <summary>Set the issuer.</summary>
		public void SetIssuer(
			AttributeCertificateIssuer issuer)
		{
			acInfoGen.SetIssuer(AttCertIssuer.GetInstance(issuer.form));
		}

		/// <summary>Set the serial number for the certificate.</summary>
		public void SetSerialNumber(
			BigInteger serialNumber)
		{
			acInfoGen.SetSerialNumber(new DerInteger(serialNumber));
		}

		public void SetNotBefore(
			DateTime date)
		{
			acInfoGen.SetStartDate(new Asn1GeneralizedTime(date));
		}

		public void SetNotAfter(
			DateTime date)
		{
			acInfoGen.SetEndDate(new Asn1GeneralizedTime(date));
		}

		/// <summary>Add an attribute.</summary>
		public void AddAttribute(
			X509Attribute attribute)
		{
			acInfoGen.AddAttribute(AttributeX509.GetInstance(attribute.ToAsn1Object()));
		}

		public void SetIssuerUniqueId(
			bool[] iui)
		{
			// TODO convert bool array to bit string
			//acInfoGen.SetIssuerUniqueID(iui);
			throw new NotImplementedException("SetIssuerUniqueId()");
		}

		/// <summary>Add a given extension field for the standard extensions tag.</summary>
		public void AddExtension(
			string			oid,
			bool			critical,
			Asn1Encodable	extensionValue)
		{
			extGenerator.AddExtension(new DerObjectIdentifier(oid), critical, extensionValue);
		}

		/// <summary>
		/// Add a given extension field for the standard extensions tag.
		/// The value parameter becomes the contents of the octet string associated
		/// with the extension.
		/// </summary>
		public void AddExtension(
			string	oid,
			bool	critical,
			byte[]	extensionValue)
		{
			extGenerator.AddExtension(new DerObjectIdentifier(oid), critical, extensionValue);
		}

		/// <summary>
		/// Generate a new <see cref="X509V2AttributeCertificate"/> using the provided <see cref="ISignatureFactory"/>.
		/// </summary>
		/// <param name="signatureFactory">A <see cref="ISignatureFactory">signature factory</see> with the necessary
		/// algorithm details.</param>
		/// <returns>An <see cref="X509V2AttributeCertificate"/>.</returns>
		public X509V2AttributeCertificate Generate(ISignatureFactory signatureFactory)
        {
			var sigAlgID = (AlgorithmIdentifier)signatureFactory.AlgorithmDetails;

			acInfoGen.SetSignature(sigAlgID);

			if (!extGenerator.IsEmpty)
			{
				acInfoGen.SetExtensions(extGenerator.Generate());
			}

            var acInfo = acInfoGen.GenerateAttributeCertificateInfo();

			var signature = X509Utilities.GenerateSignature(signatureFactory, acInfo);

			return new X509V2AttributeCertificate(new AttributeCertificate(acInfo, sigAlgID, signature));
		}

        /// <summary>
        /// Allows enumeration of the signature names supported by the generator.
        /// </summary>
        public IEnumerable<string> SignatureAlgNames
		{
			get { return X509Utilities.GetAlgNames(); }
		}
	}
}
