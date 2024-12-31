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
                var low = arg.ToLowerInvariant();
                if (File.Exists(arg) && low.EndsWith(".cat"))
                {
                    ExtractCat(arg);
                }
                else if (File.Exists(arg) && (low.EndsWith(".dpk") || low.EndsWith(".lds")))
                {
                    ExtractDPK(arg);
                }
                else if (File.Exists(arg) && (low.EndsWith(".bin")))
                {
                    ExtractText(arg);
                }
                else if (File.Exists(arg) && (low.EndsWith(".txt")))
                {
                    InsertText(arg);
                }
                else if (Directory.Exists(arg))
                {
                    PackCat(arg);
                }
            }
            Console.WriteLine("Finished, Press a Key To Exit");
            Console.ReadKey();
        }

        private static void ExtractText(string arg)
        {
            Console.WriteLine("Dumping: " + Path.GetFileName(arg));
            bool IsAdvBin = arg.ToUpper().Contains("ADV");

            if (IsAdvBin)
            {
                ExtractAdvText(arg);
                return;
            }
            try
            {
                BIN Parser = new BIN(File.ReadAllBytes(arg));
                var Lines = Parser.Import().Select(x => x.Replace("\n", "\\n")).ToArray();
                File.WriteAllLines(arg + ".txt", Lines);
            }
            catch
            {
                ExtractAdvText(arg);
            }
        }

        private static void ExtractAdvText(string arg)
        {
            Console.WriteLine("! AdvBin Detected !");
            var AdvParser = new AdvBinHelper(File.ReadAllBytes(arg));
            var AdvLines = AdvParser.Import().Select(x => x.Replace("\n", "\\n")).ToArray();
            File.WriteAllLines(arg + ".txt", AdvLines);
        }

        private static void InsertText(string arg)
        {
            Console.WriteLine("Inserting: " + Path.GetFileName(arg));
            var binPath = arg.Substring(0, arg.LastIndexOf('.'));

            bool IsAdvBin = arg.ToUpper().Contains("ADV");

            if (IsAdvBin) {
                InsertAdvText(binPath, arg);
                return;
            }

            try
            {
                BIN Parser = new BIN(File.ReadAllBytes(binPath));
                Parser.Import();
                var newLines = File.ReadAllLines(arg).Select(x => x.Replace("\\n", "\n")).ToArray();

                var newData = Parser.Export(newLines);

                File.WriteAllBytes(binPath + ".new", newData);
            }
            catch
            {
                InsertAdvText(binPath, arg);
            }
        }

        private static void InsertAdvText(string binPath, string textPath)
        {
            Console.WriteLine("! AdvBin Detected !");
            var Parser = new AdvBinHelper(File.ReadAllBytes(binPath));
            Parser.Import();
            var newLines = File.ReadAllLines(textPath).Select(x => x.Replace("\\n", "\n")).ToArray();

            var newData = Parser.Export(newLines);

            File.WriteAllBytes(binPath + ".new", newData);
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
            var low = OriPackage.ToLowerInvariant();
            if (low.EndsWith(".dpk") || low.EndsWith(".lds"))
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
            using (Stream NewPackget = File.Open(OutFile, FileMode.CreateNew, FileAccess.ReadWrite))
            using (Stream Original = File.Open(OriPackage, FileMode.Open, FileAccess.Read))
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
