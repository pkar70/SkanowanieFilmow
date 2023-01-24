using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

// https://www.codeproject.com/KB/Articles/5251929/ExifData_1.6.zip
// plik mainpage.cs

namespace CompactExifLib
{
    public class FileExif2String
    {
        /// <summary>
        /// robi pełny DUMP danych z pliku (wczytując plik); na razie tylko JPG
        /// </summary>
        public static string GetString(string ImageFileName)
        {
            if (!System.IO.File.Exists(ImageFileName)) return "";
        if(System.IO.Path.GetExtension(ImageFileName).ToLowerInvariant() != ".jpg") return "";

            try
            {
                ExifData d = new ExifData(ImageFileName);

                StringBuilder sb = new StringBuilder(200000);
                sb.Append("File name:  ");
                sb.Append(ImageFileName);
                sb.Append("\n");
                PrintByteOrder(sb, d);
                sb.Append("\n");

                PrintIfdData(sb, ExifIfd.PrimaryData, d);
                PrintIfdData(sb, ExifIfd.PrivateData, d);
                PrintIfdData(sb, ExifIfd.GpsInfoData, d);
                PrintIfdData(sb, ExifIfd.Interoperability, d);
                PrintIfdData(sb, ExifIfd.ThumbnailData, d);
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Catch: {ex.Message}";
            }


        }

        private static void PrintByteOrder(StringBuilder sb, ExifData d)
        {
            sb.Append("Byte order: ");
            if (d.ByteOrder == ExifByteOrder.LittleEndian)
            {
                sb.Append("Little Endian");
            }
            else sb.Append("Big Endian");
            sb.Append("\n");
        }


        private static void PrintIfdData(StringBuilder sb, ExifIfd Ifd, ExifData d)
        {
            const int MaxContentLength = 35; // Maximum character count for the content length
            const int MaxRawDataOutputCount = 40; // Maximum number of bytes for the raw data output
            ExifTagType TagType;
            ExifTag TagSpec;
            int ValueCount, TagDataIndex, TagDataByteCount;
            byte[] TagData;
            ExifTagId TagId;

            sb.Append("--- IFD ");
            sb.Append(Ifd.ToString());
            sb.Append(" ---\n");
            bool HeaderPrinted = false;
            d.InitTagEnumeration(Ifd);
            while (d.EnumerateNextTag(out TagSpec))
            {
                if (!HeaderPrinted)
                {
                    sb.Append("Name                       ID      Type        Value   Byte   ");
                    AlignedAppend(sb, "Content", MaxContentLength + 2);
                    sb.Append("Raw data\n");
                    sb.Append("                                               count   count\n");
                    HeaderPrinted = true;
                }

                d.GetTagRawData(TagSpec, out TagType, out ValueCount, out TagData, out TagDataIndex);
                AlignedAppend(sb, GetExifTagName(TagSpec), 27);

                TagId = ExifData.ExtractTagId(TagSpec);
                sb.Append("0x");
                sb.Append(((ushort)TagId).ToString("X4"));
                sb.Append("  ");

                AlignedAppend(sb, TagType.ToString(), 9);
                sb.Append("  ");
                AlignedAppend(sb, ValueCount.ToString(), 6, true);
                sb.Append("  ");

                TagDataByteCount = ExifData.GetTagByteCount(TagType, ValueCount);
                AlignedAppend(sb, TagDataByteCount.ToString("D"), 6, true);
                sb.Append("  ");

                AppendInterpretedContent(sb, d, TagSpec, TagType, MaxContentLength);
                sb.Append("  ");

                int k = TagDataByteCount;
                if (k > MaxRawDataOutputCount) k = MaxRawDataOutputCount;
                for (int i = 0; i < k; i++)
                {
                    sb.Append(TagData[TagDataIndex + i].ToString("X2"));
                    sb.Append(" ");
                }
                if (k < TagDataByteCount) sb.Append("…");
                sb.Append("\n");
            }
            sb.Append("\n");
        }


