using System;
using System.IO;

namespace MultiServer.Addons.Org.BouncyCastle.Asn1
{
	internal class IndefiniteLengthInputStream
		: LimitedInputStream
	{
        private int _lookAhead;
        private bool _eofOn00 = true;

		internal IndefiniteLengthInputStream(Stream	inStream, int limit)
			: base(inStream, limit)
		{
            _lookAhead = RequireByte();

            if (0 == _lookAhead)
            {
                CheckEndOfContents();
            }
        }

		internal void SetEofOn00(bool eofOn00)
		{
			_eofOn00 = eofOn00;
            if (_eofOn00 && 0 == _lookAhead)
            {
                CheckEndOfContents();
            }
        }

        private void CheckEndOfContents()
        {
            if (0 != RequireByte())
                throw new IOException("malformed end-of-contents marker");

            _lookAhead = -1;
            SetParentEofDetect();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // Only use this optimisation if we aren't checking for 00
            if (_eofOn00 || count <= 1)
				return base.Read(buffer, offset, count);

			if (_lookAhead < 0)
				return 0;

			int numRead = _in.Read(buffer, offset + 1, count - 1);
			if (numRead <= 0)
				throw new EndOfStreamException();

			buffer[offset] = (byte)_lookAhead;
			_lookAhead = RequireByte();

			return numRead + 1;
		}

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        public override int Read(Span<byte> buffer)
        {
            // Only use this optimisation if we aren't checking for 00
            if (_eofOn00 || buffer.Length <= 1)
                return base.Read(buffer);

            if (_lookAhead < 0)
                return 0;

            int numRead = _in.Read(buffer[1..]);
            if (numRead <= 0)
                throw new EndOfStreamException();

            buffer[0] = (byte)_lookAhead;
            _lookAhead = RequireByte();

            return numRead + 1;
        }
#endif

        public override int ReadByte()
		{
            if (_eofOn00 && _lookAhead <= 0)
            {
                if (0 == _lookAhead)
                {
                    CheckEndOfContents();
                }
                return -1;
            }

            int result = _lookAhead;
            _lookAhead = RequireByte();
            return result;
		}

        private int RequireByte()
        {
            int b = _in.ReadByte();
            if (b < 0)
                throw new EndOfStreamException();

            return b;
        }
	}
}
