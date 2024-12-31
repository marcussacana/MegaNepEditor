using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MegaNepEditor
{

    //Used by SENRAN KAGURA Brust Re:Newal
    public class AdvBin
    {
        byte[] Data;

        AdvHeader Header;

        public AdvBin(byte[] data)
        {
            Data = data;
        }

        public string[] Import()
        {
            using (MemoryStream Buffer = new MemoryStream(Data))
            {
                using (var Reader = new StructReader(Buffer))
                {
                    Header = new AdvHeader();
                    Reader.ReadStruct(ref Header);

                    for (var i = 0; i < Header.Sections.Length; i++)
                    {
                        ref var Section = ref Header.Sections[i];
                        Section.Entries = new AdvEntry[Section.Count];
                        Reader.Seek(Section.Offset, SeekOrigin.Begin);
                        for (int x = 0; x < Section.Count; x++)
                        {
                            Reader.ReadStruct(ref Section.Entries[x]);

                            var Offset = Reader.BaseStream.Position;
                            ref var AdvEntry = ref Section.Entries[x];
                            Reader.Seek(AdvEntry.StringOffset, SeekOrigin.Begin);
                            AdvEntry.Content = Reader.ReadString(StringStyle.CString);
                            Reader.Seek(Offset, SeekOrigin.Begin);
                        }
                    }
                }
            }

            return Header.Sections.SelectMany(x => x.Entries.Select(y => y.Content)).ToArray();
        }

        public byte[] Export(string[] Content)
        {
            for (int i = 0, y = 0; i < Header.Sections.Length; i++)
            {
                ref var Section = ref Header.Sections[i];
                for (int x = 0; x < Section.Count; x++)
                {
                    ref var AdvEntry = ref Section.Entries[x];
                    AdvEntry.Content = Content[y++];
                }
            }

            using (MemoryStream buffer = new MemoryStream())
            {
                using (var Writer = new StructWriter(buffer))
                {
                    //Step 1 - Mount the base structure to find the offsets
                    Writer.WriteStruct(ref Header);

                    for (int x = 0; x < Header.Sections.Length; x++)
                    {
                        ref var Section = ref Header.Sections[x];
                        Section.Offset = (int)Writer.BaseStream.Position;
                        for (int i = 0; i < Section.Entries.Length; i++)
                        {
                            ref var Entry = ref Section.Entries[i];
                            Writer.WriteStruct(ref Entry);
                        }
                    }

                    //Step 2 - Create String Table
                    int[] Offsets = new int[Content.Length];
                    for (int x = 0; x < Content.Length; x++)
                    {
                        Offsets[x] = (int)Writer.BaseStream.Position;
                        Writer.Write(Content[x], StringStyle.CString);
                    }

                    //Step 3 - Apply New String Offsets;
                    for (int x = 0, y = 0; x < Header.Sections.Length; x++)
                    {
                        ref var Section = ref Header.Sections[x];
                        for (int i = 0; i < Section.Entries.Length; i++)
                        {
                            ref var Entry = ref Section.Entries[i];
                            Entry.StringOffset = Offsets[y++];
                        }
                    }

                    //Step 4 - Rewrite Headers with updated offsets
                    Writer.BaseStream.Position = 0;

                    Writer.WriteStruct(ref Header);

                    for (int x = 0; x < Header.Sections.Length; x++)
                    {
                        ref var Section = ref Header.Sections[x];
                        Writer.Seek(Section.Offset, SeekOrigin.Begin);
                        for (int i = 0; i < Section.Entries.Length; i++)
                        {
                            ref var Entry = ref Section.Entries[i];
                            Writer.WriteStruct(ref Entry);
                        }
                    }

                }
                return buffer.ToArray();
            }
        }
    }

    struct AdvHeader
    {
        public int Unk;

        [PArray(PrefixType = Const.INT32), StructField]
        public Section[] Sections;
    }

    struct Section
    {
        public int Count;
        public int Offset;

        [Ignore]
        public AdvEntry[] Entries;
    }

    public struct  AdvEntry
    {
        public int UnkA;
        public int UnkB;
        public int UnkC;

        public int StringOffset;

        [FString(Length = 0x10)]
        public string TagNameA;
        [FString(Length = 0x10)]
        public string TagNameB;

        [Ignore]
        public string Content;
    }
}
