using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MegaNepEditor
{
    public class TAMSHelper
    {
        TAMS tams;
        public TAMSHelper(byte[] Script)
        {
            tams = new TAMS(Script);
        }

        List<string> Formats = new List<string>();
        List<int> Counts = new List<int>();

        public string[] Import()
        {
            var OriLines = tams.Import();


            List<string> OutLines = new List<string>();
            for (int i = 0; i < OriLines.Length; i++)
            {
                AdvBinHelper.SplitLines(OriLines[i], out string[] Lines, out string Format);

                Counts.Add(Lines.Length);
                Formats.Add(Format);
                OutLines.AddRange(Lines);
            }

            return OutLines.ToArray();
        }

        public byte[] Export(string[] Content)
        {
            List<string> FullLines = new List<string>();
            for (int i = 0, x = 0; i < Content.Length;)
            {
                var Count = Counts[x];
                var Format = Formats[x++];

                var Lines = Content.Skip(i).Take(Count).ToArray();

                var FinalLine = string.Format(Format, Lines);

                FullLines.Add(FinalLine);

                i += Count;
            }
            return tams.Export(FullLines.ToArray());
        }

    }
}
