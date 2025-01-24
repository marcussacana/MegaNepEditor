using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MegaNepEditor
{
    public class CatV2
    {

        public static Entry[] Open(Stream Input) {

            List<Entry> Entries = new List<Entry>();
            using (BinaryReader reader = new BinaryReader(Input))
            { 
                int readPos = 0;
                while (true) {
                    reader.BaseStream.Position = readPos;
                    readPos += 4;

                    int Begin = reader.ReadInt32();
                    int End = reader.ReadInt32();


                    int Length;

                    if (End == -1)
                        Length = (int)Input.Length - Begin;
                    else
                        Length = End - Begin;


                    reader.BaseStream.Position = Begin;

                    byte[] Data = reader.ReadBytes(Length);

                    Entry Entry = new Entry() 
                    {
                        Content = new MemoryStream(Data),
                        FileName = Entries.Count.ToString("X8") + ".b2n"
                    };

                    Entries.Add(Entry);

                    if (End == -1)
                        break;
                }
            }

            return Entries.ToArray();
        }

        public static void Save(Entry[] Entries, Stream Output)
        {

            Entries = Entries.OrderBy(x => Convert.ToUInt32(Path.GetFileNameWithoutExtension(x.FileName).Trim(), 16)).ToArray();

            using (BinaryWriter writer = new BinaryWriter(Output))
            {
                int Offset = Entries.Length * 4;
                Offset += 0x10 - (Offset % 0x10);

                foreach (Entry Entry in Entries)
                {
                    writer.Write(Offset);
                    Offset += (int)Entry.Content.Length;
                }

                writer.Write(-1);

                writer.Flush();

                while (writer.BaseStream.Position % 0x10 != 0) { 
                    writer.Write((byte)0);
                    writer.Flush();
                }

                foreach (Entry Entry in Entries)
                {
                    Entry.Content.CopyTo(Output);
                }
            }
        }
    }
}
