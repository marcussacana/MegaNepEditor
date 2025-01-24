using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MegaNepEditor
{
    public class FontSizeParser
    {

        byte[] Data;
        public FontSizeParser(byte[] SizeData)
        {
            Data = SizeData;
        }

        public string[] Import()
        {
            using (var Buffer = new MemoryStream(Data))
            {
                using (var Reader = new StructReader(Buffer, false, Encoding.Unicode))
                {
                    var Info = new FontSizeInfo();
                    Reader.ReadStruct(ref Info);

                    var CharList = new string[Info.EntryCount];
                    for (var i = 0; i < Info.EntryCount; i++)
                    {
                        var Entry = Info.Entries[i];
                        CharList[i] = $"{Entry.Char}\t{Entry.Width}\t{Entry.Height}";
                    }

                    return CharList;
                }
            }
        }

        public byte[] Export(string[] Content) {
            using (var Buffer = new MemoryStream())
            {
                using (var Writer = new StructWriter(Buffer, false, Encoding.Unicode))
                {
                    List<FontSizeEntry> Entries = new List<FontSizeEntry>();
                    foreach (var Line in Content)
                    {
                        var Parts = Line.Substring(1).Split('\t');
                        var Entry = new FontSizeEntry();
                        Entry.Char = Line.First().ToString();
                        Entry.Width = ushort.Parse(Parts[1]);
                        Entry.Height = ushort.Parse(Parts[2]);
                        Entries.Add(Entry);
                    }

                    var Info = new FontSizeInfo();
                    Info.EntryCount = (uint)Entries.Count;
                    Info.Entries = Entries.ToArray();

                    Writer.WriteStruct(ref Info);
                    Writer.Flush();

                    return Buffer.ToArray();
                }
            }
        }

        public struct FontSizeEntry
        {
            [FString(Length = 2)]
            public string Char;
            public ushort Width;
            public ushort Height;
        }

        public struct FontSizeInfo
        {
            public uint EntryCount;
            public uint AssetionA;
            public ulong AssetionB;

            [RArray(FieldName = nameof(EntryCount)), StructField]
            public FontSizeEntry[] Entries;
        }
    }
}
