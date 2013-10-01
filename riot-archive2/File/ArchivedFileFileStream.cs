using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Complete.IO;
using Complete.IO.Zlib;

namespace RiotArchive.File
{
    /// <summary>
    /// Read-only file stream. This class is NOT thread safe.
    /// </summary>
    public class ArchivedFileFileStream : Stream
    {
        readonly string archiveFile;
        readonly int dataOffset;
        readonly int dataLength;
        readonly DecompressionMode decompressionMode;
        
        bool initialized;

        Stream stream;
        Stream zlibStream;

        static readonly uint ZlibCompressionMethodMask;

        static ArchivedFileFileStream()
        {
            ZlibCompressionMethodMask = Convert.ToUInt32("00001111", 2);
        }

        public ArchivedFileFileStream(string archiveFile, int dataOffset, int dataLength, DecompressionMode decompressionMode)
        {
            this.archiveFile = archiveFile;
            this.dataOffset = dataOffset;
            this.dataLength = dataLength;
            this.decompressionMode = decompressionMode;
        }

        public void Open()
        {
            if (initialized)
                return;
            initialized = true;

            var fileStream = new FileStream(this.archiveFile + ".dat", FileMode.Open, FileAccess.Read);
            stream = new SubStream(fileStream, dataOffset, dataLength);

            var decompress = false;

            switch (decompressionMode)
            {
                case DecompressionMode.DontDecompress:
                    decompress = false;
                    break;
                case DecompressionMode.Decompress:
                    decompress = true;
                    break;
                case DecompressionMode.Auto:
                    var zlibHeader = new byte[2];
                    var bytesRead = stream.Read(zlibHeader, 0, 2);
                    if (bytesRead != 2)
                        throw new InvalidDataException();

                    // reset the stream position after checking header
                    stream.Seek(0, SeekOrigin.Begin);

                    // See http://tools.ietf.org/html/rfc1950 for zlib file format
                    var compressionMethod = zlibHeader[0] & ZlibCompressionMethodMask;
                    var compressionInfo = zlibHeader[0] >> 4;

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(zlibHeader);

                    var hasZlibHeader = compressionMethod == 8 && compressionInfo <= 7 && BitConverter.ToUInt16(zlibHeader, 0) % 31 == 0;
                    decompress = hasZlibHeader;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("mode");
            }

            if (decompress)
                zlibStream = new ZlibStream(stream, CompressionMode.Decompress);
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (offset == 0 && origin == SeekOrigin.Begin)
            {
                ResetStreams();
                Open();
                return 0;
            }
                
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (zlibStream != null)
                return zlibStream.Read(buffer, offset, count);

            return stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override long Length
        {
            get
            {
                if (stream != null && zlibStream == null)
                    return stream.Length;
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        protected override void Dispose(bool disposing)
        {
            ResetStreams();

            base.Dispose(disposing);
        }

        void ResetStreams()
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }

            if (zlibStream != null)
            {
                zlibStream.Dispose();
                zlibStream = null;
            }

            if (initialized)
                initialized = false;
        }
    }
}
