using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patcher.Manifest
{
    public struct ReleasePackage
    {
        public readonly int Version;

        public string String
        {
            get
            {
                return string.Format(
                    "{0}.{1}.{2}.{3}",
                    (Version >> 24) & 0xff,
                    (Version >> 16) & 0xff,
                    (Version >> 8) & 0xff,
                    (Version >> 0) & 0xff);
            }
        }

        public ReleasePackage(int version) : this()
        {
            this.Version = version;
        }

        public ReleasePackage(string s) : this()
        {
            var parts = s.Split('.');
            if (parts.Length != 4)
                throw new ArgumentOutOfRangeException();
            this.Version = parts.Aggregate(0, (v, b) => (v << 8) | byte.Parse(b));
        }

        public static bool IsVersionString(string s)
        {
            try { new ReleasePackage(s); }
            catch (Exception) { return false; }
            return true;
        }

        public override string ToString()
        {
            return this.String;
        }

        public bool Equals(ReleasePackage other)
        {
            return Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is ReleasePackage && Equals((ReleasePackage)obj);
        }

        public override int GetHashCode()
        {
            return Version;
        }
    }

}
