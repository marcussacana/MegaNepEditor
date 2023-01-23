using AdvancedBinary;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MegaNepEditor {
    public class Cat
    {
        Encoding Encoding = Encoding.GetEncoding(932);
        StructReader Package;
        uint DataBegin;
        Entry[] Entries = null;
        EntriesHeader Header = new EntriesHeader();
        CatHeader PackageHeader = new CatHeader();
        public Cat(Stream Package) {
            this.Package = new StructReader(Package, false, Encoding);
        }


        public Entry[] Open() {

            Package.ReadStruct(ref PackageHeader);
            
            DataBegin = PackageHeader.HeaderLen + 0x4;
            Package.BaseStream.Position = DataBegin;
            
            Package.ReadStruct(ref Header);

            Entry[] Entries = new Entry[Header.Entries];

            for (uint i = 0; i < Header.Entries; i++) {
                Entry Entry = new Entry();

                Entry.Content = new VirtStream(Package.BaseStream, Header.Offsets[i] + PackageHeader.HeaderLen, Header.Lengths[i]);

                bool IsDPK = Entry.Content.IsDPK();

                var Buffer = new byte[4];
                Entry.Content.Read(Buffer, 0, 4);
                Entry.Content.Position = 0;

                int Magic = BitConverter.ToInt32(Buffer, 0);

                if (PackageHeader.Flags == 0x3) {
                    Package.Seek(Header.NamesOffset[i] + PackageHeader.HeaderLen, SeekOrigin.Begin);
                    Entry.FileName = Package.ReadString(StringStyle.CString);
                } else
                    Entry.FileName = i.ToString("X8") + (IsDPK ? ".dpk" : ExtHelper.GetExtension(Magic));

                Entries[i] = Entry;
            }

            this.Entries = Entries;
            return Entries;
        }

        public void Save(Entry[] Entries, Stream Output) {
            if (this.Entries == null)
                Open();

            if (this.Entries.LongLength != Entries.LongLength)
                throw new Exception("You can't add or remove files from the package.");

            Package.BaseStream.Position = 0x00;
            byte[] Buffer = new byte[DataBegin];
            if (Package.Read(Buffer, 0, Buffer.Length) != Buffer.Length)
                throw new Exception("Failed to Copy the Static Header Data.");

            StructWriter Writer = new StructWriter(Output, false, Encoding);
            Writer.Write(Buffer, 0, Buffer.Length);

            //Recovery File Order
            if (PackageHeader.Flags != 0x3) {
                Entries = Entries.OrderBy(x => Convert.ToUInt32(Path.GetFileNameWithoutExtension(x.FileName).Trim(), 16)).ToArray();
            }

            uint ContentBegin = Header.Offsets.First() + PackageHeader.HeaderLen;
            uint BufferPos = ContentBegin;
            for (uint i = 0; i < Header.Offsets.Length; i++) { 
                uint Length = (uint)Entries[i].Content.Length;
                Header.Offsets[i] = BufferPos - PackageHeader.HeaderLen;
                Header.Lengths[i] = Length;
                BufferPos += Length + (uint)AsserionRequired(Length);

                if (PackageHeader.Flags == 0x3) {
                    Header.NamesOffset[i] = BufferPos - PackageHeader.HeaderLen;

                    uint StrLen = (uint)Encoding.GetByteCount(Entries[i].FileName + "\x0");
                    StrLen += (uint)AsserionRequired(StrLen);

                    BufferPos += StrLen;
                }
            }

            Writer.WriteStruct(ref Header);

            if (Writer.BaseStream.Position < ContentBegin) {
                Buffer = new byte[ContentBegin - Writer.BaseStream.Position];
                Package.BaseStream.Position = Writer.BaseStream.Position;
                if (Package.Read(Buffer, 0, Buffer.Length) != Buffer.Length)
                    throw new Exception("Failed to copy unk data");

                Writer.Write(Buffer, 0, Buffer.Length);
            }

            Writer.BaseStream.Position = ContentBegin;

            for (uint i = 0; i < Header.Offsets.Length; i++) {
                Entries[i].Content.CopyTo(Writer.BaseStream);
                byte[] Assertion = new byte[AsserionRequired(Writer.BaseStream.Position)];
                Writer.Write(Assertion);


                if (PackageHeader.Flags == 0x3) {
                    byte[] Data = Encoding.GetBytes(Entries[i].FileName + "\x0");                    
                    Assertion = new byte[AsserionRequired(Data.Length)];
                    Writer.Write(Data);
                    Writer.Write(Assertion);
                }
            }

            uint OriDataEnd = Header.Lengths.Last() + Header.Offsets.Last() + PackageHeader.HeaderLen;
            uint NewDataEnd = (uint)Writer.BaseStream.Position;


            Writer.BaseStream.Position = 0x10;


            byte[] tmp = new byte[4];
            Writer.BaseStream.Read(tmp, 0, tmp.Length);
            Writer.BaseStream.Position -= 4;

            int UnkOffset = BitConverter.ToInt32(tmp, 0);
            UnkOffset += ((int)NewDataEnd - (int)OriDataEnd);

            BitConverter.GetBytes(UnkOffset).CopyTo(tmp, 0);
            Writer.BaseStream.Write(tmp, 0, tmp.Length);

            int ReamingData = ((int)NewDataEnd - (int)OriDataEnd);

            if (PackageHeader.UnkCount == 0x2 && ReamingData > 0)
            {

                Writer.BaseStream.Read(tmp, 0, tmp.Length);
                Writer.BaseStream.Position -= 4;

                UnkOffset = BitConverter.ToInt32(tmp, 0);
                UnkOffset += ((int)NewDataEnd - (int)OriDataEnd);

                BitConverter.GetBytes(UnkOffset).CopyTo(tmp, 0);
                Writer.BaseStream.Write(tmp, 0, tmp.Length);
            }

            if (ReamingData > 0)
            {
                Package.BaseStream.Position = OriDataEnd;
                Writer.BaseStream.Position = NewDataEnd;

                Package.BaseStream.CopyTo(Writer.BaseStream);
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
        public uint UnkCount; // Section Count?
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
