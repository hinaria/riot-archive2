using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Patcher;
using Patcher.Manifest;
using RiotArchive.File;

namespace RiotArchive
{
    /// <summary>
    /// Provides read-only access to a collection of Riot Archive Files, used in the League of
    /// Legends. Thead-safe.
    /// </summary>
    public class ArchiveCollection
    {
        public Archive[] Archives { get; private set; }

        private ArchiveCollection() { }

        public static async Task<ArchiveCollection> FromFilesAsync(IEnumerable<string> files)
        {
            var archiveTasks = files.Select(Archive.FromFileAsync).ToArray();
            var archives = await Task.WhenAll(archiveTasks);

            return new ArchiveCollection()
            {
                Archives = archives
            };
        }

        public bool Exists(string path)
        {
            return Archives.Any(a => a.Exists(path));
        }

        public IEnumerable<ArchiveFile> Files
        {
            get
            {
                var lookup = Archives.SelectMany(a => a.Files).ToLookup(x => x.FullName, StringComparer.OrdinalIgnoreCase);
                return from fileGrouping in lookup
                       select FilterFiles(fileGrouping.ToArray());
            }
        }

        public IEnumerable<ArchiveFile> GetFiles(string regularExpression)
        {
            var lookup = Archives.SelectMany(a => a.GetFiles(regularExpression)).ToLookup(x => x.FullName, StringComparer.OrdinalIgnoreCase);
            return from fileGrouping in lookup
                   select FilterFiles(fileGrouping.ToArray());
        }

        public ArchiveFile this[string path]
        {
            get
            {
                var files = Archives
                    .Where(archive => archive.Exists(path))
                    .Select(archive => archive[path])
                    .ToArray();

                return FilterFiles(files);
            }
        }

        public bool TryGetValue(string path, out ArchiveFile file)
        {
            ArchiveFile outFile = null;
            var result = Archives.Any(a => a.TryGetValue(path, out outFile));
            file = outFile;
            return result;
        }

        /////////////////////
        // Helpers

        static ArchiveFile FilterFiles(ArchiveFile[] files)
        {
            if (files.Length <= 1)
                return files[0];

            // Try to use the latest package if package names are available... otherwise will just return an arbitary item
            var pair = files.OrderByDescending(SortByReleasePackage).First();
            return pair;
        }

        static int SortByReleasePackage(ArchiveFile file)
        {
            if (file.Owner == null)
                return 0;

            var regex = ReleasePackagePathRegex.Regex;
            var path = file.Owner.FullName;
            var match = regex.Match(path);
            if (match.Success)
            {
                var str = match.Groups[1].Value;
                if (ReleasePackage.IsVersionString(str))
                    return new ReleasePackage(str).Version;
            }
            return 0;
        }
    }
}
