﻿using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Force.Crc32;

namespace Learning.EventStore.Extensions
{
    public static class StringExtensions
    {
        public static string Compress(this string value, int compressionThreshold)
        {
            if (Encoding.UTF8.GetByteCount(value) > compressionThreshold && !value.IsCompressed())
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                using (var msi = new MemoryStream(bytes))
                using (var mso = new MemoryStream())
                {
                    using (var gs = new GZipStream(mso, CompressionMode.Compress))
                    {
                        msi.CopyTo(gs);
                    }

                    var compressedString = Convert.ToBase64String(mso.ToArray());

                    return compressedString;
                }
            }

            return value;
        }

        public static string Decompress(this string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.IsCompressed())
            {
                return value;
            }

            try
            {
                var bytes = Convert.FromBase64String(value);
                using (var msi = new MemoryStream(bytes))
                using (var mso = new MemoryStream())
                {
                    using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                    {
                        gs.CopyTo(mso);
                    }

                    return Encoding.UTF8.GetString(mso.ToArray());
                }
            }
            catch (InvalidDataException)
            {
                return value;
            }
            catch (FormatException)
            {
                return value;
            }
        }

        public static bool IsCompressed(this string value)
        {
            //Check for the Base64 encoded Gzip header
            const string gzipHeader = "H4sI";
            return value.Substring(0, 4) == gzipHeader;
        }

        public static string CalculatePartition(this string commitId)
        {
            var bytes = Encoding.UTF8.GetBytes(commitId);
            var hash = Crc32Algorithm.Compute(bytes);
            var partition = hash % 655360;

            return partition.ToString();
        }
    }
}
