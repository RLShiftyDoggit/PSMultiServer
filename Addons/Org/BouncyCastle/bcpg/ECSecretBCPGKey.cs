using System;

using MultiServer.Addons.Org.BouncyCastle.Math;

namespace MultiServer.Addons.Org.BouncyCastle.Bcpg
{
	/// <remarks>Base class for an EC Secret Key.</remarks>
    public class ECSecretBcpgKey
        : BcpgObject, IBcpgKey
    {
        internal readonly MPInteger m_x;

        public ECSecretBcpgKey(
            BcpgInputStream bcpgIn)
        {
            m_x = new MPInteger(bcpgIn);
        }

        public ECSecretBcpgKey(
            BigInteger x)
        {
            m_x = new MPInteger(x);
        }

		/// <summary>The format, as a string, always "PGP".</summary>
		public string Format
		{
			get { return "PGP"; }
		}

		/// <summary>Return the standard PGP encoding of the key.</summary>
		public override byte[] GetEncoded()
		{
			try
			{
				return base.GetEncoded();
			}
			catch (Exception)
			{
				return null;
			}
		}

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            bcpgOut.WriteObject(m_x);
        }

        public virtual BigInteger X
        {
            get { return m_x.Value; }
        }
    }
}
