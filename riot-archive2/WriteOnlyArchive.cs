using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Complete.IO;
using System.Runtime.InteropServices;
using Complete.Async;

namespace RiotArchive
{
    public class WriteOnlyArchive
    {
        public string FullName { get; private set; }

        FileStream dataStream;
        FileStream descriptionStream;
        FileStream checksumStream;

        readonly List<FileEntry> files;
        readonly AsyncLock streamLock;
        bool initialized;

        public bool CreateChecksumFile { get; set; }

        public WriteOnlyArchive()
        {
            CreateChecksumFile = true;

            files = new List<FileEntry>();
            streamLock = new AsyncLock();
        }

        public WriteOnlyArchive(string path) : this()
        {
            SetOutput(path);
        }

        public void SetOutput(string path)
        {
            if (initialized || dataStream != null)
                throw new InvalidOperationException();

            try
            {
                descriptionStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
                dataStream = new FileStream(path + ".dat", FileMode.Create, FileAccess.ReadWrite);

                if (CreateChecksumFile)
                    checksumStream = new FileStream(path + ".dat.chk", FileMode.Create, FileAccess.ReadWrite);

                FullName = path;
                initialized = true;
            }
            catch
            {
                CloseStreams();
                throw;
            }
        }

        // If `WriteChecksumFile` is enabled and a checksum is not provided, one will be calculated.
        // However, this means that the `stream` MUST support seeking back to the start of the
        // stream.
        public Task WriteAsync(string name, Stream stream)
        {
            if (CreateChecksumFile)
            {
                var md5 = Md5Hash.Compute(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return WriteAsync(name, stream, md5);
            }
            else
            {
                return WriteAsync(name, stream, null);
            }
        }

        public async Task WriteAsync(string name, Stream stream, byte[] md5)
        {
            if (dataStream == null)
                throw new InvalidOperationException();

            using (await streamLock.LockAsync())
            {
                var offset = dataStream.Position;
                await stream.CopyToAsync(dataStream);
                var length = dataStream.Position - offset;

                files.Add(new FileEntry(name, (uint)offset, (uint)length, md5));
            }
        }

        public async Task CommitAsync()
        {
            if (dataStream == null)
                throw new InvalidOperationException();

            ////////////////////
            // Init

            // File entries must be ordered by hash, then by lexicographically by name for Riot's
            // library to work
            var files = this.files
                .OrderBy(x => x.NameHash)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var header = new RiotArchiveHeader(Archive.Magic, Archive.Version);
            var stream = descriptionStream;



            ////////////////////
            // Write

            // Skip the header; we'll come back to it once we have all the
            // required offsets
            await stream.WriteStructAsync(header);

            // Write file list
            header.FilesOffset = (uint)stream.Position;
            await IntermediateMemoryBuffer.CopyToStreamAsync(stream, memory => WriteFileList(files, memory));

            // Write name list (aka path list or string list)
            header.NamesOffset = (uint)stream.Position;
            await IntermediateMemoryBuffer.CopyToStreamAsync(stream, memory => WriteNameList(files, memory));

            // Return to the header and write it out
            stream.Seek(0, SeekOrigin.Begin);
            await stream.WriteStructAsync(header);


            if (CreateChecksumFile && checksumStream != null)
                await IntermediateMemoryBuffer.CopyToStreamAsync(checksumStream, WriteChecksumFile);


            ////////////////////
            // Finish

            // Dispose of all streams
            CloseStreams();
        }

        void WriteFileList(FileEntry[] files, MemoryStream stream)
        {
            stream.WriteStruct(new RiotFilesHeader((uint)files.Length));
            for (uint i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var entry = new RiotFile(file.NameHash, file.DataOffset, file.DataLength, i);
                stream.WriteStruct(entry);
            }
        }

        void WriteNameList(FileEntry[] files, MemoryStream stream)
        {
            var offsets = new NameListOffsets((uint)stream.Position, (uint)files.Length);
            var strings = files.Select(x => GetCString(x.Name)).ToArray();
            var stringLength = strings.Sum(x => x.Length);
            var totalLength = offsets.Strings - offsets.Header + stringLength;

            // Write the header
            var header = new RiotStringsHeader((uint)totalLength, (uint)files.Length);
            stream.WriteStruct(header);

            // Write pointers to strings
            var offsetFromHeader = offsets.StringsFromNameListHeader;
            foreach (var str in strings)
            {
                var length = (uint)str.Length;
                var entry = new RiotString(offsetFromHeader, length);
                offsetFromHeader += length;

                stream.WriteStruct(entry);
            }

            // Write actual strings
            foreach (var str in strings)
                stream.Write(str, 0, str.Length);
        }

        void WriteChecksumFile(MemoryStream stream)
        {
            var files = this.files.ToArray();

            // If any of the files don't have an MD5 checksum, we can't continue.
            if (files.Any(x => x.Md5 == null))
                return;

            stream.WriteStruct(new ChecksumFileHeader() { FileCount = files.Length });

            foreach (var file in files)
            {
                var checksum = new Checksum()
                {
                    Name = file.Name,
                    DataOffset = file.DataOffset,
                    DataLength = file.DataLength,
                    Md5 = file.Md5
                };
                checksum.Write(stream);
            }
        }

        void CloseStreams()
        {
            if (dataStream != null)
            {
                dataStream.Close();
                dataStream = null;
            }
            if (descriptionStream != null)
            {
                descriptionStream.Close();
                descriptionStream = null;
            }
            if (checksumStream != null)
            {
                checksumStream.Close();
                checksumStream = null;
            }
        }
        
        byte[] GetCString(string str)
        {
            var characters = str.Length;
            var bytes = new byte[characters + 1];
            Encoding.ASCII.GetBytes(str, 0, characters, bytes, 0);
            return bytes;
        }



        struct FileEntry
        {
            public readonly uint DataOffset;
            public readonly uint DataLength;
            public readonly string Name;
            public readonly uint NameHash;
            public readonly byte[] Md5;

            public FileEntry(string name, uint offset, uint length, byte[] md5)
            {
                this.Name = name;
                this.NameHash = ArchiveHash.GetHash(name);

                this.DataOffset = offset;
                this.DataLength = length;

                if (md5 != null && md5.Length != Md5Hash.Size)
                    throw new ArgumentOutOfRangeException("md5", "MD5 isn't right.");

                this.Md5 = md5;
            }
        }

        class NameListOffsets
        {
            static readonly uint HeaderSize = (uint)Marshal.SizeOf(typeof(RiotStringsHeader));
            static readonly uint EntrySize = (uint)Marshal.SizeOf(typeof(RiotString));

            public uint Header { get; private set; }
            public uint Entries { get { return Header + HeaderSize; } }
            public uint Strings { get { return Entries + EntrySize * itemCount; } }
            public uint StringsFromNameListHeader { get { return Strings - Header; } }

            readonly uint itemCount;

            public NameListOffsets(uint headerOffset, uint itemCount)
            {
                this.Header = headerOffset;
                this.itemCount = itemCount;
            }
        }
    }
}