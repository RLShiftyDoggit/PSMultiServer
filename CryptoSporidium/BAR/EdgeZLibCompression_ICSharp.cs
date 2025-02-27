﻿using MultiServer.Addons.ICSharpCode.SharpZipLib.Zip.Compression;
using MultiServer.Addons.ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace MultiServer.CryptoSporidium.BAR
{
    public class EdgeZLibCompression_ICSharp : CompressionBase
    {
        public override byte[] Compress(TOCEntry te)
        {
            return Compress(te.RawData);
        }

        public override byte[] Decrypt(TOCEntry te)
        {
            return Decompress(te.RawData);
        }

        public override byte[] Compress(byte[] inData)
        {
            MemoryStream memoryStream = new MemoryStream(inData.Length);
            MemoryStream memoryStream2 = new MemoryStream(inData);
            while (memoryStream2.Position < memoryStream2.Length)
            {
                int num = Math.Min((int)(memoryStream2.Length - memoryStream2.Position), 65535);
                byte[] array = new byte[num];
                memoryStream2.Read(array, 0, num);
                byte[] array2 = CompressChunk(array);
                memoryStream.Write(array2, 0, array2.Length);
            }
            memoryStream2.Close();
            memoryStream.Close();
            return memoryStream.ToArray();
        }

        private byte[] CompressChunk(byte[] InData)
        {
            MemoryStream memoryStream = new MemoryStream();
            Deflater deflater = new Deflater(9, true);
            DeflaterOutputStream deflaterOutputStream = new DeflaterOutputStream(memoryStream, deflater);
            deflaterOutputStream.Write(InData, 0, InData.Length);
            deflaterOutputStream.Close();
            memoryStream.Close();
            byte[] array = memoryStream.ToArray();
            byte[] array2;
            if (array.Length >= InData.Length)
                array2 = InData;
            else
                array2 = array;
            byte[] array3 = new byte[array2.Length + 4];
            Array.Copy(array2, 0, array3, 4, array2.Length);
            ChunkHeader chunkHeader = default;
            chunkHeader.SourceSize = (ushort)InData.Length;
            chunkHeader.CompressedSize = (ushort)array2.Length;
            byte[] array4 = chunkHeader.GetBytes();
            array4 = Utils.EndianSwap(array4);
            Array.Copy(array4, 0, array3, 0, ChunkHeader.SizeOf);
            return array3;
        }

        public override byte[] Decompress(byte[] inData)
        {
            MemoryStream memoryStream = new MemoryStream();
            MemoryStream memoryStream2 = new MemoryStream(inData);
            byte[] array = new byte[ChunkHeader.SizeOf];
            while (memoryStream2.Position < memoryStream2.Length)
            {
                memoryStream2.Read(array, 0, ChunkHeader.SizeOf);
                array = Utils.EndianSwap(array);
                ChunkHeader header = ChunkHeader.FromBytes(array);
                int compressedSize = header.CompressedSize;
                byte[] array2 = new byte[compressedSize];
                memoryStream2.Read(array2, 0, compressedSize);
                byte[] array3 = DecompressChunk(array2, header);
                memoryStream.Write(array3, 0, array3.Length);
            }
            memoryStream2.Close();
            memoryStream.Close();
            return memoryStream.ToArray();
        }

        private byte[] DecompressChunk(byte[] inData, ChunkHeader header)
        {
            if (header.CompressedSize == header.SourceSize)
                return inData;
            MemoryStream baseInputStream = new MemoryStream(inData);
            Inflater inf = new Inflater(true);
            InflaterInputStream inflaterInputStream = new InflaterInputStream(baseInputStream, inf);
            MemoryStream memoryStream = new MemoryStream();
            byte[] array = new byte[4096];
            for (; ; )
            {
                int num = inflaterInputStream.Read(array, 0, array.Length);
                if (num <= 0)
                    break;
                memoryStream.Write(array, 0, num);
            }
            inflaterInputStream.Close();
            return memoryStream.ToArray();
        }

        public override CompressionMethod Method
        {
            get
            {
                return CompressionMethod.EdgeZLib;
            }
        }

        private const uint SPUBLOCKSIZE = 65535U;

        private const ushort ZLIBHEADER = 55928;

        internal struct ChunkHeader
        {
            internal byte[] GetBytes()
            {
                byte[] array = new byte[4];
                Array.Copy(BitConverter.GetBytes(SourceSize), 0, array, 2, 2);
                Array.Copy(BitConverter.GetBytes(CompressedSize), 0, array, 0, 2);
                return array;
            }

            internal static int SizeOf
            {
                get
                {
                    return 4;
                }
            }

            internal static ChunkHeader FromBytes(byte[] inData)
            {
                ChunkHeader result = default;
                byte[] array = inData;
                if (inData.Length > SizeOf)
                {
                    array = new byte[4];
                    Array.Copy(inData, array, 4);
                }
                result.SourceSize = BitConverter.ToUInt16(array, 2);
                result.CompressedSize = BitConverter.ToUInt16(array, 0);
                return result;
            }

            // Token: 0x0400003B RID: 59
            internal ushort SourceSize;

            // Token: 0x0400003C RID: 60
            internal ushort CompressedSize;
        }
    }
}
