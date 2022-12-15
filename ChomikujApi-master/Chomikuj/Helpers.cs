using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Chomikuj.Extensions;
using Chomikuj.Rest;
using Newtonsoft.Json;

namespace Chomikuj
{
    internal static class Helpers
    {
        public static string GetVerificationToken(this IRestHandler client)
        {
            return client.GetCookies(new Uri(ChomikujBase.MainAddress))[3].Value;
        }

        public static Dictionary<string, string> ToJsonDictionary(this Response result)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(result.Content);
        }

        public static byte[] ToBytes(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public static Stream CombineStreams(IEnumerable<Stream> streams)
        {
            return new ConcatenatedStream(streams);
        }

        public static DateTime ParseChomikujDate(string date)
        {
            return DateTime.ParseExact(date, "d MMM yy HH:mm", GetCultureInfo());
        }

        public static CultureInfo GetCultureInfo()
        {
            return CultureInfo.GetCultureInfo("PL-pl");
        }

        public static double ParseFileSize(string value, string unit)
        {
            switch (unit.ToUpper())
            {
                case "KB":
                    return double.Parse(value, GetCultureInfo());
                case "MB":
                    return double.Parse(value, GetCultureInfo()) * 1024;
                case "GB":
                    return double.Parse(value, GetCultureInfo()) * 1024 * 1024;
                default:
                    throw new FormatException("Invalid unit: " + unit);
            }
        }

        public static ChomikujFileType GetFileType(string fileType)
        {
            switch (fileType.ToLower())
            {
                case "mpg":
                    return ChomikujFileType.Video;
                case "mp3":
                    return ChomikujFileType.Audio;
                case "jpg":
                case "gif":
                    return ChomikujFileType.Image;
                case "zip":
                    return ChomikujFileType.Archive;
                case "exe":
                    return ChomikujFileType.Program;
                case "pdf":
                case "htm":
                case "txt":
                case "ppt":
                case "doc":
                case "xls":
                    return ChomikujFileType.Document;
                default:
                    return ChomikujFileType.Unknown;
            }
        }

        private class ConcatenatedStream : Stream
        {
            private readonly Queue<Stream> _streams;

            public ConcatenatedStream(IEnumerable<Stream> streams)
            {
                _streams = new Queue<Stream>(streams);
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_streams.Count == 0)
                    return 0;

                int bytesRead = _streams.Peek().Read(buffer, offset, count);
                if (bytesRead == 0)
                {
                    _streams.Dequeue().Dispose();
                    bytesRead += Read(buffer, offset + bytesRead, count - bytesRead);
                }
                return bytesRead;
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override long Length
            {
                get { throw new NotImplementedException(); }
            }

            public override long Position
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            protected override void Dispose(bool disposing)
            {
                Array.ForEach(_streams.ToArray(), stream => stream.Dispose());
            }

            public override void Close()
            {
                Array.ForEach(_streams.ToArray(), stream => stream.Close());
            }
        }
    }
}
