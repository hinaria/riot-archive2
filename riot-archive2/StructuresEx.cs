using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Complete.IO;
using RiotArchive.File;

namespace RiotArchive
{
    ////////////////////
    // RAF Extensions

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ChecksumFileHeader
    {
        // - Number of files expected in the RAF
        // - Number of checksums
        public long FileCount;
    }

    class Checksum
    {
        // Expected name (prefixed with 32-bit size)
        public string Name;
        // Expected data offset
        public uint DataOffset;
        // Expected data length
        public uint DataLength;
        // 16-byte MD5
        public byte[] Md5;

        public static Checksum Read(MemoryStream stream)
        {
            return new Checksum()
            {
                Name = stream.ReadString(),
                DataOffset = stream.ReadUInt(),
                DataLength = stream.ReadUInt(),
                Md5 = stream.ReadBytes(Md5Hash.Size)
            };
        }

        public void Write(MemoryStream stream)
        {
            if (Md5 == null || Md5.Length != Md5Hash.Size)
                throw new NullReferenceException("MD5 Checksum isn't correct.");

            stream.WriteString(Name);
            stream.WriteUInt(DataOffset);
            stream.WriteUInt(DataLength);
            stream.Write(Md5, 0, Md5.Length);
        }
    }


    class ChecksumLookup
    {
        string Name;
        uint DataOffset;
        uint DataLength;

        public ChecksumLookup(string name, uint dataOffset, uint dataLength)
        {
            Name = name;
            DataOffset = dataOffset;
            DataLength = dataLength;
        }

        public static ChecksumLookup FromChecksum(Checksum checksum)
        {
            return new ChecksumLookup(checksum.Name, checksum.DataOffset, checksum.DataLength);
        }

        public static ChecksumLookup FromArchivedFile(ArchivedFile file)
        {
            return new ChecksumLookup(file.FullName, (uint)file.DataOffset, (uint)file.DataLength);
        }

        #region Equality Comparer

        public sealed class EqualityComparer : IEqualityComparer<ChecksumLookup>
        {
            public static readonly IEqualityComparer<ChecksumLookup> Instance = new EqualityComparer();

            bool IEqualityComparer<ChecksumLookup>.Equals(ChecksumLookup x, ChecksumLookup y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.Name, y.Name) && x.DataOffset == y.DataOffset && x.DataLength == y.DataLength;
            }

            int IEqualityComparer<ChecksumLookup>.GetHashCode(ChecksumLookup obj)
            {
                unchecked
                {
                    int hashCode = (obj.Name != null ? obj.Name.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (int)obj.DataOffset;
                    hashCode = (hashCode * 397) ^ (int)obj.DataLength;
                    return hashCode;
                }
            }
        }

        #endregion
    }

}
