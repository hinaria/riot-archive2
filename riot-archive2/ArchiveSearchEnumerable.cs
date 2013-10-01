using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RiotArchive.File;

namespace RiotArchive
{
    internal class ArchiveSearchEnumerable : IEnumerable<ArchiveFile>
    {
        readonly IEnumerable<ArchiveFile> files;
        readonly Regex regularExpression;

        public ArchiveSearchEnumerable(IEnumerable<ArchiveFile> files, string regularExpression)
        {
            this.files = files;
            this.regularExpression = new Regex(regularExpression, RegexOptions.IgnoreCase | RegexOptions.ECMAScript | RegexOptions.CultureInvariant);
        }

        public IEnumerator<ArchiveFile> GetEnumerator()
        {
            return new ArchiveSearchEnumerator(files.GetEnumerator(), regularExpression.IsMatch);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class ArchiveSearchEnumerator : IEnumerator<ArchiveFile>
    {
        readonly IEnumerator<ArchiveFile> enumerator;
        readonly Predicate<string> predicate;

        public ArchiveSearchEnumerator(IEnumerator<ArchiveFile> enumerator, Predicate<string> predicate)
        {
            this.enumerator = enumerator;
            this.predicate = predicate;
        }

        public void Dispose()
        {
            enumerator.Dispose();
        }

        public bool MoveNext()
        {
            while (enumerator.MoveNext())
            {
                if (predicate.Invoke(enumerator.Current.FullName))
                    return true;
            }
            return false;
        }

        public void Reset()
        {
            enumerator.Reset();
        }

        object IEnumerator.Current { get { return Current; } }
        public ArchiveFile Current { get { return enumerator.Current; } }
    }
}
