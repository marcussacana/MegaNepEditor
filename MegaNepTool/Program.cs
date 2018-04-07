using MegaNepEditor;
using System;
using System.IO;
using System.Linq;

namespace MegaNepTool {
    class Program {
        static void Main(string[] args) {
            Console.Title = "MegaNepTool - By Marcussacana";
            if (args?.Length == 0) {
                Console.WriteLine("Drag&Drop .cat files or extracted directory.");
            }

            foreach (string arg in args) {
                if (File.Exists(arg)) {
                    using (Stream Reader = new StreamReader(arg).BaseStream) {
                        Cat Packget = new Cat(Reader);

                        Entry[] Entries = Packget.Open();

                        string OutDir = arg + "~\\";
                        if (!Directory.Exists(OutDir))
                            Directory.CreateDirectory(OutDir);

                        foreach (Entry Entry in Entries) {
                            Console.WriteLine("Extracting: {0}", Entry.FileName);
                            string OutFile = OutDir + Entry.FileName;
                            if (File.Exists(OutFile))
                                File.Delete(OutFile);

                            using (Stream Writer = new StreamWriter(OutFile).BaseStream) {
                                Entry.Content.CopyTo(Writer);
                                Writer.Flush();
                                Writer.Close();
                            }
                        }
                    }
                } else if (Directory.Exists(arg)) {
                    string OriPackget = arg.TrimEnd('\\', '/', ' ', '~');
                    string OutFile = OriPackget + ".new";
                    if (!File.Exists(OriPackget)) {
                        Console.WriteLine("Failed to Repack: \"{0}\" Not Found", OriPackget);
                        continue;
                    }

                    Entry[] Entries = (from x in Directory.GetFiles(arg) select new Entry() {
                        FileName = Path.GetFileName(x),
                        Content = new StreamReader(x).BaseStream
                    }).ToArray();

                    if (File.Exists(OutFile))
                        File.Delete(OutFile);

                    Console.WriteLine("Repacking to {0}, Please Wait...", OutFile);
                    using (Stream NewPackget = new StreamWriter(OutFile).BaseStream)
                    using (Stream Original = new StreamReader(OriPackget).BaseStream) {
                        Cat Manager = new Cat(Original);
                        Manager.Save(Entries, NewPackget);
                    }

                    foreach (Entry Entry in Entries)
                        try {
                            Entry.Content?.Close();
                        } catch { }
                }
            }
            Console.WriteLine("Finished, Press a Key To Exit");
            Console.ReadKey();
        }
    }
}
