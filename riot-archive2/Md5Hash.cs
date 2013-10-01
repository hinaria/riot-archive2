using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Complete.Security.Cryptography;

namespace RiotArchive
{
    static class Md5Hash
    {
        public const int Size = 16;

        static readonly HashAlgorithm Md5 = new MD5Managed();

        public static byte[] Compute(Stream stream)
        {
            return Md5.ComputeHash(stream);
        }
        public static byte[] Compute(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                return Compute(stream);
        }
    }
}
