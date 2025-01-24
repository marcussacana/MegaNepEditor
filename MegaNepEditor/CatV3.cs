using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MegaNepEditor
{
    public class CatV3
    {
        public static NamedEntry[] Open(Stream Input)
        {
            var Reader = new StructReader(Input, Encoding: Encoding.UTF8);

            int HeaderSize = Reader.ReadInt32();

            Reader.Seek(0, SeekOrigin.Begin);

            int EntryCount = HeaderSize / 12;

            NamedEntry[] Entries = new NamedEntry[EntryCount];

            for (int i = 0; i < EntryCount; i++)
            {
                Entries[i] = new NamedEntry();
                Reader.ReadStruct(ref Entries[i]);

                if (Entries[i].Offset == -1 && Entries[i].Length == -1)
                {
                    Array.Resize(ref Entries, i);
                    break;
                }

                var oriPos = Reader.BaseStream.Position;

                Reader.Seek(Entries[i].NameOffset, SeekOrigin.Begin);

                Entries[i].FileName =  $"{i}_"  + Reader.ReadString(StringStyle.CString).TrimEnd(' ');

                Reader.Seek(oriPos, SeekOrigin.Begin);

                Entries[i].Content = new VirtStream(Input, Entries[i].Offset, Entries[i].Length);
            }

            return Entries;
        }

        public static void Save(NamedEntry[] Entries, Stream Output)
        {
            try
            {
                Entries = Entries.OrderBy(x => int.Parse(x.FileName.Split('_').First())).ToArray();
            }
            catch
            {
                // ignored
            }

            using (var Writer = new StructWriter(Output, Encoding: Encoding.UTF8))
            {
                Output.SetLength(Entries.Length * 12);
                Output.Seek(0, SeekOrigin.End);

                //Last entry flag
                Writer.Write(-1);
                Writer.Write(-1);
                Writer.Write(0);

                while (Writer.BaseStream.Position % 0x10 != 0)
                    Writer.Write((byte)0);

                for (var i = 0; i < Entries.Length; i++)
                {
                    Entries[i].Offset = (int)Writer.BaseStream.Position;
                    Entries[i].Length = (int)Entries[i].Content.Length;
                    Entries[i].Content.CopyTo(Output);

                    while (Output.Position % 0x10 != 0)
                        Output.WriteByte(0);

                    Entries[i].NameOffset = (int)Output.Position;

                    var fileName = Entries[i].FileName;
                    fileName = fileName.Substring(fileName.IndexOf("_") + 1) + " ";

                    Writer.Write(fileName, StringStyle.CString);

                    if (i + 1 < Entries.Length)
                    {
                        while (Output.Position % 0x10 != 0)
                            Output.WriteByte(0);
                    }
                }

                Writer.Seek(0, SeekOrigin.Begin);

                for (int i = 0; i < Entries.Length; i++)
                {
                    Writer.WriteStruct(ref Entries[i]);
                }

                Writer.Flush();
            }
        }

        public struct NamedEntry
        {
            public int Offset;
            public int Length;
            public int NameOffset;

            [Ignore]
            public string FileName;
            [Ignore]
            public Stream Content;
        }

    }
}
