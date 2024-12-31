using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MegaNepEditor
{

    //Used by SENRAN KAGURA Brust Re:Newal
    public class BIN
    {
        public byte[] data;

        StrInfo strInfo;

        public BIN(byte[] Script)
        {
            data = Script;
        }


        public string[] Import()
        {
            List<string> list = new List<string>();
            using (var buffer = new MemoryStream(data)) {
                using (var reader = new StructReader(buffer))
                {
                    var format = new StrEntry();
                    reader.ReadStruct(ref format);

                    strInfo = new StrInfo();
                    strInfo.Count = format.Offset / 0xC;

                    reader.Seek(0, SeekOrigin.Begin);
                    reader.ReadStruct(ref strInfo);

                    foreach (var Entry in strInfo.Entries)
                    {
                        reader.Seek(Entry.Offset, SeekOrigin.Begin);
                        list.Add(reader.ReadString(StringStyle.CString));
                    }
                }
            }

            return list.ToArray();
        }

        public byte[] Export(string[] Lines)
        {
            using (MemoryStream Buffer = new MemoryStream())
            {
                var tableSize = strInfo.Count * 0xC;
                for (uint i = 0; i < strInfo.Count; i++)
                {
                    var Data = Encoding.UTF8.GetBytes(Lines[i] + "\x0");
                    strInfo.Entries[i].Offset = (uint)(tableSize + Buffer.Position);

                    Buffer.Write(Data, 0, Data.Length);
                }

                var StringData = Buffer.ToArray();

                Buffer.Position = 0;

                using (var writer = new StructWriter(Buffer))
                {
                    writer.WriteStruct(ref strInfo);
                    writer.Write(StringData, 0, StringData.Length);
                }

                return Buffer.ToArray();
            }
        }


        public struct StrInfo
        {
            [Ignore]
            public uint Count;

            [StructField, RArray(FieldName = nameof(Count))]
            public StrEntry[] Entries;
        }

        public struct StrEntry
        {
            public uint Unk;
            public uint ID;
            public uint Offset;
        }
    }
}
