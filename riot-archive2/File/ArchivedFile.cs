using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiotArchive.File
{
    /// <summary>
    /// Provides access to information about a file in a Archive.
    /// </summary>
    internal class ArchivedFile : ArchiveFile
    {
        public override string FullName { get { return Path; } }
        public override Archive Owner { get { return owner; } }

        internal readonly string Path;
        internal readonly int DataOffset;
        internal readonly int DataLength;

        readonly Archive owner;

        internal ArchivedFile(Archive owner, string path, int dataOffset, int dataLength)
        {
            this.owner = owner;
            this.Path = path;
            this.DataOffset = dataOffset;
            this.DataLength = dataLength;
        }

        public override Stream GetStream(DecompressionMode mode)
        {
            var stream = new ArchivedFileFileStream(this.owner.FullName, this.DataOffset, this.DataLength, mode);
            stream.Open();
            return stream;
        }
    }
}
