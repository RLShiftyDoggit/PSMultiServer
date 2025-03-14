using System.IO;

namespace MultiServer.Addons.Org.BouncyCastle.Asn1
{
	public class DerSetGenerator
		: DerGenerator
	{
		private readonly MemoryStream _bOut = new MemoryStream();

		public DerSetGenerator(Stream outStream)
			: base(outStream)
		{
		}

		public DerSetGenerator(Stream outStream, int tagNo, bool isExplicit)
			: base(outStream, tagNo, isExplicit)
		{
		}

        protected override void Finish()
        {
            WriteDerEncoded(Asn1Tags.Constructed | Asn1Tags.Set, _bOut.ToArray());
        }

        public override void AddObject(Asn1Encodable obj)
		{
            obj.EncodeTo(_bOut, Asn1Encodable.Der);
		}

        public override void AddObject(Asn1Object obj)
        {
            obj.EncodeTo(_bOut, Asn1Encodable.Der);
        }

        public override Stream GetRawOutputStream()
		{
			return _bOut;
		}
	}
}
