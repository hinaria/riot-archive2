using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Complete.IO;

namespace RiotArchive
{
    static class StreamExtensions
    {
        public static uint ReadUInt(this Stream stream)
        {
            var bytes = stream.ReadBytes(4);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public static async Task<uint> ReadUIntAsync(this Stream stream)
        {
            var bytes = await stream.ReadBytesAsync(4);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public static string ReadString(this Stream stream)
        {
            var length = ReadUInt(stream);
            var bytes = stream.ReadBytes((int)length);
            return Encoding.UTF8.GetString(bytes);
        }

        public static async Task<string> ReadStringAsync(this Stream stream)
        {
            var length = await ReadUIntAsync(stream);
            var bytes = await stream.ReadBytesAsync((int)length);
            return Encoding.UTF8.GetString(bytes);
        }





        public static void WriteUInt(this Stream stream, uint i)
        {
            var bytes = GetUIntBytes(i);
            stream.Write(bytes, 0, bytes.Length);
        }
        
        public static Task WriteUIntAsync(this Stream stream, uint i)
        {
            var bytes = GetUIntBytes(i);
            return stream.WriteAsync(bytes, 0, bytes.Length);
        }

        public static void WriteString(this Stream stream, string str)
        {
            var stringBytes = Encoding.UTF8.GetBytes(str);
            var lengthBytes = GetUIntBytes((uint)stringBytes.Length);
            var bytes = lengthBytes.Concat(stringBytes).ToArray();
            stream.Write(bytes, 0, bytes.Length);
        }

        public static Task WriteStringAsync(this Stream stream, string str)
        {
            var stringBytes = Encoding.UTF8.GetBytes(str);
            var lengthBytes = GetUIntBytes((uint)stringBytes.Length);
            var bytes = lengthBytes.Concat(stringBytes).ToArray();
            return stream.WriteAsync(bytes, 0, bytes.Length);
        }





        static byte[] GetUIntBytes(uint i)
        {
            var bytes = BitConverter.GetBytes(i);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }
    }
}
