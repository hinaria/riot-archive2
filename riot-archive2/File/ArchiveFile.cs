using Complete.Security.Cryptography;
using System;
using System.IO;
using System.Security.Cryptography;

namespace RiotArchive.File
{
    public abstract class ArchiveFile
    {
        static readonly HashAlgorithm MD5Hasher = new MD5Managed();

        // Gets the full name of the file
        public abstract string FullName { get; }

        public abstract Archive Owner { get; }

        public abstract Stream GetStream(DecompressionMode mode);
        
        public virtual Stream GetStream() { return GetStream(DecompressionMode.Auto); }

        // Returns the name of the file, if the file full name is a path
        public string Name { get { return Path.GetFileName(this.FullName); } }

        // Returns the Riot Hash for this file name.
        public uint Hash { get { return ArchiveHash.GetHash(this.FullName); } }

        // Infrastructure to support proprietary checksum file. Return null to indicate no MD5
        // extension (MD5 will be calculated from stream).
        internal virtual byte[] ExtendedMd5 { get; set; }

        public byte[] Md5
        {
            get
            {
                if (ExtendedMd5 == null)
                {
                    lock (this)
                    {
                        if (ExtendedMd5 == null)
                            ExtendedMd5 = CalculateMd5();
                    }
                }

                return ExtendedMd5;
            }
        }

        byte[] CalculateMd5()
        {
            using (var stream = GetStream(DecompressionMode.Auto))
                return MD5Hasher.ComputeHash(stream);
        }
    }
}
