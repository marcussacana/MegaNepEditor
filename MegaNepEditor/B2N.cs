using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MegaNepEditor
{

    //Used by SENRAN KAGURA Shinovi Versus
    //This is not an real extension, just a identifier for the CatV2 extracted data
    public class B2N
    {
        byte[] Data;

        B2NEntry[] Entries;

        public B2N(byte[] data)
        {
            Data = data;
        }

        public string[] Import()
        {
            using (var Stream = new MemoryStream(Data))
            {
                StructReader reader = new StructReader(Stream, Encoding: Encoding.Unicode);

                if (reader.ReadInt32() != 0)
                    throw new Exception("Invalid B2N File");

                var Count = reader.ReadUInt32() / 8;

                Entries = new B2NEntry[Count];

                reader.Seek(0, SeekOrigin.Begin);
                for (int i = 0; i < Count; i++)
                {
                    Entries[i] = new B2NEntry();
                    reader.ReadStruct(ref Entries[i]);
                }

                string[] Strings = new string[Count];
                for (int i = 0; i < Count; i++)
                {
                    reader.Seek(Entries[i].Offset, SeekOrigin.Begin);
                    Strings[i] = reader.ReadString(StringStyle.UCString);
                }

                return Strings;
            }
        }

        public byte[] Export(string[] Strings)
        {
            if (Strings.Length != Entries.Length)
                throw new Exception("Invalid String Count");

            using (var Stream = new MemoryStream())
            {
                StructWriter Writer = new StructWriter(Stream, Encoding: Encoding.Unicode);


                uint HeaderSize = (uint)Strings.Length * 8u;

                int[] Offsets = new int[Strings.Length];
                var StringBuffer = new MemoryStream();

                for (int i = 0; i < Strings.Length; i++)
                {
                    Entries[i].Offset = (uint)StringBuffer.Position + HeaderSize;
                    var Data = Encoding.Unicode.GetBytes(Strings[i] + "\x0");
                    StringBuffer.Write(Data, 0, Data.Length);
                }

                for (int i = 0; i < Strings.Length; i++)
                {
                    Writer.WriteStruct(ref Entries[i]);
                }

                Writer.Flush();

                StringBuffer.Position = 0;
                StringBuffer.CopyTo(Stream);

                while (Stream.Length % 0x10 != 0)
                    Stream.WriteByte(0);

                return Stream.ToArray();
            }
        }

        private struct B2NEntry
        {
            public uint Id;
            public uint Offset;
        }
    }

}
