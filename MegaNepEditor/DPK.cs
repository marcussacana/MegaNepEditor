using AdvancedBinary;
using System;
using System.IO;
using System.Linq;

namespace MegaNepEditor
{
    //Data Pack, the extension/name is something that I gave and isn't oficial.
    public static class DPK
    {

        public static bool IsDPK(this Stream Buffer)
        {
            var InitialPosition = Buffer.Position;
            var Reader = new BinaryReader(Buffer);

            int HSize = Reader.ReadInt32();
            int Count = Reader.ReadInt32();
            int Size = Reader.ReadInt32();
            int FirstOffset = Reader.ReadInt32();

            Buffer.Position = InitialPosition;

            if (HSize == (Count * 4) + 12 && Size == Buffer.Length && FirstOffset == 0)
                return true;
            return false;
        }
        public static Entry[] Open(Stream Package)
        {
            var Header = new DPKHeader();
            
            StructReader Reader = new StructReader(Package);
            Reader.ReadStruct(ref Header);

            var Entries = Header.Offsets.Select((x, i) => {
                bool IsLast = i == Header.EntriesCount - 1;
                uint FileSize = IsLast ? Header.DPKSize - (x + Header.HeaderSize) : Header.Offsets[i + 1] - x;
                
                Reader.Seek(x + Header.HeaderSize, SeekOrigin.Begin);
                var Signature = Reader.ReadInt32();

                var NewEntry = new Entry
                {
                    Content = new VirtStream(Package, x + Header.HeaderSize, FileSize),
                    FileName = i.ToString("X8") + ExtHelper.GetExtension(Signature)
                };

                return NewEntry;
            }).ToArray();

            return Entries;
        }
        
        public static void Save(Entry[] Entries, Stream Output)
        {
            var SortedEntries = Entries.OrderBy(x => Convert.ToInt32(Path.GetFileNameWithoutExtension(x.FileName), 16)).ToArray();

            var Writer = new StructWriter(Output);

            DPKHeader Header = new DPKHeader();
            Header.HeaderSize = (uint)(Entries.Length * 4) + 12;
            Header.EntriesCount = (uint)Entries.Length;
            Header.DPKSize = (uint)(Header.HeaderSize + Entries.Sum(x => x.Content.Length));

            Header.Offsets = new uint[Header.EntriesCount];
            for (long i = 0, x = 0; i < Header.EntriesCount; i++)
            {
                Header.Offsets[i] = (uint)x;
                x += SortedEntries[i].Content.Length;
            }


            Writer.WriteStruct(ref Header);
            Writer.Flush();

            for (int i = 0; i < Header.Offsets.Length; i++)
            {
                var Entry = SortedEntries[i].Content;
                Entry.Position = 0;

                Entry.CopyTo(Writer.BaseStream);
            }

            Writer.BaseStream.Flush();
        }

        public struct Entry
        {
            public Stream Content;
            public string FileName;
        }
    }

    struct DPKHeader
    {
        public uint HeaderSize;
        public uint EntriesCount;
        public uint DPKSize;

        [RArray(FieldName = "EntriesCount")]
        public uint[] Offsets;
    }
}
