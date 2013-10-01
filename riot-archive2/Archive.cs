using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Complete.IO;
using RiotArchive.File;

namespace RiotArchive
{
    public class Archive
    {
        internal const uint Magic = 0x18BE0EF0;
        internal const uint Version = 1;

        public static Archive Empty = new Archive(null);

        public bool ReadChecksumFile { get; set; }

        // Full file path to the *.raf file
        public string FullName { get; private set; }
        public uint ManagerIndex { get; private set; }
        public IEnumerable<ArchiveFile> Files { get { return filesBacking.Values; } }

        Dictionary<string, ArchiveFile> filesBacking;

        private Archive(string path)
        {
            ReadChecksumFile = true;

            this.FullName = path;
            this.filesBacking = new Dictionary<string, ArchiveFile>(0);
        }

        #region From*

        public static Task<Archive> FromFileAsync(string path)
        {
            return FromFileAsync(path, true);
        }
        public static async Task<Archive> FromFileAsync(string path, bool readChecksumFile)
        {
            var archive = new Archive(path) { ReadChecksumFile = readChecksumFile };
            await archive.ReadAsync();
            return archive;
        }

        #endregion

        #region Read

        async Task ReadAsync()
        {
            using (var stream = new FileStream(FullName, FileMode.Open, FileAccess.Read))
            using (var memory = new MemoryStream((int)stream.Length))
            {
                var checksums = await GetChecksumsAsync();

                await stream.CopyToAsync(memory);
                memory.Position = 0;

                LoadWithChecksums(memory, checksums);
            }
        }

        void LoadWithChecksums(MemoryStream stream, Checksum[] checksums)
        {
            var archive = stream.ReadStruct<RiotArchiveHeader>();
            if (archive.Magic != Magic || archive.Version != Version)
                throw new FileFormatIncorrectException();

            this.ManagerIndex = archive.ManagerIndex;
            var names = GetNames(stream, archive);
            var files = GetFiles(stream, archive, names, checksums);
            this.filesBacking = files.ToDictionary(x => x.FullName, x => x, StringComparer.OrdinalIgnoreCase);
        }

        string[] GetNames(Stream stream, RiotArchiveHeader archive)
        {
            stream.Seek(archive.NamesOffset, SeekOrigin.Begin);

            // Get list of offsets into the string pool
            var header = stream.ReadStruct<RiotStringsHeader>();
            var count = (int)header.Length;

            var offsets = new RiotString[count];
            for (int i = 0; i < count; i++)
                offsets[i] = stream.ReadStruct<RiotString>();

            // Read strings from string pool
            var strings = new List<string>(count);
            foreach (var x in offsets)
            {
                var offset = archive.NamesOffset + x.Offset;
                stream.Seek(offset, SeekOrigin.Begin);

                var bytes = stream.ReadBytes((int)x.Length);
                var value = Encoding.ASCII.GetString(bytes);
                var trimmed = value.TrimEnd('\0');

                strings.Add(trimmed);
            }
            return strings.ToArray();
        }

        IEnumerable<ArchiveFile> GetFiles(Stream stream, RiotArchiveHeader archive, string[] paths, Checksum[] checksums)
        {
            var checksumsLookup = checksums.ToDictionary(ChecksumLookup.FromChecksum, ChecksumLookup.EqualityComparer.Instance);

            stream.Seek(archive.FilesOffset, SeekOrigin.Begin);
            var header = stream.ReadStruct<RiotFilesHeader>();

            for (int i = 0; i < header.Length; i++)
            {
                var fileDesc = stream.ReadStruct<RiotFile>();
                var file = new ArchivedFile(this, paths[fileDesc.NameIndex], (int)fileDesc.DataOffset, (int)fileDesc.DataLength);

                Checksum checksum;
                if (checksumsLookup.TryGetValue(ChecksumLookup.FromArchivedFile(file), out checksum))
                    file.ExtendedMd5 = checksum.Md5;

                yield return file;
            }
        }

        async Task<Checksum[]> GetChecksumsAsync()
        {
            var checksumPath = FullName + ".dat.chk";

            if (!ReadChecksumFile || !System.IO.File.Exists(checksumPath))
                return new Checksum[0];

            try
            {
                using (var stream = new FileStream(checksumPath, FileMode.Open, FileAccess.Read))
                    return await IntermediateMemoryBuffer.CopyFromStreamAsync(stream, GetChecksums);
            }
            catch
            {
            }

            return new Checksum[0];
        }

        Checksum[] GetChecksums(MemoryStream stream)
        {
            var header = stream.ReadStruct<ChecksumFileHeader>();
            var count = header.FileCount;

            var checksums = new Checksum[count];
            for (int i = 0; i < count; i++)
                checksums[i] = Checksum.Read(stream);
            return checksums;
        }

        #endregion

        public IEnumerable<ArchiveFile> GetFiles(string regularExpression)
        {
            if (this.filesBacking == null)
                return Enumerable.Empty<ArchiveFile>();

            return new ArchiveSearchEnumerable(this.filesBacking.Values, regularExpression);
        }

        public bool Exists(string path)
        {
            return filesBacking.ContainsKey(path);
        }

        public ArchiveFile this[string path]
        {
            get { return filesBacking[path]; }
        }

        public bool TryGetValue(string path, out ArchiveFile file)
        {
            return filesBacking.TryGetValue(path, out file);
        }
    }
}
