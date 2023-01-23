using MegaNepEditor;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace MegaNepTool {
    class Program {
        static void Main(string[] args) {
            Console.Title = "MegaNepTool - By Marcussacana";
            if (args?.Length == 0) {
                Console.WriteLine("Drag&Drop .cat/.dpk files or extracted directory.");
            }

            foreach (string arg in args) {
                if (File.Exists(arg) && arg.ToLowerInvariant().EndsWith(".cat"))
                {
                    ExtractCat(arg);
                }
                else if (File.Exists(arg) && arg.ToLowerInvariant().EndsWith(".dpk"))
                {
                    ExtractDPK(arg);
                }
                else if (Directory.Exists(arg))
                {
                    PackCat(arg);
                }
            }
            Console.WriteLine("Finished, Press a Key To Exit");
            Console.ReadKey();
        }

        public static void ExtractDPK(string DPKPath)
        {
            using (Stream Reader = new StreamReader(DPKPath).BaseStream)
            {

                var Entries = DPK.Open(Reader);

                string OutDir = DPKPath + "~\\";
                if (!Directory.Exists(OutDir))
                    Directory.CreateDirectory(OutDir);

                foreach (var Entry in Entries)
                {
                    Console.WriteLine("Extracting: {0}", Entry.FileName);
                    string OutFile = OutDir + Entry.FileName;
                    if (File.Exists(OutFile))
                        File.Delete(OutFile);

                    using (Stream Writer = new StreamWriter(OutFile).BaseStream)
                    {
                        Entry.Content.CopyTo(Writer);
                        Writer.Flush();
                        Writer.Close();
                    }
                }
            }
        }

        public static void ExtractCat(string Cat)
        {
            using (Stream Reader = new StreamReader(Cat).BaseStream)
            {
                Cat Packget = new Cat(Reader);

                Entry[] Entries = Packget.Open();

                string OutDir = Cat + "~\\";
                if (!Directory.Exists(OutDir))
                    Directory.CreateDirectory(OutDir);

                foreach (Entry Entry in Entries)
                {
                    Console.WriteLine("Extracting: {0}", Entry.FileName);
                    string OutFile = OutDir + Entry.FileName;
                    if (File.Exists(OutFile))
                        File.Delete(OutFile);

                    using (Stream Writer = new StreamWriter(OutFile).BaseStream)
                    {
                        Entry.Content.CopyTo(Writer);
                        Writer.Flush();
                        Writer.Close();
                    }
                }
            }
        }

        public static void PackCat(string Folder)
        {

            string OriPackage = Folder.TrimEnd('\\', '/', ' ', '~');
            if (OriPackage.ToLowerInvariant().EndsWith(".dpk"))
            {
                PackDpk(Folder);
                return;
            }

            string OutFile = OriPackage + ".new";
            if (!File.Exists(OriPackage))
            {
                Console.WriteLine("Failed to Repack: \"{0}\" Not Found", OriPackage);
                return;
            }

            Entry[] Entries = (from x in Directory.GetFiles(Folder)
                               select new Entry()
                               {
                                   FileName = Path.GetFileName(x),
                                   Content = new StreamReader(x).BaseStream
                               }).ToArray();

            if (File.Exists(OutFile))
                File.Delete(OutFile);

            Console.WriteLine("Repacking to {0}, Please Wait...", OutFile);
            using (Stream NewPackget = new StreamWriter(OutFile).BaseStream)
            using (Stream Original = new StreamReader(OriPackage).BaseStream)
            {
                Cat Manager = new Cat(Original);
                Manager.Save(Entries, NewPackget);
            }

            foreach (Entry Entry in Entries)
                try
                {
                    Entry.Content?.Close();
                }
                catch { }
        }

        public static void PackDpk(string Folder)
        {

            string OriPackage = Folder.TrimEnd('\\', '/', ' ', '~');
            string OutFile = OriPackage + ".new";
            if (!File.Exists(OriPackage))
            {
                Console.WriteLine("Failed to Repack: \"{0}\" Not Found", OriPackage);
                return;
            }

            var Entries = (from x in Directory.GetFiles(Folder)
                               select new DPK.Entry()
                               {
                                   FileName = Path.GetFileName(x),
                                   Content = new StreamReader(x).BaseStream
                               }).ToArray();

            if (File.Exists(OutFile))
                File.Delete(OutFile);

            Console.WriteLine("Repacking to {0}, Please Wait...", OutFile);
            using (Stream NewPackage = new StreamWriter(OutFile).BaseStream)
            {
                DPK.Save(Entries, NewPackage);
            }

            foreach (var Entry in Entries)
                try
                {
                    Entry.Content?.Close();
                }
                catch { }
        }
    }
}