        private static string GetExifTagName(ExifTag TagSpec)
        {
            string s = TagSpec.ToString();
            if ((s.Length > 0) && (s[0] >= '0') && (s[0] <= '9'))
            {
                s = "???"; // If the name starts with a digit there is no name defined in the enum type "ExifTag"
            }
            return (s);
        }


        private static void AppendInterpretedContent(StringBuilder sb, ExifData d, ExifTag TagSpec, ExifTagType TagType, int Length)
        {
            string s = "";

            try
            {
                if (TagType == ExifTagType.Ascii)
                {
                    if (!d.GetTagValue(TagSpec, out s, StrCoding.Utf8)) s = "???";
                }
                else if ((TagType == ExifTagType.Byte) && ((TagSpec == ExifTag.XpTitle) || (TagSpec == ExifTag.XpComment) || (TagSpec == ExifTag.XpAuthor) ||
                         (TagSpec == ExifTag.XpKeywords) || (TagSpec == ExifTag.XpSubject)))
                {
                    if (!d.GetTagValue(TagSpec, out s, StrCoding.Utf16Le_Byte)) s = "???";
                }
                else if ((TagType == ExifTagType.Undefined) && (TagSpec == ExifTag.UserComment))
                {
                    if (!d.GetTagValue(TagSpec, out s, StrCoding.IdCode_Utf16)) s = "???";
                }
                else if ((TagType == ExifTagType.Undefined) && ((TagSpec == ExifTag.ExifVersion) || (TagSpec == ExifTag.FlashPixVersion) ||
                         (TagSpec == ExifTag.InteroperabilityVersion)))
                {
                    if (!d.GetTagValue(TagSpec, out s, StrCoding.UsAscii_Undef)) s = "???";
                }
                else if ((TagType == ExifTagType.Undefined) && ((TagSpec == ExifTag.SceneType) || (TagSpec == ExifTag.FileSource)))
                {
                    d.GetTagRawData(TagSpec, out _, out _, out byte[] RawData);
                    if (RawData.Length > 0) s += RawData[0].ToString();
                }
                else if ((TagType == ExifTagType.Byte) || (TagType == ExifTagType.UShort) || (TagType == ExifTagType.ULong))
                {
                    d.GetTagValueCount(TagSpec, out int k);
                    for (int i = 0; i < k; i++)
                    {
                        d.GetTagValue(TagSpec, out uint v, i);
                        if (i > 0) s += ", ";
                        s += v.ToString();
                    }
                }
                else if (TagType == ExifTagType.SLong)
                {
                    d.GetTagValueCount(TagSpec, out int k);
                    for (int i = 0; i < k; i++)
                    {
                        d.GetTagValue(TagSpec, out int v, i);
                        if (i > 0) s += ", ";
                        s += v.ToString();
                    }
                }
                else if ((TagType == ExifTagType.SRational) || (TagType == ExifTagType.URational))
                {
                    d.GetTagValueCount(TagSpec, out int k);
                    for (int i = 0; i < k; i++)
                    {
                        d.GetTagValue(TagSpec, out ExifRational v, i);
                        if (i > 0) s += ", ";
                        s += v.ToString();
                    }
                }
            }
            catch
            {
                s = "!Error!";
            }
            s = s.Replace('\r', ' ');
            s = s.Replace('\n', ' ');
            AlignedAppend(sb, s, Length);
        }


        private static void AlignedAppend(StringBuilder sb, string s, int CharCount, bool RightAlign = false)
        {
            if (s.Length <= CharCount)
            {
                int SpaceCount = CharCount - s.Length;
                if (RightAlign) sb.Append(' ', SpaceCount);
                sb.Append(s);
                if (!RightAlign) sb.Append(' ', SpaceCount);
            }
            else
            {
                sb.Append(s.Substring(0, CharCount - 1));
                sb.Append('…');
            }
        }

    }

}