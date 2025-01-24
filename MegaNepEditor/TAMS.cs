using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MegaNepEditor
{
    public class TAMS
    {
        TAMSHeader Header;
        byte[] Data;

        public TAMS(byte[] data)
        {
            Data = data;
        }

        public static bool IsTAMS(byte[] data)
        {
            try
            {
                using (var Buffer = new MemoryStream(data))
                {
                    using (var Reader = new StructReader(Buffer, false, Encoding.Unicode))
                    {
                        var Header = new TAMSHeaderQuery();
                        Reader.ReadStruct(ref Header);
                        return Header.Signature == 0x534D4154;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public string[] Import()
        {
            using (var Buffer = new MemoryStream(Data))
            {
                using (var Reader = new StructReader(Buffer, false, Encoding.Unicode))
                {
                    Header = new TAMSHeader();
                    Reader.ReadStruct(ref Header);

                    if (Header.Signature != 0x534D4154)
                        throw new Exception("Invalid TAMS file");

                    bool skipFirstOffset = Header.Flags == 1;

                    var Strings = new string[Header.StringCount];
                    for (int i = 0; i < Header.StringCount; i++)
                    {
                        if (i == 0 && skipFirstOffset)
                        {
                            Strings[i] = string.Empty;
                            continue;
                        }

                        Reader.Seek(Header.StringOffsets[i], SeekOrigin.Begin);
                        Strings[i] = EscapeString(Reader.ReadString(StringStyle.UCString));
                    }

                    return Strings;
                }
            }
        }

        public byte[] Export(string[] Content)
        {
            using (var Buffer = new MemoryStream())
            {
                Buffer.Write(Data, 0, Data.Length);
                using (var Writer = new StructWriter(Buffer, false, Encoding.Unicode))
                {
                    Writer.Seek(Header.StringTableOffset, SeekOrigin.Begin);

                    bool skipFirstOffset = Header.Flags == 1;

                    for (int i = 0; i < Content.Length; i++)
                    {
                        if (i == 0 && skipFirstOffset)
                        {
                            Header.StringOffsets[i] = 0;
                            continue;
                        }

                        Header.StringOffsets[i] = (ushort)Buffer.Position;
                        Writer.Write(UnescapeString(Content[i]), StringStyle.UCString);
                    }

                    Writer.Seek(0, SeekOrigin.Begin);
                    Writer.WriteStruct(ref Header);
                    return Buffer.ToArray();
                }
            }
        }

        private string UnescapeString(string str)
        {
            var Unescaped = str
                .Replace("<EOD>", "\x25A1")
                .Replace("<WAIT_CLICK>", "▽")
                .Replace("<MID>", "\x7m");//???

            Unescaped = SimpleUnscape("VOICE", Unescaped);
            Unescaped = SimpleUnscape("SFX", Unescaped);
            Unescaped = SimpleUnscape("WAIT", Unescaped);
            Unescaped = SimpleUnscape("EFFECT", Unescaped);
            Unescaped = SimpleUnscape("YOMI", Unescaped);
            Unescaped = SimpleUnscape("HZ", Unescaped);
            Unescaped = SimpleUnscape("CF", Unescaped);
            Unescaped = SimpleUnscape("P", Unescaped);

            return Unescaped;
        }

        private string SimpleUnscape(string CMD, string Unescaped)
        {
            while (Unescaped.Contains($"<{CMD}"))
            {
                var Index = Unescaped.IndexOf($"<{CMD}");
                var Prefix = Unescaped.Substring(0, Index);
                var LineBegin = Unescaped.IndexOf(">", Index) + 1;
                var Cmd = Unescaped.Substring(Index, LineBegin - Index);
                Cmd = Cmd.Replace($"<{CMD} ", "\x7").Replace(">", "");
                Unescaped = Unescaped.Substring(LineBegin);

                Unescaped = Prefix + Cmd + Unescaped;
            }

            return Unescaped;
        }

        private string EscapeString(string str)
        {
            var Escaped = str
                .Replace("\x25A1", "<EOD>")
                .Replace("\x7m", "<MID>")//Center text???
                .Replace("▽", "<WAIT_CLICK>");

            Escaped = SimpleEscape("v", 9, "VOICE", Escaped);
            Escaped = SimpleEscape("s", 9, "SFX", Escaped);
            Escaped = SimpleEscape("w", 5, "WAIT", Escaped);
            Escaped = SimpleEscape("e", 7, "EFFECT", Escaped);
            Escaped = SimpleEscape("y", 8, "YOMI", Escaped);
            Escaped = SimpleEscape("c", 11, "CF", Escaped);
            Escaped = SimpleEscape("p", 4, "P", Escaped);
            Escaped = SimpleEscape("h", 7, "HZ", Escaped);//Typewrite effect???

            return Escaped;
        }

        private string SimpleEscape(string CmdPrefix, int CmdLen, string Tag, string Escaped)
        {

            while (Escaped.Contains($"\x7{CmdPrefix}"))
            {
                var Index = Escaped.IndexOf($"\x7{CmdPrefix}");

                var Prefix = Escaped.Substring(0, Index);

                var LineBegin = Index + CmdLen;//Fixed Size

                var Cmd = Escaped.Substring(Index, LineBegin - Index);
                Cmd = Cmd.Replace("\x7", $"<{Tag} ") + ">";

                Escaped = Escaped.Substring(LineBegin);

                Escaped = Prefix + Cmd + Escaped;
            }


            return Escaped;
        }

        public struct TAMSHeaderQuery
        {
            public int Signature;
            public ushort Version;//???
            public ushort OPCount;
            public ushort StringCount;
            public ushort Flags;//1 = 32bit string table offset??
        }

        public struct TAMSHeader
        {
            public int Signature;
            public ushort Version;//???
            public ushort OPCount;
            public ushort StringCount;
            public ushort Flags;//1 = 32bit string table offset??

            public ushort OpcodeTableOffset;
            public ushort StringTableOffset;

            [RArray(FieldName = nameof(StringCount))]
            public ushort[] StringOffsets;
            [RArray(FieldName = nameof(OPCount))]
            public ushort[] OpcodeOffsets;
        }
    }
}