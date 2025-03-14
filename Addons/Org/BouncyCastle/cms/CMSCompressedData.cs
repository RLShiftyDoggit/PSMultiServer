using System.IO;

using MultiServer.Addons.Org.BouncyCastle.Asn1;
using MultiServer.Addons.Org.BouncyCastle.Asn1.Cms;
using MultiServer.Addons.Org.BouncyCastle.Utilities;
using MultiServer.Addons.Org.BouncyCastle.Utilities.IO.Compression;

namespace MultiServer.Addons.Org.BouncyCastle.Cms
{
    /**
    * containing class for an CMS Compressed Data object
    */
    public class CmsCompressedData
    {
        internal ContentInfo contentInfo;

		public CmsCompressedData(
            byte[] compressedData)
            : this(CmsUtilities.ReadContentInfo(compressedData))
        {
        }

		public CmsCompressedData(
            Stream compressedDataStream)
            : this(CmsUtilities.ReadContentInfo(compressedDataStream))
        {
        }

		public CmsCompressedData(
            ContentInfo contentInfo)
        {
            this.contentInfo = contentInfo;
        }

		/**
		 * Return the uncompressed content.
		 *
		 * @return the uncompressed content
		 * @throws CmsException if there is an exception uncompressing the data.
		 */
		public byte[] GetContent()
        {
            CompressedData comData = CompressedData.GetInstance(contentInfo.Content);
            ContentInfo content = comData.EncapContentInfo;

			Asn1OctetString bytes = (Asn1OctetString) content.Content;
			Stream zIn = ZLib.DecompressInput(bytes.GetOctetStream());

			try
			{
				return CmsUtilities.StreamToByteArray(zIn);
			}
			catch (IOException e)
			{
				throw new CmsException("exception reading compressed stream.", e);
			}
			finally
			{
                zIn.Dispose();
			}
        }

	    /**
	     * Return the uncompressed content, throwing an exception if the data size
	     * is greater than the passed in limit. If the content is exceeded getCause()
	     * on the CMSException will contain a StreamOverflowException
	     *
	     * @param limit maximum number of bytes to read
	     * @return the content read
	     * @throws CMSException if there is an exception uncompressing the data.
	     */
		public byte[] GetContent(int limit)
		{
			CompressedData  comData = CompressedData.GetInstance(contentInfo.Content);
			ContentInfo     content = comData.EncapContentInfo;

			Asn1OctetString bytes = (Asn1OctetString)content.Content;
            Stream zIn = ZLib.DecompressInput(bytes.GetOctetStream());

			try
			{
				return CmsUtilities.StreamToByteArray(zIn, limit);
			}
			catch (IOException e)
			{
				throw new CmsException("exception reading compressed stream.", e);
			}
		}

		/**
		 * return the ContentInfo 
		 */
		public ContentInfo ContentInfo
		{
			get { return contentInfo; }
		}

		/**
        * return the ASN.1 encoded representation of this object.
        */
        public byte[] GetEncoded()
        {
			return contentInfo.GetEncoded();
        }
    }
}
