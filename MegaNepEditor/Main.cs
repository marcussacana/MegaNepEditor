using AdvancedBinary;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MegaNepEditor {
    public class Cat
    {
        Encoding Encoding = Encoding.GetEncoding(932);
        StructReader Packget;
        uint DataBegin;
        Entry[] Entries = null;
        EntriesHeader Header = new EntriesHeader();
        CatHeader PackgetHeader = new CatHeader();
        public Cat(Stream Packget) {
            this.Packget = new StructReader(Packget, false, Encoding);
        }


        public Entry[] Open() {

            Packget.ReadStruct(ref PackgetHeader);
            
            DataBegin = PackgetHeader.HeaderLen + 0x4;
            Packget.BaseStream.Position = DataBegin;
            
            Packget.ReadStruct(ref Header);

            Entry[] Entries = new Entry[Header.Entries];

            for (uint i = 0; i < Header.Entries; i++) {
                Entry Entry = new Entry();

                if (PackgetHeader.Flags == 0x3) {
                    Packget.Seek(Header.NamesOffset[i] + PackgetHeader.HeaderLen, SeekOrigin.Begin);
                    Entry.FileName = Packget.ReadString(StringStyle.CString);
                } else
                    Entry.FileName = i.ToString("X8") + ".bin";

                Entry.Content = new VirtStream(Packget.BaseStream, Header.Offsets[i] + PackgetHeader.HeaderLen, Header.Lengths[i]);

                Entries[i] = Entry;
            }

            this.Entries = Entries;
            return Entries;
        }

        public void Save(Entry[] Entries, Stream Output) {
            if (this.Entries == null)
                Open();

            if (this.Entries.LongLength != Entries.LongLength)
                throw new Exception("You can't add or remove files from the packget.");

            Packget.BaseStream.Position = 0x00;
            byte[] Buffer = new byte[DataBegin];
            if (Packget.Read(Buffer, 0, Buffer.Length) != Buffer.Length)
                throw new Exception("Failed to Copy the Static Header Data.");

            StructWriter Writer = new StructWriter(Output, false, Encoding);
            Writer.Write(Buffer, 0, Buffer.Length);

            //Recovery File Order
            if (PackgetHeader.Flags != 0x3) {
                Entries = Entries.OrderBy(x => Convert.ToUInt32(Path.GetFileNameWithoutExtension(x.FileName).Trim(), 16)).ToArray();
            }

            uint ContentBegin = Header.Offsets.First() + PackgetHeader.HeaderLen;
            uint BufferPos = ContentBegin;
            for (uint i = 0; i < Header.Offsets.Length; i++) { 
                uint Length = (uint)Entries[i].Content.Length;
                Header.Offsets[i] = BufferPos - PackgetHeader.HeaderLen;
                Header.Lengths[i] = Length;
                BufferPos += Length + (uint)AsserionRequired(Length);

                if (PackgetHeader.Flags == 0x3) {
                    Header.NamesOffset[i] = BufferPos - PackgetHeader.HeaderLen;

                    uint StrLen = (uint)Encoding.GetByteCount(Entries[i].FileName + "\x0");
                    StrLen += (uint)AsserionRequired(StrLen);

                    BufferPos += StrLen;
                }

            }

            Writer.WriteStruct(ref Header);

            if (Writer.BaseStream.Position < ContentBegin) {
                Buffer = new byte[ContentBegin - Writer.BaseStream.Position];
                Packget.BaseStream.Position = Writer.BaseStream.Position;
                if (Packget.Read(Buffer, 0, Buffer.Length) != Buffer.Length)
                    throw new Exception("Failed to copy unk data");

                Writer.Write(Buffer, 0, Buffer.Length);
            }

            Writer.BaseStream.Position = ContentBegin;

            for (uint i = 0; i < Header.Offsets.Length; i++) {
                Entries[i].Content.CopyTo(Writer.BaseStream);
                byte[] Assertion = new byte[AsserionRequired(Writer.BaseStream.Position)];
                Writer.Write(Assertion);


                if (PackgetHeader.Flags == 0x3) {
                    byte[] Data = Encoding.GetBytes(Entries[i].FileName + "\x0");                    
                    Assertion = new byte[AsserionRequired(Data.Length)];
                    Writer.Write(Data);
                    Writer.Write(Assertion);
                }
            }

            Writer.Flush();
        }

        public long AsserionRequired(long Value) {
            return 0x10 - (Value % 0x10);
        }
    }

    public struct Entry {
        public string FileName;
        public Stream Content;
    }

    public struct CatHeader {
        public uint Flags;//Not Sure, Maybe Version
        public uint UnkCount;
        public uint Unk2;
        public uint HeaderLen;   
    }

    public struct EntriesHeader {
        public uint Entries;
        public uint Unk1;
        public uint Unk2;
        public uint Unk3;

        [RArray(FieldName = "Entries")]
        public uint[] Offsets;//+CatHeader.HeaderLen Needed
        [RArray(FieldName = "Entries")]
        public uint[] Lengths;
        [RArray(FieldName = "Entries")]
        public uint[] NamesOffset;//CString, Only exist when CatHeader.Flags == 0x3?
    }
}
