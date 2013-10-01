using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RiotArchive
{
    ////////////////////
    // RAF Structures (Spec)

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct RiotArchiveHeader
    {
        public uint Magic;
        public uint Version;
        public uint ManagerIndex;
        public uint FilesOffset;
        public uint NamesOffset;

        public RiotArchiveHeader(uint magic, uint version) : this()
        {
            Magic = magic;
            Version = version;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct RiotStringsHeader
    {
        // Size, in bytes, of the PathList
        public uint SizeInBytes;
        // Number of path strings contained in the path list
        public uint Length;

        public RiotStringsHeader(uint sizeInBytes, uint length)
        {
            SizeInBytes = sizeInBytes;
            Length = length;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct RiotString
    {
        // offset from the START OF THE STRING TABLE
        public uint Offset;
        // length of the string, in bytes
        public uint Length;

        public RiotString(uint offset, uint length)
        {
            Offset = offset;
            Length = length;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct RiotFilesHeader
    {
        // Number of files in the file list
        public uint Length;

        public RiotFilesHeader(uint length)
        {
            Length = length;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct RiotFile
    {
        public uint Hash;
        public uint DataOffset;
        public uint DataLength;
        public uint NameIndex;

        public RiotFile(uint hash, uint dataOffset, uint dataLength, uint nameIndex)
        {
            Hash = hash;
            DataOffset = dataOffset;
            DataLength = dataLength;
            NameIndex = nameIndex;
        }
    }
}
